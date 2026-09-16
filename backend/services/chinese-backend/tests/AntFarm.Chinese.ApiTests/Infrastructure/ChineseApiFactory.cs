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
/// </summary>
public sealed class ChineseApiFactory : WebApplicationFactory<Program>
{
    public ChineseApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=unused");
        Environment.SetEnvironmentVariable("AutoMigrate", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
