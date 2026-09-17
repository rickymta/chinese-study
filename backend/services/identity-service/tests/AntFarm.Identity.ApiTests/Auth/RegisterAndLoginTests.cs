using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

[Collection(IdentityApiCollection.Name)]
public class RegisterAndLoginTests(IdentityDbApiFactory factory) : IClassFixture<IdentityDbApiFactory>
{
    private static string NewEmail() => $"hocvien-{Guid.NewGuid():N}@vidu.com";

    /// <summary>Origin mặc định luôn hợp lệ — [ValidateOrigin] áp cho MỌI action của AuthController (R-A7b) nên mọi lời gọi test đều cần header này, không chỉ các test kiểm CORS riêng.</summary>
    private HttpClient CreateClient()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        return client;
    }

    [DbFact]
    public async Task Register_VoiEmailMoi_TraVe201VaCookieDungCauHinhDev()
    {
        var client = CreateClient();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-du-dai", displayName = "Học viên", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonDefaults.Options);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Account.Email.Should().Be(email);

        response.Headers.TryGetValues("Set-Cookie", out var setCookies).Should().BeTrue();
        // ASP.NET Core ghi thuộc tính cookie bằng CHỮ THƯỜNG (path=, samesite=strict, httponly...).
        var cookie = setCookies!.Single(c => c.StartsWith("af_rt="));
        cookie.Should().Contain("path=/identity/api/auth");
        cookie.Should().Contain("httponly");
        cookie.Should().Contain("samesite=strict");
        cookie.Should().NotContain("domain="); // dev: rỗng ⇒ không đặt Domain (host-only)
        cookie.Should().NotContain("secure"); // dev: RefreshCookieSecure=false
    }

    [DbFact]
    public async Task Register_TrungEmailKhacHoaThuong_TraVe409()
    {
        var client = CreateClient();
        var email = NewEmail();

        (await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-du-dai", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" },
            JsonDefaults.Options)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = email.ToUpperInvariant(), password = "mat-khau-khac", displayName = "B", timeZone = "Asia/Ho_Chi_Minh" },
            JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("EMAIL_TAKEN");
    }

    [DbFact]
    public async Task Register_MatKhauQuaNgan_TraVe400Validation()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = "123", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" },
            JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("VALIDATION");
        error.Details!.Value.TryGetProperty("password", out _).Should().BeTrue();
    }

    [DbFact]
    public async Task Register_MuiGioKhongHopLe_TraVe422()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = "mat-khau-du-dai", displayName = "A", timeZone = "Mars/Olympus" },
            JsonDefaults.Options);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("INVALID_TIME_ZONE");
    }

    [DbFact]
    public async Task Login_SaiMatKhau_TraVe401KhongPhanBietLoai()
    {
        var client = CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-sai" }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [DbFact]
    public async Task Login_EmailKhongTonTai_TraVe401CungMaLoiNhuSaiMatKhau()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = NewEmail(), password = "khong-quan-trong" }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [DbFact]
    public async Task Login_SaiMatKhau10Lan_KhoaTaiKhoan423()
    {
        var client = CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);

        for (var i = 0; i < 9; i++)
            await client.PostAsJsonAsync("/api/auth/login", new { email, password = "sai" }, JsonDefaults.Options);

        var last = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "sai" }, JsonDefaults.Options);
        last.StatusCode.Should().Be((HttpStatusCode)423);
        var error = await last.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("ACCOUNT_LOCKED");

        // Ngay cả đúng mật khẩu cũng bị chặn trong lúc khoá.
        var whileLocked = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-dung" }, JsonDefaults.Options);
        whileLocked.StatusCode.Should().Be((HttpStatusCode)423);
    }

    [DbFact]
    public async Task Login_ThanhCong_TraVe200VaCookieMoi()
    {
        var client = CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "mat-khau-dung" }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out _).Should().BeTrue();
    }

    private sealed record AuthResponseDto(string AccessToken, DateTime AccessTokenExpiresAt, AccountDto Account);

    private sealed record AccountDto(string Id, string Email, string DisplayName, string TimeZone, DateTime CreatedAt);

    private sealed record ErrorDto(string Error, string Code, JsonElement? Details);
}
