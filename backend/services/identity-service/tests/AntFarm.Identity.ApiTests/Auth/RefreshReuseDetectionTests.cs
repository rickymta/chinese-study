using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>R-A6 "ngoài cửa sổ ân hạn" — cần đồng hồ giả để tua > 30 giây mà không Thread.Sleep thật.</summary>
[Collection(IdentityApiCollection.Name)]
public class RefreshReuseDetectionTests : IClassFixture<IdentityDbApiFactoryWithFakeClock>
{
    private readonly IdentityDbApiFactoryWithFakeClock _factory;

    public RefreshReuseDetectionTests(IdentityDbApiFactoryWithFakeClock factory) => _factory = factory;

    private static string NewEmail() => $"hocvien-{Guid.NewGuid():N}@vidu.com";

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        return client;
    }

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith($"{cookieName}="));
        return setCookie.Split(';')[0][(cookieName.Length + 1)..];
    }

    private static HttpRequestMessage RefreshRequest(string cookieValue)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"af_rt={cookieValue}");
        return request;
    }

    [DbFact]
    public async Task DungLaiTokenDaXoay_SauHonBaMuoiGiay_ThuHoiCaHoVa401()
    {
        var client = CreateClient();
        var email = NewEmail();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);
        var cookie1 = ExtractCookieValue(registerResponse, "af_rt");

        // Xoay lần 1: cookie1 → cookie2 (rotated_at = now).
        var firstRotation = await client.SendAsync(RefreshRequest(cookie1));
        firstRotation.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie2 = ExtractCookieValue(firstRotation, "af_rt");

        // Tua đồng hồ QUÁ cửa sổ ân hạn (30s) rồi mới dùng LẠI cookie1 (đã xoay) — RK7/R-A6:
        // đây là dấu hiệu token bị đánh cắp/dùng lại, phải thu hồi CẢ HỌ (kể cả cookie2 hợp lệ).
        _factory.Clock.Advance(TimeSpan.FromSeconds(31));

        var reuseAttempt = await client.SendAsync(RefreshRequest(cookie1));
        reuseAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await reuseAttempt.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("REFRESH_INVALID");

        // cookie2 (token "hợp lệ" nhất, chưa từng bị dùng lại) cũng phải bị thu hồi theo CẢ HỌ.
        var cookie2AfterRevocation = await client.SendAsync(RefreshRequest(cookie2));
        cookie2AfterRevocation.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task DungLaiTokenDaXoay_TrongBaMuoiGiay_TraVe200KhongThuHoi()
    {
        var client = CreateClient();
        var email = NewEmail();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);
        var cookie1 = ExtractCookieValue(registerResponse, "af_rt");

        var firstRotation = await client.SendAsync(RefreshRequest(cookie1));
        firstRotation.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie2 = ExtractCookieValue(firstRotation, "af_rt");

        _factory.Clock.Advance(TimeSpan.FromSeconds(20)); // vẫn trong 30s

        var secondUseOfCookie1 = await client.SendAsync(RefreshRequest(cookie1));
        secondUseOfCookie1.StatusCode.Should().Be(HttpStatusCode.OK);

        // cookie2 (nhánh xoay hợp lệ đầu tiên) vẫn còn dùng được — KHÔNG bị thu hồi oan.
        var cookie2StillWorks = await client.SendAsync(RefreshRequest(cookie2));
        cookie2StillWorks.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ErrorDto(string Error, string Code);
}
