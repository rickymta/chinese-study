using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AntFarm.Identity.ApiTests.Infrastructure;

/// <summary>
/// Factory KHÔNG cần PostgreSQL thật: chuỗi kết nối chỉ để AddInfrastructure không ném
/// lỗi lúc khởi động; AutoMigrate=false nên không có kết nối DB thật nào được mở trong
/// các test F0 (chỉ /health/live, /api/system/info).
///
/// ⚠️ Dùng BIẾN MÔI TRƯỜNG chứ không phải ConfigureAppConfiguration: Program.cs đọc
/// builder.Configuration.GetConnectionString("Default") TRƯỚC khi builder.Build() được
/// gọi (bên trong AddInfrastructure), còn hook ConfigureAppConfiguration của
/// WebApplicationFactory chỉ có tác dụng vào lúc Build() chạy — quá muộn, giá trị đã
/// được đọc rồi. Biến môi trường thì AddEnvironmentVariables() của chính
/// WebApplicationBuilder.CreateBuilder(args) đọc ngay từ đầu nên luôn kịp.
/// </summary>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    public IdentityApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=unused");
        Environment.SetEnvironmentVariable("AutoMigrate", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
