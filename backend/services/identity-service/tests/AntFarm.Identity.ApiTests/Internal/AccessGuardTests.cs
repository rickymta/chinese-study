using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Internal;

/// <summary>R-W4 (§5.2.9) ở tầng HTTP thật — bổ sung cho <c>InternalAccessPolicyTests</c> (unit, hàm thuần).</summary>
[Collection(IdentityApiCollection.Name)]
public class AccessGuardTests(IdentityInternalApiFactory factory) : IClassFixture<IdentityInternalApiFactory>
{
    private HttpClient CreateClient() => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
        HandleCookies = false,
        AllowAutoRedirect = false
    });

    [DbFact]
    public async Task Ping_QuaCongCongKhai_KeCaKhoaDung_TraVe404()
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/internal/ping");
        request.Headers.Add("X-Test-Local-Port", IdentityInternalApiFactory.PublicPort.ToString());
        request.Headers.Add("X-Service-Key", IdentityInternalApiFactory.ServiceKey);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [DbFact]
    public async Task Ping_QuaCongNoiBo_ThieuKhoa_TraVe401()
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/internal/ping");
        request.Headers.Add("X-Test-Local-Port", IdentityInternalApiFactory.InternalPort.ToString());

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>();
        body!.Code.Should().Be("SERVICE_KEY_INVALID");
    }

    [DbFact]
    public async Task Ping_QuaCongNoiBo_KhoaSai_TraVe401()
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/internal/ping");
        request.Headers.Add("X-Test-Local-Port", IdentityInternalApiFactory.InternalPort.ToString());
        request.Headers.Add("X-Service-Key", "khoa-sai-nhung-cung-du-dai-32-ky-tu-tro-len");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task Ping_QuaCongNoiBo_KhoaDung_TraVe200()
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/internal/ping");
        request.Headers.Add("X-Test-Local-Port", IdentityInternalApiFactory.InternalPort.ToString());
        request.Headers.Add("X-Service-Key", IdentityInternalApiFactory.ServiceKey);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OkDto>();
        body!.Ok.Should().BeTrue();
    }

    [DbFact]
    public async Task RouteCongKhaiBatKy_QuaCongNoiBo_TraVe404()
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/.well-known/jwks.json");
        request.Headers.Add("X-Test-Local-Port", IdentityInternalApiFactory.InternalPort.ToString());
        request.Headers.Add("X-Service-Key", IdentityInternalApiFactory.ServiceKey);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [DbFact]
    public async Task Post_ThieuActorHeaders_TraVe400()
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/internal/accounts/{Guid.NewGuid()}/enable");
        request.Headers.Add("X-Test-Local-Port", IdentityInternalApiFactory.InternalPort.ToString());
        request.Headers.Add("X-Service-Key", IdentityInternalApiFactory.ServiceKey);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>();
        body!.Code.Should().Be("VALIDATION");
    }

    private sealed record ErrorDto(string Error, string Code);

    private sealed record OkDto(bool Ok);
}
