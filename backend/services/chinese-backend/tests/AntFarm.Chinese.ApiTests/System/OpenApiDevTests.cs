using System.Net;
using AntFarm.Chinese.ApiTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.System;

/// <summary>Review F3 17/09/2026 — FallbackPolicy không được chặn tài liệu API ở Development.</summary>
[Collection(ChineseApiCollection.Name)]
public class OpenApiDevTests(ChineseDevEnvironmentApiFactory factory) : IClassFixture<ChineseDevEnvironmentApiFactory>
{
    [Fact]
    public async Task OpenApiJson_MoiTruongDevelopment_KhongCanDangNhapVanTraVe200()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ScalarUi_MoiTruongDevelopment_KhongCanDangNhapVanTraVe200()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/scalar/v1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
