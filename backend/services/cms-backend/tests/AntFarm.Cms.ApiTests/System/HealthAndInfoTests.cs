using System.Net;
using System.Net.Http.Json;
using AntFarm.Cms.ApiTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AntFarm.Cms.ApiTests.System;

[Collection(CmsApiCollection.Name)]
public class HealthAndInfoTests(CmsApiFactory factory) : IClassFixture<CmsApiFactory>
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
        body!.Service.Should().Be("cms-backend");
    }

    /// <summary>
    /// FallbackPolicy = RequireAuthenticatedUser áp dụng cho MỌI request KHÔNG khớp endpoint nào
    /// (không chỉ endpoint có [Authorize]) — chưa đăng nhập ⇒ 401 UNAUTHENTICATED thay vì lộ 404
    /// "đường dẫn không tồn tại" (chép khuôn chinese-backend F3).
    /// </summary>
    [Fact]
    public async Task DuongDanKhongTonTai_ChuaXacThuc_TraVe401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/khong-ton-tai");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record SystemInfoDto(string Service, string Version, string Environment, DateTime ServerTimeUtc);
}
