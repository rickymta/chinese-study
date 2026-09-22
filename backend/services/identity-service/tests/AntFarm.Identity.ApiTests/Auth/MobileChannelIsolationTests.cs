using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>
/// M1 §5.2.3 #4, #5, #8, #9 — hai kênh web (cookie) và mobile (body) HOÀN TOÀN cô lập: dùng SAI
/// kênh không thu hồi gì (RM-A3); endpoint mobile không đọc cookie dù trình duyệt tự gửi kèm
/// (RM-A1); đăng xuất/đổi mật khẩu phân biệt đúng "họ" theo kênh (RM-A7/RM-A8).
/// </summary>
[Collection(IdentityApiCollection.Name)]
public class MobileChannelIsolationTests(IdentityDbApiFactory factory) : IClassFixture<IdentityDbApiFactory>
{
    private static string NewEmail() => $"kenh-{Guid.NewGuid():N}@vidu.com";

    /// <summary>Client WEB — Origin hợp lệ mặc định (khớp CorsAndOriginTests/RefreshTests).</summary>
    private HttpClient CreateWebClient()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        return client;
    }

    /// <summary>Client MOBILE — KHÔNG Origin (app native).</summary>
    private HttpClient CreateMobileClient()
        => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });

    private static HttpRequestMessage MobilePost(string path, object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonDefaults.Options);
        request.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (android)");
        return request;
    }

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
        => response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith($"{cookieName}=")).Split(';')[0][(cookieName.Length + 1)..];

    private async Task<string> RegisterWebAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);
        response.EnsureSuccessStatusCode();
        return ExtractCookieValue(response, "af_rt");
    }

    private async Task<(string AccessToken, string RefreshToken)> RegisterMobileAsync(HttpClient client, string email)
    {
        var response = await client.SendAsync(MobilePost("/api/auth/mobile/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenDto>(JsonDefaults.Options);
        return (body!.AccessToken, body.RefreshToken);
    }

    [DbFact]
    public async Task TokenWeb_GuiToiMobileRefresh_TraVe401_VaVanLamMoiDuocQuaCookie()
    {
        var webClient = CreateWebClient();
        var cookieWeb = await RegisterWebAsync(webClient, NewEmail());

        var mobileClient = CreateMobileClient();
        var wrongChannel = await mobileClient.SendAsync(MobilePost("/api/auth/mobile/refresh", new { refreshToken = cookieWeb }));
        wrongChannel.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await wrongChannel.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("REFRESH_INVALID");

        // Token web KHÔNG bị thu hồi bởi lần gọi nhầm kênh ở trên — vẫn làm mới được qua cookie thật.
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"af_rt={cookieWeb}");
        request.Headers.Add("Origin", "http://localhost:3280");
        var stillWorks = await webClient.SendAsync(request);
        stillWorks.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DbFact]
    public async Task TokenMobile_GuiLamCookieToiWebRefresh_TraVe401_VaVanLamMoiDuocQuaMobile()
    {
        var mobileClient = CreateMobileClient();
        var (_, refreshTokenMobile) = await RegisterMobileAsync(mobileClient, NewEmail());

        var webClient = CreateWebClient();
        var wrongChannelRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        wrongChannelRequest.Headers.Add("Cookie", $"af_rt={refreshTokenMobile}");
        wrongChannelRequest.Headers.Add("Origin", "http://localhost:3280");
        var wrongChannel = await webClient.SendAsync(wrongChannelRequest);
        wrongChannel.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await wrongChannel.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("REFRESH_INVALID");

        var stillWorks = await mobileClient.SendAsync(MobilePost("/api/auth/mobile/refresh", new { refreshToken = refreshTokenMobile }));
        stillWorks.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DbFact]
    public async Task MobileRefresh_VoiCookieAfRtKemTheo_BiLoDiHoanToan()
    {
        var email = NewEmail();
        var webClient = CreateWebClient();
        var cookieWeb = await RegisterWebAsync(webClient, email);

        var mobileClient = CreateMobileClient();
        var (_, refreshTokenMobile) = await RegisterMobileAsync(mobileClient, NewEmail());

        var request = MobilePost("/api/auth/mobile/refresh", new { refreshToken = refreshTokenMobile });
        request.Headers.Add("Cookie", $"af_rt={cookieWeb}");

        var response = await mobileClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Token web đính kèm trong Cookie KHÔNG bị đụng tới — vẫn làm mới được nguyên vẹn.
        var stillWorksRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        stillWorksRequest.Headers.Add("Cookie", $"af_rt={cookieWeb}");
        stillWorksRequest.Headers.Add("Origin", "http://localhost:3280");
        var stillWorks = await webClient.SendAsync(stillWorksRequest);
        stillWorks.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DbFact]
    public async Task LogoutMobile_ThuHoiCaHo_RefreshSauDoTraVe401()
    {
        var mobileClient = CreateMobileClient();
        var email = NewEmail();
        var (_, refreshToken1) = await RegisterMobileAsync(mobileClient, email);

        // Xoay một lần để có 2 token active cùng họ (refreshToken1 đã xoay, refreshToken2 mới nhất).
        var rotated = await mobileClient.SendAsync(MobilePost("/api/auth/mobile/refresh", new { refreshToken = refreshToken1 }));
        var refreshToken2 = (await rotated.Content.ReadFromJsonAsync<TokenDto>(JsonDefaults.Options))!.RefreshToken;

        var logout = await mobileClient.SendAsync(MobilePost("/api/auth/mobile/logout", new { refreshToken = refreshToken2 }));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshAfterLogout = await mobileClient.SendAsync(MobilePost("/api/auth/mobile/refresh", new { refreshToken = refreshToken2 }));
        refreshAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task LogoutMobile_TokenLa_VanTraVe204()
    {
        var mobileClient = CreateMobileClient();
        var response = await mobileClient.SendAsync(MobilePost("/api/auth/mobile/logout", new { refreshToken = new string('a', 64) }));
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [DbFact]
    public async Task LogoutMobile_VoiTokenWeb_TraVe204_VaTokenWebVanSong()
    {
        var webClient = CreateWebClient();
        var cookieWeb = await RegisterWebAsync(webClient, NewEmail());

        var mobileClient = CreateMobileClient();
        var logout = await mobileClient.SendAsync(MobilePost("/api/auth/mobile/logout", new { refreshToken = cookieWeb }));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"af_rt={cookieWeb}");
        request.Headers.Add("Origin", "http://localhost:3280");
        var stillWorks = await webClient.SendAsync(request);
        stillWorks.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DbFact]
    public async Task DoiMatKhauMobile_CoRefreshTokenHienTai_GiuPhienMobile_ThuHoiPhienWebKhac()
    {
        var email = NewEmail();
        var mobileClient = CreateMobileClient();
        var (accessToken, refreshTokenMobile) = await RegisterMobileAsync(mobileClient, email);

        // Đăng nhập web (CÙNG tài khoản vừa đăng ký qua mobile ở trên) để có một họ WEB khác.
        var webClient = CreateWebClient();
        var loginWeb = await webClient.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-dung" }, JsonDefaults.Options);
        var cookieWebSession = ExtractCookieValue(loginWeb, "af_rt");

        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/password")
        {
            Content = JsonContent.Create(new { currentPassword = "mat-khau-dung", newPassword = "mat-khau-moi-du-dai", refreshToken = refreshTokenMobile }, options: JsonDefaults.Options)
        };
        changeRequest.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (android)");
        changeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var changeResponse = await mobileClient.SendAsync(changeRequest);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await changeResponse.Content.ReadFromJsonAsync<ChangePasswordResponseDto>(JsonDefaults.Options);
        result!.CurrentSessionKept.Should().BeTrue();
        result.OtherSessionsRevoked.Should().BeGreaterThanOrEqualTo(1);

        // Phiên mobile hiện tại vẫn sống.
        var refreshMobile = await mobileClient.SendAsync(MobilePost("/api/auth/mobile/refresh", new { refreshToken = refreshTokenMobile }));
        refreshMobile.StatusCode.Should().Be(HttpStatusCode.OK);

        // Phiên web (khác) bị thu hồi.
        var refreshWebSessionRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshWebSessionRequest.Headers.Add("Cookie", $"af_rt={cookieWebSession}");
        refreshWebSessionRequest.Headers.Add("Origin", "http://localhost:3280");
        var refreshWebSession = await webClient.SendAsync(refreshWebSessionRequest);
        refreshWebSession.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task DoiMatKhauMobile_KhongGuiRefreshToken_ThuHoiTatCa()
    {
        var email = NewEmail();
        var mobileClient = CreateMobileClient();
        var (accessToken, refreshTokenMobile) = await RegisterMobileAsync(mobileClient, email);

        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/password")
        {
            Content = JsonContent.Create(new { currentPassword = "mat-khau-dung", newPassword = "mat-khau-moi-du-dai" }, options: JsonDefaults.Options)
        };
        changeRequest.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (android)");
        changeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var changeResponse = await mobileClient.SendAsync(changeRequest);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await changeResponse.Content.ReadFromJsonAsync<ChangePasswordResponseDto>(JsonDefaults.Options);
        result!.CurrentSessionKept.Should().BeFalse();

        var refreshMobile = await mobileClient.SendAsync(MobilePost("/api/auth/mobile/refresh", new { refreshToken = refreshTokenMobile }));
        refreshMobile.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task DoiMatKhauMobile_SaiMatKhauHienTai_TraVe422()
    {
        var email = NewEmail();
        var mobileClient = CreateMobileClient();
        var (accessToken, refreshTokenMobile) = await RegisterMobileAsync(mobileClient, email);

        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/password")
        {
            Content = JsonContent.Create(new { currentPassword = "sai-mat-khau", newPassword = "mat-khau-moi-du-dai", refreshToken = refreshTokenMobile }, options: JsonDefaults.Options)
        };
        changeRequest.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (android)");
        changeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await mobileClient.SendAsync(changeRequest);
        response.StatusCode.Should().Be((HttpStatusCode)422);
        (await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("WRONG_PASSWORD");
    }

    private sealed record TokenDto(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record ChangePasswordResponseDto(int OtherSessionsRevoked, bool CurrentSessionKept);

    private sealed record ErrorDto(string Error, string Code);
}
