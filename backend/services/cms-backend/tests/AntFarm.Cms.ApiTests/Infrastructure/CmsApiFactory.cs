using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AntFarm.Cms.ApiTests.Infrastructure;

/// <summary>
/// Factory KHÔNG cần PostgreSQL thật: chuỗi kết nối chỉ để AddInfrastructure không ném lỗi lúc
/// khởi động; AutoMigrate=false nên không có kết nối DB thật nào được mở (chỉ /health/live,
/// /api/system/info, và các test guard không đụng DB).
///
/// ⚠️ Dùng BIẾN MÔI TRƯỜNG chứ không phải ConfigureAppConfiguration — Program.cs đọc cấu hình
/// TRƯỚC Build() (chép khuôn ChineseApiFactory/IdentityApiFactory).
///
/// ⚠️ Biến môi trường DÙNG CHUNG TOÀN TIẾN TRÌNH với <see cref="CmsDbApiFactory"/> — MỌI lớp test
/// dùng bất kỳ factory cms nào phải nằm trong CÙNG <see cref="CmsApiCollection"/> để chạy TUẦN TỰ.
/// </summary>
public sealed class CmsApiFactory : WebApplicationFactory<Program>
{
    public CmsApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=unused");
        Environment.SetEnvironmentVariable("AutoMigrate", "false");
        Environment.SetEnvironmentVariable("Auth__Issuer", "https://id.antfarms.xyz.test");
        Environment.SetEnvironmentVariable("Auth__Audience", "af-cms");
        Environment.SetEnvironmentVariable("Auth__JwksUrl", "http://localhost:65535/.well-known/jwks.json");
        Environment.SetEnvironmentVariable("Auth__RequireHttpsMetadata", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
