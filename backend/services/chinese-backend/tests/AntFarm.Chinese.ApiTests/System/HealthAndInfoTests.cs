using System.Net;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.System;

[Collection(ChineseApiCollection.Name)]
public class HealthAndInfoTests(ChineseApiFactory factory) : IClassFixture<ChineseApiFactory>
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
        body!.Service.Should().Be("chinese-backend");
    }

    /// <summary>
    /// F3: FallbackPolicy = RequireAuthenticatedUser (§5.2.3) áp dụng cho MỌI request KHÔNG khớp
    /// endpoint nào (không chỉ endpoint có [Authorize]) — hành vi bảo mật-mặc-định của ASP.NET
    /// Core từ khi có FallbackPolicy: chưa đăng nhập ⇒ 401 UNAUTHENTICATED thay vì lộ 404 "đường
    /// dẫn không tồn tại". Trước F3 (chưa có FallbackPolicy) test này kỳ vọng 404 — đổi có chủ đích.
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
