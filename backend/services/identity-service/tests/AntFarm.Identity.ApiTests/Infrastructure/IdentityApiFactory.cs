using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AntFarm.Identity.ApiTests.Infrastructure;

/// <summary>
/// Factory KHÔNG cần PostgreSQL thật cho các test không đụng DB (F0: /health/live,
/// /api/system/info) — cấu hình cookie kiểu DEV. Test đụng DB dùng <see cref="IdentityDbApiFactory"/>;
/// bộ cấu hình cookie kiểu PROD dùng <see cref="IdentityDbApiFactoryProdCookies"/> (RK6).
///
/// ⚠️ Dùng BIẾN MÔI TRƯỜNG chứ không phải ConfigureAppConfiguration: Program.cs đọc
/// builder.Configuration.GetConnectionString("Default") TRƯỚC khi builder.Build() được
/// gọi (bên trong AddInfrastructure), còn hook ConfigureAppConfiguration của
/// WebApplicationFactory chỉ có tác dụng vào lúc Build() chạy — quá muộn, giá trị đã
/// được đọc rồi. Biến môi trường thì AddEnvironmentVariables() của chính
/// WebApplicationBuilder.CreateBuilder(args) đọc ngay từ đầu nên luôn kịp.
///
/// ⚠️ xUnit tự dựng class fixture qua reflection nên constructor PHẢI không tham số — không
/// dùng tham số kiểu <c>bool prodCookieConfig = false</c> (xUnit không biết truyền gì).
///
/// ⚠️ Vì cấu hình nạp qua BIẾN MÔI TRƯỜNG DÙNG CHUNG TOÀN TIẾN TRÌNH, MỌI lớp test dùng bất kỳ
/// factory nào ở đây phải nằm trong CÙNG MỘT xUnit collection (<see cref="IdentityApiCollection"/>)
/// để chạy TUẦN TỰ — hai factory cấu hình khác nhau chạy song song sẽ đua nhau ghi đè biến môi
/// trường của nhau (§9.2, RK6).
/// </summary>
public class IdentityApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Thư mục khoá ký RS256 tạm — LUÔN có sẵn một khoá để FileSigningKeyStore không cần môi trường "Development" mới sinh được khoá (R-A13 chỉ tự sinh ở Development, còn factory chạy môi trường "Testing").</summary>
    public string KeysDirectory { get; } = CreateTemporaryKeyDirectory();

    public IdentityApiFactory()
    {
        // Đặt CHỈ MỘT LẦN ở đây (không đưa vào ApplyCommonEnvironment): IdentityDbApiFactory ghi
        // đè giá trị này thành chuỗi kết nối af_identity_test thật ngay sau khi base() chạy —
        // nếu ApplyCommonEnvironment (được gọi lại bởi IdentityDbApiFactoryProdCookies) cũng đặt
        // khoá này thì sẽ XOÁ MẤT chuỗi kết nối thật, làm request thật sự chạm DB "unused" (lỗi
        // Npgsql mơ hồ "No password has been provided").
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=unused");
        Environment.SetEnvironmentVariable("AutoMigrate", "false");

        ApplyCommonEnvironment(prodCookieConfig: false);
    }

    /// <summary>Gọi lại được nhiều lần an toàn (KHÔNG đụng ConnectionStrings/AutoMigrate) — IdentityDbApiFactoryProdCookies gọi lại với prodCookieConfig=true sau khi lớp cha đã đặt chuỗi kết nối DB thật.</summary>
    protected void ApplyCommonEnvironment(bool prodCookieConfig)
    {
        Environment.SetEnvironmentVariable("Jwt__KeysPath", KeysDirectory);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "http://localhost:5280/identity");
        Environment.SetEnvironmentVariable("Jwt__Audiences__0", "af-identity");
        Environment.SetEnvironmentVariable("Jwt__Audiences__1", "af-chinese");
        Environment.SetEnvironmentVariable("Jwt__Audiences__2", "af-cms"); // W1: cms-backend
        Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenDays", "30");

        // §5.6.2 — hai bộ cấu hình cookie dev/prod (RK6): ApiTests F2 phải chạy được cả hai.
        Environment.SetEnvironmentVariable("Auth__RefreshCookieDomain", prodCookieConfig ? ".antfarms.xyz" : "");
        Environment.SetEnvironmentVariable("Auth__RefreshCookiePath", prodCookieConfig ? "/api/auth" : "/identity/api/auth");
        Environment.SetEnvironmentVariable("Auth__RefreshCookieSecure", prodCookieConfig ? "true" : "false");
        Environment.SetEnvironmentVariable("Auth__AllowRegistration", "true");
        Environment.SetEnvironmentVariable("Auth__RefreshReuseGraceSeconds", "30");
        Environment.SetEnvironmentVariable("Auth__MaxFailedLogins", "10");
        Environment.SetEnvironmentVariable("Auth__LockoutMinutes", "15");
        Environment.SetEnvironmentVariable("Auth__AllowedOrigins__0", "http://localhost:3280");
        Environment.SetEnvironmentVariable("Auth__AllowedOrigins__1", "http://localhost:3281");
        // Rất nhiều test gọi liên tiếp trong vài giây — limiter "auth" không tách theo IP trong
        // TestServer nên phải nới rộng để CHÍNH BỘ TEST không tự đụng rate limit của nhau; kiểm
        // 429 (nếu cần) nên dùng factory nạp giá trị nhỏ RIÊNG.
        Environment.SetEnvironmentVariable("Auth__RateLimitPermitPerMinute", "100000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    private static string CreateTemporaryKeyDirectory()
    {
        var dir = Directory.CreateTempSubdirectory("antfarm-identity-keys-").FullName;
        using var rsa = RSA.Create(2048);
        File.WriteAllText(Path.Combine(dir, "20260101-00000001.pem"), rsa.ExportPkcs8PrivateKeyPem());
        return dir;
    }
}
