using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Internal;

/// <summary>§5.2.9, §6.5 — luồng đầy đủ khoá/mở/reset/thu hồi phiên qua API nội bộ, cùng tài khoản thật đăng ký/đăng nhập qua AuthController.</summary>
[Collection(IdentityApiCollection.Name)]
public class AccountAdminFlowTests(IdentityInternalApiFactory factory) : IClassFixture<IdentityInternalApiFactory>
{
    private static readonly Guid AdminActorId = Guid.NewGuid();
    private const string AdminActorEmail = "admin@vidu.com";

    private static string NewEmail() => $"hocvien-{Guid.NewGuid():N}@vidu.com";

    private HttpClient CreatePublicClient() => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
        HandleCookies = false,
        AllowAutoRedirect = false
    });

    private HttpRequestMessage InternalRequest(HttpMethod method, string path, Guid? actorId = null, string? actorEmail = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Test-Local-Port", IdentityInternalApiFactory.InternalPort.ToString());
        request.Headers.Add("X-Service-Key", IdentityInternalApiFactory.ServiceKey);
        if (actorId is not null)
            request.Headers.Add("X-Actor-Id", actorId.Value.ToString());
        if (actorEmail is not null)
            request.Headers.Add("X-Actor-Email", actorEmail);
        return request;
    }

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith($"{cookieName}="));
        return setCookie.Split(';')[0][(cookieName.Length + 1)..];
    }

    private async Task<(Guid AccountId, string RefreshCookie)> RegisterAndLoginAsync(HttpClient client, string email)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-dung", displayName = "Học viên A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);
        registerResponse.EnsureSuccessStatusCode();
        var registerAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonDefaults.Options);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-dung" }, JsonDefaults.Options);
        loginResponse.EnsureSuccessStatusCode();
        var refreshCookie = ExtractCookieValue(loginResponse, "af_rt");

        return (registerAuth!.Account.Id, refreshCookie);
    }

    [DbFact]
    public async Task LuongDayDu_KhoaMoResetThuHoi()
    {
        var publicClient = CreatePublicClient();
        publicClient.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        var email = NewEmail();
        var (accountId, refreshCookieBeforeDisable) = await RegisterAndLoginAsync(publicClient, email);

        var internalClient = CreatePublicClient(); // dùng chung factory, chỉ khác header theo từng request

        // ── disable ──
        var disableResponse = await internalClient.SendAsync(
            InternalRequest(HttpMethod.Post, $"/internal/accounts/{accountId}/disable", AdminActorId, AdminActorEmail));
        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Refresh bằng cookie cũ không cấp token mới nữa (tài khoản đã khoá).
        var refreshWhileDisabled = await publicClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh") { Headers = { { "Cookie", $"af_rt={refreshCookieBeforeDisable}" } } });
        refreshWhileDisabled.IsSuccessStatusCode.Should().BeFalse();

        // Đăng nhập cũng bị chặn.
        var loginWhileDisabled = await publicClient.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-dung" }, JsonDefaults.Options);
        loginWhileDisabled.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var loginWhileDisabledError = await loginWhileDisabled.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        loginWhileDisabledError!.Code.Should().Be("ACCOUNT_DISABLED");

        // ── enable ──
        var enableResponse = await internalClient.SendAsync(
            InternalRequest(HttpMethod.Post, $"/internal/accounts/{accountId}/enable", AdminActorId, AdminActorEmail));
        enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginAfterEnable = await publicClient.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-dung" }, JsonDefaults.Options);
        loginAfterEnable.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshCookieAfterEnable = ExtractCookieValue(loginAfterEnable, "af_rt");

        // ── reset-password (sinh mật khẩu tạm) ──
        var resetResponse = await internalClient.SendAsync(
            InternalRequest(HttpMethod.Post, $"/internal/accounts/{accountId}/reset-password", AdminActorId, AdminActorEmail));
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        resetResponse.Headers.CacheControl!.NoStore.Should().BeTrue();
        var resetBody = await resetResponse.Content.ReadFromJsonAsync<ResetPasswordResultDto>(JsonDefaults.Options);
        resetBody!.TemporaryPassword.Should().HaveLength(16);
        resetBody.RevokedSessions.Should().BeGreaterThanOrEqualTo(1);

        // Mật khẩu cũ không dùng được nữa.
        var loginWithOldPassword = await publicClient.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-dung" }, JsonDefaults.Options);
        loginWithOldPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Mật khẩu tạm đăng nhập được.
        var loginWithTempPassword = await publicClient.PostAsJsonAsync("/api/auth/login", new { email, password = resetBody.TemporaryPassword }, JsonDefaults.Options);
        loginWithTempPassword.StatusCode.Should().Be(HttpStatusCode.OK);

        // Refresh token cấp TRƯỚC reset đã bị thu hồi.
        var refreshAfterReset = await publicClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh") { Headers = { { "Cookie", $"af_rt={refreshCookieAfterEnable}" } } });
        refreshAfterReset.IsSuccessStatusCode.Should().BeFalse();
    }

    [DbFact]
    public async Task ResetPassword_MatKhauMoiQuaNgan_TraVe400()
    {
        var publicClient = CreatePublicClient();
        publicClient.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        var (accountId, _) = await RegisterAndLoginAsync(publicClient, NewEmail());

        var request = InternalRequest(HttpMethod.Post, $"/internal/accounts/{accountId}/reset-password", AdminActorId, AdminActorEmail);
        request.Content = JsonContent.Create(new { newPassword = "ngan123" }, options: JsonDefaults.Options);

        var response = await publicClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [DbFact]
    public async Task TuThaoTacLenChinhMinh_TraVe422()
    {
        var publicClient = CreatePublicClient();
        publicClient.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        var (accountId, _) = await RegisterAndLoginAsync(publicClient, NewEmail());

        var response = await publicClient.SendAsync(
            InternalRequest(HttpMethod.Post, $"/internal/accounts/{accountId}/disable", accountId, "chinh-minh@vidu.com"));

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("SELF_ACTION_FORBIDDEN");
    }

    [DbFact]
    public async Task RevokeSessions_ThuHoiDungSoPhien_RefreshCuHong()
    {
        var publicClient = CreatePublicClient();
        publicClient.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        var (accountId, refreshCookie) = await RegisterAndLoginAsync(publicClient, NewEmail());

        var response = await publicClient.SendAsync(
            InternalRequest(HttpMethod.Post, $"/internal/accounts/{accountId}/revoke-sessions", AdminActorId, AdminActorEmail));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RevokeSessionsResultDto>(JsonDefaults.Options);
        body!.RevokedSessions.Should().BeGreaterThanOrEqualTo(1);

        var refreshAfterRevoke = await publicClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh") { Headers = { { "Cookie", $"af_rt={refreshCookie}" } } });
        refreshAfterRevoke.IsSuccessStatusCode.Should().BeFalse();
    }

    [DbFact]
    public async Task ClearLockout_SauKhiDangNhapSaiDuNguong_LoginDungDuocNgay()
    {
        var publicClient = CreatePublicClient();
        publicClient.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        var email = NewEmail();
        var (accountId, _) = await RegisterAndLoginAsync(publicClient, email);

        for (var i = 0; i < 10; i++)
            await publicClient.PostAsJsonAsync("/api/auth/login", new { email, password = "sai" }, JsonDefaults.Options);

        var lockedLogin = await publicClient.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-dung" }, JsonDefaults.Options);
        lockedLogin.StatusCode.Should().Be((HttpStatusCode)423);

        var clearLockoutResponse = await publicClient.SendAsync(
            InternalRequest(HttpMethod.Post, $"/internal/accounts/{accountId}/clear-lockout", AdminActorId, AdminActorEmail));
        clearLockoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginAfterClear = await publicClient.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-dung" }, JsonDefaults.Options);
        loginAfterClear.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DbFact]
    public async Task GetAccount_TraVe404KhiKhongTonTai()
    {
        var publicClient = CreatePublicClient();

        var response = await publicClient.SendAsync(InternalRequest(HttpMethod.Get, $"/internal/accounts/{Guid.NewGuid()}"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [DbFact]
    public async Task ListAccounts_LocTheoStatusDisabled_ChiTraVeTaiKhoanDaKhoa()
    {
        var publicClient = CreatePublicClient();
        publicClient.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        var email = NewEmail();
        var (accountId, _) = await RegisterAndLoginAsync(publicClient, email);
        await publicClient.SendAsync(InternalRequest(HttpMethod.Post, $"/internal/accounts/{accountId}/disable", AdminActorId, AdminActorEmail));

        var response = await publicClient.SendAsync(InternalRequest(HttpMethod.Get, "/internal/accounts?status=disabled&pageSize=100"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountListResultDto>(JsonDefaults.Options);
        body!.Items.Should().Contain(i => i.Id == accountId);
        body.Items.Should().OnlyContain(i => !i.IsActive);
    }

    private sealed record AuthResponseDto(string AccessToken, DateTime AccessTokenExpiresAt, AccountDto Account);

    private sealed record AccountDto(Guid Id, string Email, string DisplayName, string TimeZone, DateTime CreatedAt);

    private sealed record ErrorDto(string Error, string Code);

    private sealed record ResetPasswordResultDto(string? TemporaryPassword, int RevokedSessions);

    private sealed record RevokeSessionsResultDto(int RevokedSessions);

    private sealed record AccountListItemDto(Guid Id, string Email, bool IsActive);

    private sealed record AccountListResultDto(List<AccountListItemDto> Items, int Page, int PageSize, int TotalCount);
}
