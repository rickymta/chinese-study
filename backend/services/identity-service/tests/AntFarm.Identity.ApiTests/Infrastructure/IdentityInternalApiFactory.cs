using AntFarm.Identity.Api.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Identity.ApiTests.Infrastructure;

/// <summary>
/// Factory cho test API nội bộ /internal/* (§5.2.9, W10) — DB thật (af_identity_test, kế thừa
/// <see cref="IdentityDbApiFactory"/>) + <c>Internal:Port=5291</c> + khoá dịch vụ test (≥ 32 ký
/// tự) + thay <see cref="ILocalPortAccessor"/> bằng <see cref="TestLocalPortAccessor"/> (TestServer
/// thật luôn có <c>Connection.LocalPort == 0</c> — test tự khai "cổng muốn mô phỏng" qua header
/// <c>X-Test-Local-Port</c>, chỉ tồn tại trong project test).
/// </summary>
public sealed class IdentityInternalApiFactory : IdentityDbApiFactory
{
    public const string ServiceKey = "test-internal-service-key-du-dai-32-ky-tu-tro-len";
    public const int InternalPort = 5291;
    public const int PublicPort = 5281;

    public IdentityInternalApiFactory()
    {
        Environment.SetEnvironmentVariable("Internal__Port", InternalPort.ToString());
        Environment.SetEnvironmentVariable("Internal__ServiceKey", ServiceKey);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services => services.AddSingleton<ILocalPortAccessor, TestLocalPortAccessor>());
    }
}

/// <summary>Đọc header test <c>X-Test-Local-Port</c> — thiếu header ⇒ 0 (mô phỏng ĐÚNG hành vi TestServer thật khi không set, tức "không phải cổng nội bộ").</summary>
public sealed class TestLocalPortAccessor : ILocalPortAccessor
{
    public int GetLocalPort(HttpContext context)
        => context.Request.Headers.TryGetValue("X-Test-Local-Port", out var v) && int.TryParse(v, out var port) ? port : 0;
}
