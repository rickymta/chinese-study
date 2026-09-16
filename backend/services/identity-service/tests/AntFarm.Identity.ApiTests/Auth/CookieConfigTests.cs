using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>RK6/§5.6.2 — cookie refresh phải đúng Domain/Path/Secure theo TỪNG môi trường; test chạy cả hai bộ cấu hình dev VÀ prod (không chỉ một).</summary>
[Collection(IdentityApiCollection.Name)]
public class CookieConfigDevTests(IdentityDbApiFactory factory) : IClassFixture<IdentityDbApiFactory>
{
    [DbFact]
    public async Task CookieDev_KhongCoDomain_PathQuaVite_KhongSecure()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"dev-{Guid.NewGuid():N}@vidu.com", password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" },
            JsonDefaults.Options);

        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("af_rt="));
        cookie.Should().Contain("path=/identity/api/auth");
        cookie.Should().NotContain("domain=");
        cookie.Should().NotContain("secure");
        cookie.Should().Contain("httponly");
        cookie.Should().Contain("samesite=strict");
    }
}

[Collection(IdentityApiCollection.Name)]
public class CookieConfigProdTests(IdentityDbApiFactoryProdCookies factory) : IClassFixture<IdentityDbApiFactoryProdCookies>
{
    [DbFact]
    public async Task CookieProd_CoDomainAntfarms_PathApiAuth_Secure()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"prod-{Guid.NewGuid():N}@vidu.com", password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" },
            JsonDefaults.Options);

        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("af_rt="));
        cookie.Should().Contain("path=/api/auth");
        cookie.Should().Contain("domain=.antfarms.xyz");
        cookie.Should().Contain("secure");
        cookie.Should().Contain("httponly");
        cookie.Should().Contain("samesite=strict");
    }
}
