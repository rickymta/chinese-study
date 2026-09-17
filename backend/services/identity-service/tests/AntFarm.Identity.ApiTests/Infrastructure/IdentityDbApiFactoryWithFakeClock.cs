using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Identity.ApiTests.Infrastructure;

/// <summary>Thay <c>TimeProvider</c> DI của AuthService bằng <see cref="ManualTimeProvider"/> tua được — chỉ ảnh hưởng luồng nghiệp vụ (đăng ký/refresh...), KHÔNG ảnh hưởng <c>iat/exp</c> của access token (TokenIssuer nhận <c>TimeProvider.System</c> trực tiếp trong Program.cs, không qua DI).</summary>
public sealed class IdentityDbApiFactoryWithFakeClock : IdentityDbApiFactory
{
    public ManualTimeProvider Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }
}
