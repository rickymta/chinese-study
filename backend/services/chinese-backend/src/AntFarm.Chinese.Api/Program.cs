using System.Text.Json;
using System.Text.Json.Serialization;
using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Api.Middleware;
using AntFarm.Chinese.Application;
using AntFarm.Chinese.Application.Common.Options;
using AntFarm.Chinese.Application.Pinyin;
using AntFarm.Chinese.Infrastructure;
using AntFarm.Chinese.Infrastructure.Persistence;
using AntFarm.Chinese.Infrastructure.Seeding;
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

builder.AddAfSerilog("chinese-backend");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration); // DbContext; ném InvalidOperationException rõ ràng nếu ConnectionStrings:Default rỗng
builder.Services.AddApplication();
builder.Services.AddAfHealthChecks(); // "self" [live]; Infrastructure thêm "postgres" [ready]

// F3: ChineseAccess/ChineseAdmin — POCO singleton INSTANCE (giống Jwt/AuthOptions của
// identity-service, §5.2.0.5) để UserProvisioningService (Application, không FrameworkReference
// AspNetCore.App) tiêm thẳng được, không cần IOptions<T>.
var accessOptions = builder.Configuration.GetSection("ChineseAccess").Get<ChineseAccessOptions>() ?? new ChineseAccessOptions();
var adminOptions = builder.Configuration.GetSection("ChineseAdmin").Get<ChineseAdminOptions>() ?? new ChineseAdminOptions();
builder.Services.AddSingleton(accessOptions);
builder.Services.AddSingleton(adminOptions);

// R-N5/R-A3: mỗi service kiểm token TRỰC TIẾP qua JWKS của identity-service (mạng nội bộ —
// Auth:JwksUrl trỏ THẲNG cổng identity-service, không qua gateway). R-P1: phân quyền cục bộ —
// PermissionResolver (đăng ký trong AddApplication) đọc DB access.* của CHÍNH service này.
// KHÔNG còn AddAfCors/UseAfCors (R-N9: API ngôn ngữ luôn cùng origin, không cần CORS — review F0 17/09/2026).
builder.Services.AddAfJwtBearer(builder.Configuration);
builder.Services.AddAfPermissionAuthorization();
builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build());

// RK32: giống identity-service — .NET chỉ tin loopback theo mặc định, XOÁ danh sách mặc định rồi
// nạp tường minh (loopback dev + dải mạng af-net trong Docker qua cấu hình) để IP thật
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
        document.Info.Title = "AntFarm Chinese API";
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
    // /scalar/v1 nhận 401 dù chạy Development (review F3 17/09/2026).
    app.MapOpenApi().AllowAnonymous(); // /openapi/v1.json
    app.MapScalarApiReference().AllowAnonymous(); // /scalar/v1
}
app.UseAuthentication();
app.UseMiddleware<UserProvisioningMiddleware>(); // R-P4: SAU xác thực, TRƯỚC phân quyền
app.UseAuthorization();
app.MapControllers();
app.MapAfHealthChecks();

if (app.Configuration.GetValue<bool>("AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ChineseDbContext>();
    if (db.Database.GetMigrations().Any())
        await db.Database.MigrateAsync();

    // F3+: seeder chạy ở đây — bắt mọi exception, log Error, KHÔNG ném (CLAUDE.md mục "Seed").
    var seedAdminOptions = scope.ServiceProvider.GetRequiredService<ChineseAdminOptions>();
    var seedTimeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AccessSeeder");
    await AccessSeeder.SeedAsync(db, seedAdminOptions, seedTimeProvider, seedLogger, CancellationToken.None);
}

// F5: IPinyinCatalog đăng ký Singleton "nạp lười" (tạo lúc RESOLVE ĐẦU TIÊN) — resolve tường minh
// ở đây để học liệu hỏng lộ ra NGAY lúc khởi động (log Error, xem PinyinCatalogLoader), không phải
// lúc request /api/pinyin/* đầu tiên tới. Không ném dù học liệu thiếu/hỏng (catalog.IsAvailable=false).
app.Services.GetRequiredService<IPinyinCatalog>();

Log.Information("Khởi động chinese-backend ({Env})", app.Environment.EnvironmentName);
try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "chinese-backend dừng bất thường");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
