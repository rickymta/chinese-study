using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>
/// M1 §5.2.3 #10 — RM-A9: policy <c>auth-mobile</c> có ngân sách RIÊNG, tách khỏi policy <c>auth</c>
/// của web. CHỈ MỘT test method: <see cref="IClassFixture{T}"/> dùng CHUNG một factory/TestServer
/// cho mọi test trong lớp, nên nếu tách hai assertion thành hai method riêng thì thứ tự chạy
/// (không đảm bảo bởi xUnit) có thể khiến method này "ăn" hết ngân sách 3/phút của method kia
/// trước khi nó kịp chạy — gộp lại để luôn kiểm soát được đúng thứ tự.
/// </summary>
[Collection(IdentityApiCollection.Name)]
public class MobileRateLimitTests(IdentityDbApiFactoryLowMobileRateLimit factory) : IClassFixture<IdentityDbApiFactoryLowMobileRateLimit>
{
    private HttpClient CreateClient() => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });

    private static HttpRequestMessage MobileLoginRequest(string email)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mobile/login")
        {
            Content = JsonContent.Create(new { email, password = "khong-quan-trong" }, options: JsonDefaults.Options)
        };
        request.Headers.Add("X-AF-Client", "chinese-mobile/1.0.0+1 (android)");
        return request;
    }

    [DbFact]
    public async Task QuaSoLuongChoPhepTrongMotPhut_TraVe429_PolicyAuthCuaWebKhongBiAnhHuong()
    {
        var client = CreateClient();
        var email = $"mobileratelimit-{Guid.NewGuid():N}@vidu.com";

        for (var i = 0; i < 3; i++)
        {
            var response = await client.SendAsync(MobileLoginRequest(email));
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // được XỬ LÝ (email không tồn tại), chưa chạm rate limit
        }

        var rejected = await client.SendAsync(MobileLoginRequest(email));
        rejected.StatusCode.Should().Be((HttpStatusCode)429);
        var error = await rejected.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("RATE_LIMITED");

        // Ngân sách "auth-mobile" (3/phút) VỪA CẠN ở trên — policy "auth" (web, permit rất cao ở
        // factory) vẫn còn nguyên, chứng minh hai policy đếm ĐỘC LẬP (không phải một ngân sách chung).
        using var webRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = "khong-quan-trong" }, options: JsonDefaults.Options)
        };
        webRequest.Headers.Add("Origin", "http://localhost:3280");
        var webResponse = await client.SendAsync(webRequest);
        webResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record ErrorDto(string Error, string Code);
}
