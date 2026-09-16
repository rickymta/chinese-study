using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>R-A7b — CORS có credentials + kiểm Origin trên MỌI POST /api/auth/* (RK18, RK19, RK26).</summary>
[Collection(IdentityApiCollection.Name)]
public class CorsAndOriginTests(IdentityDbApiFactory factory) : IClassFixture<IdentityDbApiFactory>
{
    private static string NewEmail() => $"hocvien-{Guid.NewGuid():N}@vidu.com";

    private HttpClient CreateClientWithoutDefaultOrigin() => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
        HandleCookies = false,
        AllowAutoRedirect = false
    });

    [DbFact]
    public async Task Post_VoiOriginLa_TraVe403OriginNotAllowed()
    {
        var client = CreateClientWithoutDefaultOrigin();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = NewEmail(), password = "khong-quan-trong" }, options: JsonDefaults.Options)
        };
        request.Headers.Add("Origin", "https://evil-subdomain.antfarms.xyz");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be((HttpStatusCode)403);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("ORIGIN_NOT_ALLOWED");
    }

    [DbFact]
    public async Task Post_ThieuOriginVaReferer_TraVe403()
    {
        var client = CreateClientWithoutDefaultOrigin();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = NewEmail(), password = "khong-quan-trong" }, JsonDefaults.Options);

        response.StatusCode.Should().Be((HttpStatusCode)403);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("ORIGIN_NOT_ALLOWED");
    }

    [DbFact]
    public async Task Post_VoiOriginHopLe_KhongBiChan()
    {
        var client = CreateClientWithoutDefaultOrigin();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = NewEmail(), password = "khong-quan-trong" }, options: JsonDefaults.Options)
        };
        request.Headers.Add("Origin", "http://localhost:3280");

        var response = await client.SendAsync(request);

        // Không có tài khoản này ⇒ 401 INVALID_CREDENTIALS — quan trọng là KHÔNG phải 403 ORIGIN_NOT_ALLOWED.
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task Preflight_TuOriginHopLe_ChoPhepCredentialsVaKhongCoWildcard()
    {
        var client = CreateClientWithoutDefaultOrigin();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "http://localhost:3280");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var allowCredentials).Should().BeTrue();
        allowCredentials!.Single().Should().Be("true");

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowOrigin).Should().BeTrue();
        allowOrigin!.Single().Should().Be("http://localhost:3280"); // KHÔNG BAO GIỜ "*" khi có credentials (trình duyệt sẽ tự chặn)
    }

    [DbFact]
    public async Task Preflight_TuOriginLa_KhongCoAccessControlAllowOrigin()
    {
        var client = CreateClientWithoutDefaultOrigin();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "https://khong-nam-trong-danh-sach.example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    private sealed record ErrorDto(string Error, string Code);
}
