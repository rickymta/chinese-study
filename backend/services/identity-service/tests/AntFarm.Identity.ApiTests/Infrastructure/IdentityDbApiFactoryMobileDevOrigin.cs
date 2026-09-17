using Microsoft.AspNetCore.Hosting;

namespace AntFarm.Identity.ApiTests.Infrastructure;

/// <summary>
/// M1 (RM-A4) — môi trường THẬT là Development (không phải "Testing" của các factory khác) +
/// <c>Auth:MobileDevOrigins</c> có <c>http://localhost:3290</c>, dùng để kiểm nhánh "Origin dev
/// được cho qua". Khoá ký RSA vẫn nạp được vì <see cref="IdentityApiFactory.KeysDirectory"/> LUÔN
/// có sẵn file .pem (không phụ thuộc <c>IsDevelopment()</c> để tự sinh).
/// </summary>
public sealed class IdentityDbApiFactoryMobileDevOrigin : IdentityDbApiFactory
{
    public IdentityDbApiFactoryMobileDevOrigin()
        => Environment.SetEnvironmentVariable("Auth__MobileDevOrigins__0", "http://localhost:3290");

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");

    /// <summary>
    /// Biến môi trường DÙNG CHUNG TOÀN TIẾN TRÌNH (xem cảnh báo ở <see cref="IdentityApiFactory"/>)
    /// — không dọn ở đây thì factory này chạy XONG vẫn để lại <c>Auth__MobileDevOrigins__0</c> rò
    /// rỉ sang mọi factory khác dựng SAU trong cùng collection tuần tự (kể cả factory môi trường
    /// Development khác chưa cố tình cấu hình origin dev).
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable("Auth__MobileDevOrigins__0", null);
        base.Dispose(disposing);
    }
}

/// <summary>
/// M1 (RM-A4, test §5.2.3 #6) — môi trường THẬT là Production kèm CÙNG danh sách
/// <c>Auth:MobileDevOrigins</c> để chứng minh nó bị BỎ QUA ngoài Development (không phải vì danh
/// sách rỗng).
/// </summary>
public sealed class IdentityDbApiFactoryMobileDevOriginProd : IdentityDbApiFactory
{
    public IdentityDbApiFactoryMobileDevOriginProd()
        => Environment.SetEnvironmentVariable("Auth__MobileDevOrigins__0", "http://localhost:3290");

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Production");

    /// <summary>Dọn biến môi trường dùng chung — cùng lý do <see cref="IdentityDbApiFactoryMobileDevOrigin.Dispose(bool)"/>.</summary>
    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable("Auth__MobileDevOrigins__0", null);
        base.Dispose(disposing);
    }
}
