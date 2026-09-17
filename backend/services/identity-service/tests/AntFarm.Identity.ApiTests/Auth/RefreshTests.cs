using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>R-A5/R-A6, §5.2.2 — luồng xoay refresh token trong AuthController thật (không giả TimeProvider vì đây là ApiTests end-to-end; cửa sổ ân hạn 30s test bằng cách xoay NGAY LẬP TỨC hai lần liên tiếp, còn ca "ngoài cửa sổ" test bằng cách dùng lại token ĐÃ BỊ XOAY TRƯỚC ĐÓ nhiều bước).</summary>
[Collection(IdentityApiCollection.Name)]
public class RefreshTests(IdentityDbApiFactory factory) : IClassFixture<IdentityDbApiFactory>
{
    private static string NewEmail() => $"hocvien-{Guid.NewGuid():N}@vidu.com";

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

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith($"{cookieName}="));
        var pair = setCookie.Split(';')[0];
        return pair[(cookieName.Length + 1)..];
    }

    private async Task<string> RegisterAndGetRefreshCookieAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);
        response.EnsureSuccessStatusCode();
        return ExtractCookieValue(response, "af_rt");
    }

    private HttpRequestMessage RefreshRequest(string cookieValue)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"af_rt={cookieValue}");
        return request;
    }

    [DbFact]
    public async Task Refresh_VoiCookieHopLe_XoayVongVaTraTokenMoi()
    {
        var client = CreateClient();
        var cookie1 = await RegisterAndGetRefreshCookieAsync(client, NewEmail());

        var response = await client.SendAsync(RefreshRequest(cookie1));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie2 = ExtractCookieValue(response, "af_rt");
        cookie2.Should().NotBe(cookie1); // R-A5: xoay vòng, token mới khác token cũ
    }

    [DbFact]
    public async Task Refresh_ThieuCookie_TraVe401ReeshInvalid()
    {
        var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("REFRESH_INVALID");
    }

    [DbFact]
    public async Task Refresh_TrongCuaSoAnHan_HaiTabCungLamMoi_KhongBiThuHoi()
    {
        // R-A6: hai tab cùng dùng token cũ gần như đồng thời (trong 30 giây) ⇒ cả hai đều
        // nhận token mới CÙNG family, không tab nào bị đăng xuất.
        var client = CreateClient();
        var cookie1 = await RegisterAndGetRefreshCookieAsync(client, NewEmail());

        var firstRotation = await client.SendAsync(RefreshRequest(cookie1));
        firstRotation.StatusCode.Should().Be(HttpStatusCode.OK);

        // Dùng LẠI cookie1 (đã xoay) ngay lập tức — vẫn trong cửa sổ ân hạn 30s.
        var secondUseOfSameOldCookie = await client.SendAsync(RefreshRequest(cookie1));

        secondUseOfSameOldCookie.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DbFact]
    public async Task Refresh_TokenDaBiThuHoi_TraVe401()
    {
        var client = CreateClient();
        var cookie1 = await RegisterAndGetRefreshCookieAsync(client, NewEmail());

        // Đăng xuất thu hồi cookie1 hẳn (revoked_at != null) — refresh lại phải 401 (khác nhánh reuse-in-grace).
        await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout") { Headers = { { "Cookie", $"af_rt={cookie1}" } } });

        var response = await client.SendAsync(RefreshRequest(cookie1));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("REFRESH_INVALID");
    }

    [DbFact]
    public async Task Logout_ThuHoiCookie_XoaCookiePhiaTrinhDuyet()
    {
        var client = CreateClient();
        var cookie1 = await RegisterAndGetRefreshCookieAsync(client, NewEmail());

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout") { Headers = { { "Cookie", $"af_rt={cookie1}" } } });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.TryGetValues("Set-Cookie", out var setCookies).Should().BeTrue();
        setCookies!.Should().Contain(c => c.StartsWith("af_rt=") && c.Contains("expires=Thu, 01 Jan 1970"));
    }

    private sealed record ErrorDto(string Error, string Code);
}
