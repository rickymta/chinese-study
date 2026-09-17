using System.Text.Json;
using System.Text.Json.Serialization;
using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Cms.Api.Middleware;
using AntFarm.Cms.Application;
using AntFarm.Cms.Application.Common.Options;
using AntFarm.Cms.Infrastructure;
using AntFarm.Cms.Infrastructure.Persistence;
using AntFarm.Cms.Infrastructure.Seeding;
using AntFarm.HealthChecks;
using AntFarm.Logging;
using AntFarm.Security.Errors;
using AntFarm.Security.Middleware;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddAfSerilog("cms-backend");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration); // DbContext; ném InvalidOperationException rõ ràng nếu ConnectionStrings:Default rỗng
builder.Services.AddApplication();
builder.Services.AddAfHealthChecks(); // "self" [live]; Infrastructure thêm "postgres" [ready]

// R-W1/R-W2: CmsAccess/CmsAdmin — POCO singleton INSTANCE (giống Jwt/AuthOptions của
// identity-service) để UserProvisioningService (Application, không FrameworkReference
// AspNetCore.App) tiêm thẳng được, không cần IOptions<T>.
var accessOptions = builder.Configuration.GetSection("CmsAccess").Get<CmsAccessOptions>() ?? new CmsAccessOptions();
var adminOptions = builder.Configuration.GetSection("CmsAdmin").Get<CmsAdminOptions>() ?? new CmsAdminOptions();
builder.Services.AddSingleton(accessOptions);
builder.Services.AddSingleton(adminOptions);

// Mỗi service kiểm token TRỰC TIẾP qua JWKS của identity-service (mạng nội bộ — Auth:JwksUrl trỏ
// THẲNG cổng identity-service, không qua gateway). Phân quyền cục bộ — PermissionResolver
// (đăng ký trong AddApplication) đọc DB access.* của CHÍNH service này (R-W1).
// KHÔNG AddAfCors/UseAfCors — API ngôn ngữ/service nền tảng luôn cùng origin (R-W7), không cần CORS.
builder.Services.AddAfJwtBearer(builder.Configuration);
builder.Services.AddAfPermissionAuthorization();
builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build());

// Giống identity-service/chinese-backend: .NET chỉ tin loopback theo mặc định, XOÁ danh sách mặc
// định rồi nạp tường minh (loopback dev + dải mạng af-net trong Docker qua cấu hình) để IP thật
// (X-Forwarded-For do gateway/nginx đặt) được tin đúng phạm vi.
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
        // dấu thành \uXXXX trong MỌI response JSON thành công (vd displayName của GET /api/me).
        o.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddAfInvalidModelStateResponse(); // 400 → { error, code: "VALIDATION", details: { field: [..] } }
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "AntFarm CMS API";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseAfSecurityHeaders();
app.UseAfCorrelationId();
// Request logging đứng NGOÀI exception handler: thấy mã trả về cuối cùng (422/401...) thay vì
// thấy ngoại lệ bay qua rồi ghi nhầm "responded 500" kèm stack trace.
app.UseSerilogRequestLogging();
app.UseAfExceptionHandler(); // AppException → status+code; còn lại 500 "Đã xảy ra lỗi nội bộ." + log Error
if (app.Environment.IsDevelopment())
{
    // AllowAnonymous BẮT BUỘC — FallbackPolicy = RequireAuthenticatedUser (dưới) áp cho MỌI
    // endpoint không có metadata phân quyền riêng; thiếu dòng này thì /openapi/v1.json và
    // /scalar/v1 nhận 401 dù chạy Development.
    app.MapOpenApi().AllowAnonymous(); // /openapi/v1.json
    app.MapScalarApiReference().AllowAnonymous(); // /scalar/v1
}
app.UseAuthentication();
app.UseMiddleware<UserProvisioningMiddleware>(); // R-W3: SAU xác thực, TRƯỚC phân quyền
app.UseAuthorization();
app.MapControllers();
app.MapAfHealthChecks();

if (app.Configuration.GetValue<bool>("AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
    if (db.Database.GetMigrations().Any())
        await db.Database.MigrateAsync();

    // W1+: seeder chạy ở đây — bắt mọi exception, log Error, KHÔNG ném (CLAUDE.md mục "Seed").
    var seedAdminOptions = scope.ServiceProvider.GetRequiredService<CmsAdminOptions>();
    var seedTimeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AccessSeeder");
    await AccessSeeder.SeedAsync(db, seedAdminOptions, seedTimeProvider, seedLogger, CancellationToken.None);
}

Log.Information("Khởi động cms-backend ({Env})", app.Environment.EnvironmentName);
try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "cms-backend dừng bất thường");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
