using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>
/// D21/D22 (2026-09-17): đổi mật khẩu chuyển sang <c>POST /api/auth/password</c> (không phải
/// <c>/api/account/password</c> của §6.2 gốc) vì cookie af_rt chỉ gửi tới path /api/auth (RK34).
/// </summary>
[Collection(IdentityApiCollection.Name)]
public class ChangePasswordTests(IdentityDbApiFactory factory) : IClassFixture<IdentityDbApiFactory>
{
    private HttpClient CreateClient()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        return client;
    }

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
        => response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith($"{cookieName}=")).Split(';')[0][(cookieName.Length + 1)..];

    private async Task<(string AccessToken, string RefreshCookie)> RegisterAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password, displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonDefaults.Options);
        return (body!.AccessToken, ExtractCookieValue(response, "af_rt"));
    }

    private static HttpRequestMessage BuildPasswordRequest(string accessToken, string? cookieValue, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/password")
        {
            Content = JsonContent.Create(payload, options: JsonDefaults.Options)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (cookieValue is not null)
            request.Headers.Add("Cookie", $"af_rt={cookieValue}");
        return request;
    }

    [DbFact]
    public async Task DoiMatKhau_CoCookiePhienHienTai_GiuPhienHienTaiThuHoiPhienKhac()
    {
        var client = CreateClient();
        var email = $"doimk-{Guid.NewGuid():N}@vidu.com";
        var (accessTokenA, cookieA) = await RegisterAsync(client, email, "mat-khau-cu-du-dai");

        // Phiên B: đăng nhập thêm một nơi khác (cùng tài khoản) để có một họ refresh token KHÁC.
        var loginB = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-cu-du-dai" }, JsonDefaults.Options);
        var cookieB = ExtractCookieValue(loginB, "af_rt");

        var changeResponse = await client.SendAsync(BuildPasswordRequest(accessTokenA, cookieA,
            new { currentPassword = "mat-khau-cu-du-dai", newPassword = "mat-khau-moi-du-dai" }));

        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await changeResponse.Content.ReadFromJsonAsync<ChangePasswordResponseDto>(JsonDefaults.Options);
        result!.CurrentSessionKept.Should().BeTrue();
        result.OtherSessionsRevoked.Should().Be(1);

        // Phiên A (cookie vừa dùng để đổi mật khẩu) vẫn làm mới được.
        var refreshA = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh") { Headers = { { "Cookie", $"af_rt={cookieA}" } } });
        refreshA.StatusCode.Should().Be(HttpStatusCode.OK);

        // Phiên B bị thu hồi.
        var refreshB = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh") { Headers = { { "Cookie", $"af_rt={cookieB}" } } });
        refreshB.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task DoiMatKhau_KhongCoCookie_ThuHoiTatCaPhien()
    {
        var client = CreateClient();
        var email = $"doimk-nocookie-{Guid.NewGuid():N}@vidu.com";
        var (accessToken, cookieA) = await RegisterAsync(client, email, "mat-khau-cu-du-dai");

        var changeResponse = await client.SendAsync(BuildPasswordRequest(accessToken, cookieValue: null,
            new { currentPassword = "mat-khau-cu-du-dai", newPassword = "mat-khau-moi-du-dai" }));

        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await changeResponse.Content.ReadFromJsonAsync<ChangePasswordResponseDto>(JsonDefaults.Options);
        result!.CurrentSessionKept.Should().BeFalse();
        result.OtherSessionsRevoked.Should().Be(1);

        var refreshA = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh") { Headers = { { "Cookie", $"af_rt={cookieA}" } } });
        refreshA.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task DoiMatKhau_SaiMatKhauHienTai_TraVe422VaKhongTangDemKhoa()
    {
        var client = CreateClient();
        var email = $"doimk-sai-{Guid.NewGuid():N}@vidu.com";
        var (accessToken, cookieA) = await RegisterAsync(client, email, "mat-khau-cu-du-dai");

        var changeResponse = await client.SendAsync(BuildPasswordRequest(accessToken, cookieA,
            new { currentPassword = "sai-mat-khau", newPassword = "mat-khau-moi-du-dai" }));

        changeResponse.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await changeResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("WRONG_PASSWORD");

        // D22: sai mật khẩu hiện tại KHÔNG tính vào đếm khoá đăng nhập — mật khẩu CŨ vẫn đăng nhập được ngay.
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-cu-du-dai" }, JsonDefaults.Options);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DbFact]
    public async Task DoiMatKhau_MatKhauMoiTrungMatKhauCu_TraVe422PasswordUnchanged()
    {
        var client = CreateClient();
        var email = $"doimk-trung-{Guid.NewGuid():N}@vidu.com";
        var (accessToken, cookieA) = await RegisterAsync(client, email, "mat-khau-cu-du-dai");

        var changeResponse = await client.SendAsync(BuildPasswordRequest(accessToken, cookieA,
            new { currentPassword = "mat-khau-cu-du-dai", newPassword = "mat-khau-cu-du-dai" }));

        changeResponse.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await changeResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("PASSWORD_UNCHANGED");
    }

    [DbFact]
    public async Task DoiMatKhau_VoiOriginLa_TraVe403()
    {
        var client = CreateClient();
        var email = $"doimk-origin-{Guid.NewGuid():N}@vidu.com";
        var (accessToken, cookieA) = await RegisterAsync(client, email, "mat-khau-cu-du-dai");

        var request = BuildPasswordRequest(accessToken, cookieA, new { currentPassword = "mat-khau-cu-du-dai", newPassword = "mat-khau-moi-du-dai" });
        request.Headers.Remove("Origin");
        request.Headers.Add("Origin", "https://khong-hop-le.example.com");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be((HttpStatusCode)403);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("ORIGIN_NOT_ALLOWED");
    }

    [DbFact]
    public async Task DoiMatKhau_ThieuBearer_TraVe401()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/password",
            new { currentPassword = "x", newPassword = "mat-khau-moi-du-dai" }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record AuthResponseDto(string AccessToken, DateTime AccessTokenExpiresAt, object Account);

    private sealed record ChangePasswordResponseDto(int OtherSessionsRevoked, bool CurrentSessionKept);

    private sealed record ErrorDto(string Error, string Code);
}
