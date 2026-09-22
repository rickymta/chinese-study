using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>M1 §5.2.3 #3 — xoay vòng/ân hạn/phát hiện dùng lại của kênh mobile (RM-A2), y hệt luật web nhưng token nằm trong body thay vì cookie.</summary>
[Collection(IdentityApiCollection.Name)]
public class MobileRefreshReuseTests : IClassFixture<IdentityDbApiFactoryWithFakeClock>
{
    private readonly IdentityDbApiFactoryWithFakeClock _factory;

    public MobileRefreshReuseTests(IdentityDbApiFactoryWithFakeClock factory) => _factory = factory;

    private static string NewEmail() => $"mobilereuse-{Guid.NewGuid():N}@vidu.com";

    private HttpClient CreateClient() => _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });

    private static HttpRequestMessage MobileRequest(string path, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, options: JsonDefaults.Options) };
        request.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (ios)");
        return request;
    }

    private async Task<string> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.SendAsync(MobileRequest("/api/auth/mobile/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenDto>(JsonDefaults.Options);
        return body!.RefreshToken;
    }

    [DbFact]
    public async Task DungLaiTokenDaXoay_SauHonBaMuoiGiay_ThuHoiCaHoVa401()
    {
        var client = CreateClient();
        var token1 = await RegisterAsync(client, NewEmail());

        var firstRotation = await client.SendAsync(MobileRequest("/api/auth/mobile/refresh", new { refreshToken = token1 }));
        firstRotation.StatusCode.Should().Be(HttpStatusCode.OK);
        var token2 = (await firstRotation.Content.ReadFromJsonAsync<TokenDto>(JsonDefaults.Options))!.RefreshToken;

        _factory.Clock.Advance(TimeSpan.FromSeconds(31));

        var reuseAttempt = await client.SendAsync(MobileRequest("/api/auth/mobile/refresh", new { refreshToken = token1 }));
        reuseAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await reuseAttempt.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("REFRESH_INVALID");

        var token2AfterRevocation = await client.SendAsync(MobileRequest("/api/auth/mobile/refresh", new { refreshToken = token2 }));
        token2AfterRevocation.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task DungLaiTokenDaXoay_TrongBaMuoiGiay_TraVe200KhongThuHoi()
    {
        var client = CreateClient();
        var token1 = await RegisterAsync(client, NewEmail());

        var firstRotation = await client.SendAsync(MobileRequest("/api/auth/mobile/refresh", new { refreshToken = token1 }));
        firstRotation.StatusCode.Should().Be(HttpStatusCode.OK);
        var token2 = (await firstRotation.Content.ReadFromJsonAsync<TokenDto>(JsonDefaults.Options))!.RefreshToken;

        _factory.Clock.Advance(TimeSpan.FromSeconds(20));

        var secondUseOfToken1 = await client.SendAsync(MobileRequest("/api/auth/mobile/refresh", new { refreshToken = token1 }));
        secondUseOfToken1.StatusCode.Should().Be(HttpStatusCode.OK);

        var token2StillWorks = await client.SendAsync(MobileRequest("/api/auth/mobile/refresh", new { refreshToken = token2 }));
        token2StillWorks.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record TokenDto(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record ErrorDto(string Error, string Code);
}
