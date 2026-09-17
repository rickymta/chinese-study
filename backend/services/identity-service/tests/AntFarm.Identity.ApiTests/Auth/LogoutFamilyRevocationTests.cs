using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>
/// Review M2: <c>LogoutAsync</c> phải thu hồi CẢ HỌ kể cả khi token gửi lên đã bị xoay (một lần
/// refresh chạy song song — vd tab/app khác — vừa xoay xong trước lúc bấm đăng xuất) — không chỉ
/// token đang cầm. Trước sửa: web chỉ thu hồi đúng token gửi lên ⇒ token kế nhiệm cùng họ vẫn
/// sống, phiên "sống lại" sau khi tưởng đã đăng xuất. Dùng đồng hồ giả (<see cref="ManualTimeProvider"/>)
/// để test cả nhánh "họ đã bị thu hồi do dùng lại ngoài cửa sổ ân hạn" mà không Thread.Sleep thật.
/// </summary>
[Collection(IdentityApiCollection.Name)]
public class LogoutFamilyRevocationTests : IClassFixture<IdentityDbApiFactoryWithFakeClock>
{
    private readonly IdentityDbApiFactoryWithFakeClock _factory;

    public LogoutFamilyRevocationTests(IdentityDbApiFactoryWithFakeClock factory) => _factory = factory;

    private static string NewEmail() => $"logoutho-{Guid.NewGuid():N}@vidu.com";

    private HttpClient CreateWebClient()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        return client;
    }

    private HttpClient CreateMobileClient()
        => _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
        => response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith($"{cookieName}=")).Split(';')[0][(cookieName.Length + 1)..];

    private static HttpRequestMessage WebRefreshRequest(string cookieValue)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"af_rt={cookieValue}");
        return request;
    }

    private static HttpRequestMessage WebLogoutRequest(string cookieValue)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add("Cookie", $"af_rt={cookieValue}");
        return request;
    }

    private static HttpRequestMessage MobilePost(string path, object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonDefaults.Options);
        request.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (android)");
        return request;
    }

    private async Task<string> RegisterWebAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);
        response.EnsureSuccessStatusCode();
        return ExtractCookieValue(response, "af_rt");
    }

    private async Task<string> RegisterMobileAsync(HttpClient client, string email)
    {
        var response = await client.SendAsync(MobilePost("/api/auth/mobile/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenDto>(JsonDefaults.Options);
        return body!.RefreshToken;
    }

    [DbFact]
    public async Task LogoutWeb_VoiTokenDaXoay_ThuHoiCaHo_TokenKeNhiemKhongDungDuocNua()
    {
        var client = CreateWebClient();
        var cookie1 = await RegisterWebAsync(client, NewEmail());

        // Xoay NGAY (trong cửa sổ ân hạn): cookie1 → cookie2. cookie1 giờ RotatedAt != null nhưng
        // RevokedAt vẫn null (MarkRotated không tự thu hồi) — LogoutAsync vẫn coi là "tồn tại,
        // chưa bị thu hồi" nên xử lý được.
        var rotate = await client.SendAsync(WebRefreshRequest(cookie1));
        rotate.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie2 = ExtractCookieValue(rotate, "af_rt");

        // Đăng xuất bằng token CŨ — mô phỏng client giữ token cũ do một tab khác vừa xoay xong
        // trước khi request đăng xuất tới nơi.
        var logout = await client.SendAsync(WebLogoutRequest(cookie1));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Token kế nhiệm (cookie2) — KHÔNG được gửi lên logout — vẫn phải bị thu hồi theo cả họ.
        var refreshWithSuccessor = await client.SendAsync(WebRefreshRequest(cookie2));
        refreshWithSuccessor.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task LogoutMobile_VoiTokenDaXoay_ThuHoiCaHo_TokenKeNhiemKhongDungDuocNua()
    {
        var client = CreateMobileClient();
        var token1 = await RegisterMobileAsync(client, NewEmail());

        var rotate = await client.SendAsync(MobilePost("/api/auth/mobile/refresh", new { refreshToken = token1 }));
        rotate.StatusCode.Should().Be(HttpStatusCode.OK);
        var token2 = (await rotate.Content.ReadFromJsonAsync<TokenDto>(JsonDefaults.Options))!.RefreshToken;

        var logout = await client.SendAsync(MobilePost("/api/auth/mobile/logout", new { refreshToken = token1 }));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshWithSuccessor = await client.SendAsync(MobilePost("/api/auth/mobile/refresh", new { refreshToken = token2 }));
        refreshWithSuccessor.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task LogoutWeb_VoiTokenThuocHoDaBiThuHoiDoDungLai_KhongLoi()
    {
        var client = CreateWebClient();
        var cookie1 = await RegisterWebAsync(client, NewEmail());

        var rotate = await client.SendAsync(WebRefreshRequest(cookie1));
        rotate.StatusCode.Should().Be(HttpStatusCode.OK);

        // Tua QUÁ cửa sổ ân hạn rồi dùng lại cookie1 (đã xoay) ⇒ reuse_detected, thu hồi cả họ.
        _factory.Clock.Advance(TimeSpan.FromSeconds(31));
        var reuse = await client.SendAsync(WebRefreshRequest(cookie1));
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Đăng xuất bằng chính token vừa bị thu hồi (RevokedAt != null, reason=reuse_detected)
        // ⇒ không lỗi, không làm gì thêm (guard chặn trước khi chạm RevokeFamilyAsync).
        var logout = await client.SendAsync(WebLogoutRequest(cookie1));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [DbFact]
    public async Task LogoutMobile_VoiTokenThuocHoDaBiThuHoiDoDungLai_KhongLoi()
    {
        var client = CreateMobileClient();
        var token1 = await RegisterMobileAsync(client, NewEmail());

        var rotate = await client.SendAsync(MobilePost("/api/auth/mobile/refresh", new { refreshToken = token1 }));
        rotate.StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.Clock.Advance(TimeSpan.FromSeconds(31));
        var reuse = await client.SendAsync(MobilePost("/api/auth/mobile/refresh", new { refreshToken = token1 }));
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var logout = await client.SendAsync(MobilePost("/api/auth/mobile/logout", new { refreshToken = token1 }));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>Chiều còn lại của RM-A1/M1 test #5 (<c>MobileChannelIsolationTests.LogoutMobile_VoiTokenWeb</c>): sai kênh cũng phải bỏ qua khi gọi từ endpoint WEB với token mobile.</summary>
    [DbFact]
    public async Task LogoutWeb_VoiTokenMobile_TraVe204_VaTokenMobileVanSong()
    {
        var mobileClient = CreateMobileClient();
        var tokenMobile = await RegisterMobileAsync(mobileClient, NewEmail());

        var webClient = CreateWebClient();
        var logout = await webClient.SendAsync(WebLogoutRequest(tokenMobile));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stillWorks = await mobileClient.SendAsync(MobilePost("/api/auth/mobile/refresh", new { refreshToken = tokenMobile }));
        stillWorks.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record TokenDto(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);
}
