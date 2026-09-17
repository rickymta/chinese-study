using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>R-A9 — 20 req/phút/IP, review F2: PHẢI partition theo IP (không phải ngân sách dùng chung); dùng permit RẤT THẤP (3) để test không phải gọi hàng chục request thật.</summary>
[Collection(IdentityApiCollection.Name)]
public class RateLimitTests(IdentityDbApiFactoryLowRateLimit factory) : IClassFixture<IdentityDbApiFactoryLowRateLimit>
{
    private HttpClient CreateClient()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        return client;
    }

    [DbFact]
    public async Task QuaSoLuongChoPhepTrongMotPhut_TraVe429()
    {
        var client = CreateClient();
        var email = $"ratelimit-{Guid.NewGuid():N}@vidu.com";

        HttpResponseMessage? last = null;
        for (var i = 0; i < 3; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "khong-quan-trong" }, JsonDefaults.Options);
            last.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // vẫn được XỬ LÝ (chỉ là email không tồn tại), chưa chạm rate limit
        }

        var rejected = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "khong-quan-trong" }, JsonDefaults.Options);

        rejected.StatusCode.Should().Be((HttpStatusCode)429);
        var error = await rejected.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("RATE_LIMITED");
    }

    private sealed record ErrorDto(string Error, string Code);
}
