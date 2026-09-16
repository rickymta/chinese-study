using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.System;

public class HealthAndInfoTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>
{
    [Fact]
    public async Task HealthLive_TraVe200()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SystemInfo_TraVe200VaTenServiceDung()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/system/info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SystemInfoDto>();
        body.Should().NotBeNull();
        body!.Service.Should().Be("identity-service");
    }

    [Fact]
    public async Task DuongDanKhongTonTai_TraVe404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/khong-ton-tai");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record SystemInfoDto(string Service, string Version, string Environment, DateTime ServerTimeUtc);
}
