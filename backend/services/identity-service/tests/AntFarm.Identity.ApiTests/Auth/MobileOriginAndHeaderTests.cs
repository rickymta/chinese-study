using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>M1 §5.2.3 #6, #7 — RM-A4 (chặn trình duyệt) + RM-A5 (header X-AF-Client bắt buộc).</summary>
[Collection(IdentityApiCollection.Name)]
public class MobileOriginAndHeaderTests(IdentityDbApiFactory factory) : IClassFixture<IdentityDbApiFactory>
{
    private static string NewEmail() => $"origin-{Guid.NewGuid():N}@vidu.com";

    private HttpClient CreateClient() => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });

    private static object RegisterBody(string email) => new { email, password = "mat-khau-du-dai", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" };

    public static IEnumerable<object[]> MobileEndpoints()
    {
        yield return ["/api/auth/mobile/register", (object)RegisterBody(NewEmail())];
        yield return ["/api/auth/mobile/login", (object)new { email = NewEmail(), password = "khong-quan-trong" }];
        yield return ["/api/auth/mobile/refresh", (object)new { refreshToken = new string('a', 64) }];
        yield return ["/api/auth/mobile/logout", (object)new { refreshToken = new string('a', 64) }];
    }

    [DbTheory]
    [MemberData(nameof(MobileEndpoints))]
    public async Task Origin_LaKhongHopLe_TraVe403OMoiEndpoint(string path, object body)
    {
        var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, options: JsonDefaults.Options) };
        request.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (android)");
        request.Headers.Add("Origin", "https://evil.example");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be((HttpStatusCode)403);
        (await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("ORIGIN_NOT_ALLOWED");
    }

    /// <summary>
    /// M1 §5.2.3 #6 — endpoint thứ 5 (<c>/mobile/password</c>) không có mặt trong
    /// <see cref="MobileEndpoints"/> vì cần Bearer HỢP LỆ trước: <c>[Authorize]</c> là
    /// AUTHORIZATION FILTER, chạy ở giai đoạn pipeline SỚM HƠN các action filter
    /// (<see cref="AntFarm.Identity.Api.Configuration.RejectBrowserOriginAttribute"/>) — thiếu
    /// Bearer sẽ trả 401 TRƯỚC KHI kịp kiểm Origin, che mất đúng behaviour cần kiểm ở đây.
    /// </summary>
    [DbFact]
    public async Task Origin_LaKhongHopLe_TraVe403_TrenEndpointPasswordCungBearerHopLe()
    {
        var client = CreateClient();
        var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/register")
        {
            Content = JsonContent.Create(RegisterBody(NewEmail()), options: JsonDefaults.Options)
        };
        registerRequest.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (android)");
        var registerResponse = await client.SendAsync(registerRequest);
        var accessToken = (await registerResponse.Content.ReadFromJsonAsync<TokenDto>(JsonDefaults.Options))!.AccessToken;

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/password")
        {
            Content = JsonContent.Create(new { currentPassword = "mat-khau-du-dai", newPassword = "mat-khau-moi-du-dai" }, options: JsonDefaults.Options)
        };
        request.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (android)");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be((HttpStatusCode)403);
        (await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("ORIGIN_NOT_ALLOWED");
    }

    [DbFact]
    public async Task Origin_DevOrigin_MoiTruongTesting_KhongPhaiDevelopment_VanBiChan()
    {
        // IdentityDbApiFactory chạy môi trường "Testing" (không phải "Development") — MobileDevOrigins
        // (dù có cấu hình ở factory khác) chỉ có tác dụng đúng Development (RM-A4).
        var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/login")
        {
            Content = JsonContent.Create(new { email = NewEmail(), password = "khong-quan-trong" }, options: JsonDefaults.Options)
        };
        request.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (android)");
        request.Headers.Add("Origin", "http://localhost:3291");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be((HttpStatusCode)403);
    }

    [DbFact]
    public async Task Origin_KhongCoOrigin_KhongBiChanBoiFilterOrigin()
    {
        var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/login")
        {
            Content = JsonContent.Create(new { email = NewEmail(), password = "khong-quan-trong" }, options: JsonDefaults.Options)
        };
        request.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (android)");

        var response = await client.SendAsync(request);

        // Không tồn tại tài khoản ⇒ 401 INVALID_CREDENTIALS — quan trọng là KHÔNG 403 ORIGIN_NOT_ALLOWED.
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbTheory]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("chinese-mobile/1.0 (windows)")]
    public async Task ThieuHoacSaiHeaderXAfClient_TraVe400Validation(string? header)
    {
        var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/login")
        {
            Content = JsonContent.Create(new { email = NewEmail(), password = "khong-quan-trong" }, options: JsonDefaults.Options)
        };
        if (header is not null)
            request.Headers.Add("X-AF-Client", header);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be((HttpStatusCode)400);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("VALIDATION");
        error.Details!.Value.TryGetProperty("X-AF-Client", out _).Should().BeTrue();
    }

    [DbFact]
    public async Task HeaderXAfClientHopLe_KhongBiChan()
    {
        var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/login")
        {
            Content = JsonContent.Create(new { email = NewEmail(), password = "khong-quan-trong" }, options: JsonDefaults.Options)
        };
        request.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (ios)");

        var response = await client.SendAsync(request);

        // Header hợp lệ ⇒ KHÔNG 400 VALIDATION (tài khoản không tồn tại nên 401 INVALID_CREDENTIALS).
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// G1 (review M1) — <c>[DisableCors]</c> trên <see cref="AntFarm.Identity.Api.Features.Auth.MobileAuthController"/>:
    /// preflight KHÔNG BAO GIỜ nhận <c>Access-Control-Allow-Origin</c>, kể cả origin có trong
    /// <c>Auth:AllowedOrigins</c> của WEB (CORS của web hoàn toàn không áp dụng cho luồng mobile —
    /// trình duyệt tự chặn NGAY ở bước preflight, không cần đợi <c>RejectBrowserOriginAttribute</c>).
    /// </summary>
    [DbFact]
    public async Task Preflight_MoiOrigin_KhongCoAccessControlAllowOrigin_DoCorsBiTat()
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/mobile/login");
        request.Headers.Add("Origin", "https://chinese.antfarms.xyz");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
    }

    private sealed record TokenDto(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

    private sealed record ErrorDto(string Error, string Code, JsonElement? Details);
}

/// <summary>M1 §5.2.3 #6 — môi trường THẬT Development + Origin ∈ MobileDevOrigins ⇒ cho qua.</summary>
[Collection(IdentityApiCollection.Name)]
public class MobileOriginDevOriginTests(IdentityDbApiFactoryMobileDevOrigin factory) : IClassFixture<IdentityDbApiFactoryMobileDevOrigin>
{
    [DbFact]
    public async Task Origin_TrongMobileDevOrigins_MoiTruongDevelopment_ChoQua()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/login")
        {
            Content = JsonContent.Create(new { email = $"devorigin-{Guid.NewGuid():N}@vidu.com", password = "khong-quan-trong" }, options: JsonDefaults.Options)
        };
        request.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (web)");
        request.Headers.Add("Origin", "http://localhost:3291");

        var response = await client.SendAsync(request);

        // Không có tài khoản này ⇒ 401 INVALID_CREDENTIALS — quan trọng là KHÔNG 403 ORIGIN_NOT_ALLOWED.
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

/// <summary>M1 §5.2.3 #6 — môi trường THẬT Production, DÙ Auth:MobileDevOrigins có cấu hình cũng bị BỎ QUA (Program.cs log Warning lúc khởi động).</summary>
[Collection(IdentityApiCollection.Name)]
public class MobileOriginDevOriginProdTests(IdentityDbApiFactoryMobileDevOriginProd factory) : IClassFixture<IdentityDbApiFactoryMobileDevOriginProd>
{
    [DbFact]
    public async Task Origin_TrongMobileDevOrigins_MoiTruongProduction_VanBiChan()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/login")
        {
            Content = JsonContent.Create(new { email = $"devoriginprod-{Guid.NewGuid():N}@vidu.com", password = "khong-quan-trong" }, options: JsonDefaults.Options)
        };
        request.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (web)");
        request.Headers.Add("Origin", "http://localhost:3291");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be((HttpStatusCode)403);
    }
}
