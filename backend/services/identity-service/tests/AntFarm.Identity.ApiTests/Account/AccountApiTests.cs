using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Identity.Infrastructure.Persistence;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace AntFarm.Identity.ApiTests.Account;

/// <summary>
/// F4 (§5.2.2, R4-1..R4-4) — GET/PUT /api/account: đổi tên hiển thị/múi giờ, refresh NGAY sau đó
/// phải đọc lại hồ sơ từ DB (RK10 phía chinese-backend phụ thuộc vào việc claim đổi được truyền
/// đúng ở đây; test này chỉ kiểm PHÍA identity-service — access token mới có claim mới).
/// </summary>
[Collection(IdentityApiCollection.Name)]
public class AccountApiTests(IdentityDbApiFactory factory) : IClassFixture<IdentityDbApiFactory>
{
    private HttpClient CreateClient()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        return client;
    }

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
        => response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith($"{cookieName}=")).Split(';')[0][(cookieName.Length + 1)..];

    private async Task<(string AccessToken, string RefreshCookie)> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-dung", displayName = "Ten Cu", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonDefaults.Options);
        return (body!.AccessToken, ExtractCookieValue(response, "af_rt"));
    }

    private static HttpRequestMessage BuildPutAccountRequest(string accessToken, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/account") { Content = JsonContent.Create(payload, options: JsonDefaults.Options) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    [DbFact]
    public async Task Put_TenVaMuiGioHopLe_TraVe200VaCapNhatHoSo()
    {
        var client = CreateClient();
        var (accessToken, _) = await RegisterAsync(client, $"put-{Guid.NewGuid():N}@vidu.com");

        var response = await client.SendAsync(BuildPutAccountRequest(accessToken, new { displayName = "Ten Moi", timeZone = "Europe/Berlin" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<AccountDto>(JsonDefaults.Options);
        account!.DisplayName.Should().Be("Ten Moi");
        account.TimeZone.Should().Be("Europe/Berlin");
    }

    [DbFact]
    public async Task Put_RoiRefreshNgay_AccessTokenMoiCoClaimMoi()
    {
        var client = CreateClient();
        var email = $"refresh-claim-{Guid.NewGuid():N}@vidu.com";
        var (accessToken, cookie) = await RegisterAsync(client, email);

        var putResponse = await client.SendAsync(BuildPutAccountRequest(accessToken, new { displayName = "Ten Sau Khi Doi", timeZone = "Europe/Berlin" }));
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // R4-4: KHÔNG chờ gì — refresh NGAY sau lượt lưu phải phản ánh hồ sơ MỚI (đọc lại từ DB, không chép claim của access token cũ).
        var refreshResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh") { Headers = { { "Cookie", $"af_rt={cookie}" } } });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<RefreshResponseDto>(JsonDefaults.Options);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(refreshed!.AccessToken);
        jwt.TryGetClaim("name", out var nameClaim).Should().BeTrue();
        nameClaim.Value.Should().Be("Ten Sau Khi Doi");
        jwt.TryGetClaim("zoneinfo", out var tzClaim).Should().BeTrue();
        tzClaim.Value.Should().Be("Europe/Berlin");
    }

    [DbFact]
    public async Task Put_MuiGioKhongHopLe_TraVe422InvalidTimeZone()
    {
        var client = CreateClient();
        var (accessToken, _) = await RegisterAsync(client, $"badtz-{Guid.NewGuid():N}@vidu.com");

        var response = await client.SendAsync(BuildPutAccountRequest(accessToken, new { displayName = "A", timeZone = "Mars/Olympus" }));

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("INVALID_TIME_ZONE");
    }

    [DbFact]
    public async Task Put_TenHienThiToanKhoangTrang_TraVe400Validation()
    {
        var client = CreateClient();
        var (accessToken, _) = await RegisterAsync(client, $"blank-{Guid.NewGuid():N}@vidu.com");

        var response = await client.SendAsync(BuildPutAccountRequest(accessToken, new { displayName = "   ", timeZone = "Asia/Ho_Chi_Minh" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("VALIDATION");
    }

    [DbFact]
    public async Task Get_ThieuBearer_TraVe401()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/account");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// §6.2: tài khoản is_active=false ⇒ 403 ACCOUNT_DISABLED ở cả GET và PUT /api/account. Chưa
    /// có API khoá tài khoản (F13) — test tự đặt cờ thẳng qua DB để mô phỏng trạng thái đã bị khoá.
    /// </summary>
    private async Task SetAccountDisabledAsync(Guid accountId)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(TestDatabase.BuildConnectionString("af_identity_test"), npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var db = new IdentityDbContext(options);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE identity.accounts SET is_active = false WHERE id = {accountId}");
    }

    [DbFact]
    public async Task Get_TaiKhoanBiKhoa_TraVe403AccountDisabled()
    {
        var client = CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"disabled-get-{Guid.NewGuid():N}@vidu.com", password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" },
            JsonDefaults.Options);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonDefaults.Options);
        await SetAccountDisabledAsync(auth!.Account.Id);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/account");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be((HttpStatusCode)403);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("ACCOUNT_DISABLED");
    }

    [DbFact]
    public async Task Put_TaiKhoanBiKhoa_TraVe403AccountDisabled()
    {
        var client = CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"disabled-put-{Guid.NewGuid():N}@vidu.com", password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" },
            JsonDefaults.Options);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonDefaults.Options);
        await SetAccountDisabledAsync(auth!.Account.Id);

        var response = await client.SendAsync(BuildPutAccountRequest(auth.AccessToken, new { displayName = "B", timeZone = "Asia/Ho_Chi_Minh" }));

        response.StatusCode.Should().Be((HttpStatusCode)403);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("ACCOUNT_DISABLED");
    }

    private sealed record AuthResponseDto(string AccessToken, DateTime AccessTokenExpiresAt, AccountDto Account);

    private sealed record RefreshResponseDto(string AccessToken, DateTime AccessTokenExpiresAt);

    private sealed record AccountDto(Guid Id, string Email, string DisplayName, string TimeZone, DateTime CreatedAt);

    private sealed record ErrorDto(string Error, string Code);
}
