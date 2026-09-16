using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AntFarm.Chinese.ApiTests.Infrastructure;

/// <summary>
/// Factory KHÔNG cần PostgreSQL thật: chuỗi kết nối chỉ để AddInfrastructure không ném
/// lỗi lúc khởi động; AutoMigrate=false nên không có kết nối DB thật nào được mở trong
/// các test F0 (chỉ /health/live, /api/system/info).
///
/// ⚠️ Dùng BIẾN MÔI TRƯỜNG chứ không phải ConfigureAppConfiguration — xem giải thích đầy
/// đủ ở IdentityApiFactory (cùng bẫy, cùng lý do: Program.cs đọc cấu hình TRƯỚC Build()).
///
/// F3: <c>AddAfJwtBearer(IConfiguration)</c> ném ngay lúc khởi động nếu thiếu section "Auth" —
/// các giá trị dưới đây KHÔNG cần đúng thật (health/system-info là AllowAnonymous, không request
/// nào ở đây mang Bearer token) chỉ cần khác rỗng để service khởi động được.
///
/// ⚠️ Biến môi trường DÙNG CHUNG TOÀN TIẾN TRÌNH với <see cref="ChineseDbApiFactory"/> — MỌI lớp
/// test dùng bất kỳ factory chinese nào phải nằm trong CÙNG <see cref="ChineseApiCollection"/> để
/// chạy TUẦN TỰ (giống IdentityApiFactory, §9.2).
/// </summary>
public sealed class ChineseApiFactory : WebApplicationFactory<Program>
{
    public ChineseApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=unused");
        Environment.SetEnvironmentVariable("AutoMigrate", "false");
        // F6: ContentImportRunner chạy ĐỘC LẬP với AutoMigrate (xem ghi chú Program.cs) — factory
        // này không có DB thật ("unused"), tắt hẳn để tránh mọi lượt kết nối DB không cần thiết.
        Environment.SetEnvironmentVariable("Content__ImportOnStartup", "false");
        Environment.SetEnvironmentVariable("Auth__Issuer", "https://id.antfarms.xyz.test");
        Environment.SetEnvironmentVariable("Auth__Audience", "af-chinese");
        Environment.SetEnvironmentVariable("Auth__JwksUrl", "http://localhost:65535/.well-known/jwks.json");
        Environment.SetEnvironmentVariable("Auth__RequireHttpsMetadata", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
