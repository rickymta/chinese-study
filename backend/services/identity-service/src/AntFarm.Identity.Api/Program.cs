using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AntFarm.Auth;
using AntFarm.HealthChecks;
using AntFarm.Identity.Api.Configuration;
using AntFarm.Identity.Application;
using AntFarm.Identity.Application.Common.Abstractions;
using AntFarm.Identity.Application.Common.Options;
using AntFarm.Identity.Infrastructure;
using AntFarm.Identity.Infrastructure.Persistence;
using AntFarm.Identity.Infrastructure.Security;
using AntFarm.Logging;
using AntFarm.Security.Errors;
using AntFarm.Security.Middleware;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddAfSerilog("identity-service");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration); // DbContext; ném InvalidOperationException rõ ràng nếu ConnectionStrings:Default rỗng
builder.Services.AddApplication();
builder.Services.AddAfHealthChecks(); // "self" [live]; Infrastructure thêm "postgres" [ready]

// ── F2: Jwt/Auth options — POCO singleton INSTANCE (không IOptions<T>) để Application layer
// (không FrameworkReference AspNetCore.App) vẫn tiêm thẳng được vào AuthService/AccountService. ──
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Thiếu cấu hình Jwt — xem appsettings.Development.json.example.");
var authOptions = builder.Configuration.GetSection("Auth").Get<AuthOptions>()
    ?? throw new InvalidOperationException("Thiếu cấu hình Auth — xem appsettings.Development.json.example.");
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(authOptions);

// R-A13: khoá ký RS256 persist file PEM. Dựng TRỰC TIẾP (không qua AddInfrastructure) vì cần
// instance CỤ THỂ ngay bây giờ để truyền vào AddAfJwtBearer (kiểm token của chính mình bằng
// khoá cục bộ, không gọi JWKS qua mạng tới chính mình). Development: thư mục trống ⇒ tự sinh
// khoá; môi trường khác: ném lỗi ngay ở đây, dừng khởi động rõ ràng thay vì lỗi mơ hồ lúc ký.
var keysPath = Path.IsPathRooted(jwtOptions.KeysPath)
    ? jwtOptions.KeysPath
    : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, jwtOptions.KeysPath));
var signingKeyStore = new FileSigningKeyStore(keysPath, jwtOptions.ActiveKeyId, builder.Environment.IsDevelopment());
builder.Services.AddSingleton<ISigningKeyStore>(signingKeyStore);
builder.Services.AddSingleton<ITokenIssuer>(new TokenIssuer(signingKeyStore, jwtOptions, TimeProvider.System));
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();

builder.Services.AddIdentityCors(authOptions);

// Bearer riêng của CHÍNH identity-service (audience "af-identity") cho /api/account, /api/auth/password.
builder.Services.AddAfJwtBearer(
    new AfAuthOptions { Issuer = jwtOptions.Issuer, Audience = "af-identity", RequireHttpsMetadata = !builder.Environment.IsDevelopment() },
    () => signingKeyStore.GetPublicKeys());
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    // RejectionStatusCode mặc định của ASP.NET Core là 503 — PHẢI đổi tường minh thành 429,
    // nếu không OnRejected vẫn chạy (ghi đúng body JSON) nhưng mã trạng thái vẫn là 503 (§6.0).
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Bạn thao tác quá nhanh, thử lại sau.", code = "RATE_LIMITED" },
            cancellationToken: ct);
    };
    // R-A9: 20 yêu cầu/phút/IP cho login|register|refresh (áp cả logout/password vì cùng
    // controller). PHẢI dùng AddPolicy + RateLimitPartition (khoá theo IP thật, đã được
    // UseForwardedHeaders đặt lại đúng từ X-Forwarded-For) — AddFixedWindowLimiter(name, configure)
    // trần tạo MỘT limiter DÙNG CHUNG CHO MỌI NGƯỜI GỌI (không tách theo IP), nghĩa là một người
    // spam đăng nhập sẽ khoá luôn mọi người dùng khác đang gọi cùng lúc (review F2 17/09/2026).
    // PermitLimit lấy từ cấu hình (không hardcode) để ApiTests nới rộng — TestServer thường chỉ
    // có MỘT địa chỉ IP (RemoteIpAddress null/loopback) cho mọi request nên vẫn cần permit cao.
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authOptions.RateLimitPermitPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// RK32: .NET chỉ tin loopback theo mặc định — XOÁ danh sách mặc định rồi nạp tường minh
// (loopback dev + dải mạng af-net trong Docker qua cấu hình) để IP thật (X-Forwarded-For do
// gateway/nginx đặt) được tin đúng phạm vi, không thiếu (IP = IP gateway) không thừa (giả IP để né khoá).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = builder.Configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 2;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    var networks = builder.Configuration.GetSection("ForwardedHeaders:KnownIPNetworks").Get<string[]>()
        ?? ["127.0.0.1/32", "::1/128"];
    foreach (var cidr in networks)
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // Giống AntFarm.Security.Errors.ExceptionHandlingExtensions — tránh escape chữ Việt có
        // dấu thành \uXXXX trong MỌI response JSON thành công (vd displayName của GET /api/account).
        o.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddAfInvalidModelStateResponse(); // 400 → { error, code: "VALIDATION", details: { field: [..] } }
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "AntFarm Identity API";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseAfSecurityHeaders();
app.UseAfCorrelationId();
app.UseAfExceptionHandler(); // AppException → status+code; còn lại 500 "Đã xảy ra lỗi nội bộ." + log Error
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
    app.MapScalarApiReference(); // /scalar/v1
}
app.UseIdentityCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapAfHealthChecks();

if (app.Configuration.GetValue<bool>("AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    if (db.Database.GetMigrations().Any())
        await db.Database.MigrateAsync();
    // F2 chưa có seeder (không có danh mục người dùng tự xoá ở identity) — F3 mới cần seeder cho chinese-backend.
}

Log.Information("Khởi động identity-service ({Env})", app.Environment.EnvironmentName);
try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "identity-service dừng bất thường");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
