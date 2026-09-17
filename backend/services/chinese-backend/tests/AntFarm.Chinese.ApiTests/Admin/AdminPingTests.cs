using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Admin;

/// <summary>GET /api/admin/ping (§6.3, §7 F3) — canh gác quyền "users.manage" (RequirePermissionAttribute).</summary>
[Collection(ChineseApiCollection.Name)]
public class AdminPingTests(ChineseDbApiFactory factory) : IClassFixture<ChineseDbApiFactory>
{
    [DbFact]
    public async Task HocVien_GoiPing_TraVe403Json()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.TokenFactory.CreateToken(email: $"hoc-vien-{Guid.NewGuid():N}@vidu.com"));

        var response = await client.GetAsync("/api/admin/ping");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("FORBIDDEN");
    }

    [DbFact]
    public async Task Admin_GoiPing_TraVe200()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.TokenFactory.CreateToken(email: ChineseDbApiFactory.BootstrapAdminEmail));

        var response = await client.GetAsync("/api/admin/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PingDto>(JsonDefaults.Options);
        body!.Ok.Should().BeTrue();
    }

    [DbFact]
    public async Task KhongCoToken_TraVe401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/ping");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record ErrorDto(string Error, string Code);

    private sealed record PingDto(bool Ok);
}
