using System.Net;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Internal;

/// <summary>§5.2.9, §6.5 — hình dạng phản hồi + validation 400 (ranh giới ngày 23:30/00:30 VN đã có unit test riêng ở <c>RegistrationStatsCalculatorTests</c>, không cần DB).</summary>
[Collection(IdentityApiCollection.Name)]
public class RegistrationStatsTests(IdentityInternalApiFactory factory) : IClassFixture<IdentityInternalApiFactory>
{
    private HttpClient CreateClient() => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
        HandleCookies = false,
        AllowAutoRedirect = false
    });

    private HttpRequestMessage InternalGet(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Test-Local-Port", IdentityInternalApiFactory.InternalPort.ToString());
        request.Headers.Add("X-Service-Key", IdentityInternalApiFactory.ServiceKey);
        return request;
    }

    [DbFact]
    public async Task KhongTruyenThamSo_TraVe200Voi30Ngay()
    {
        var client = CreateClient();

        var response = await client.SendAsync(InternalGet("/internal/stats/registrations"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StatsDto>(JsonDefaults.Options);
        body!.TimeZone.Should().Be("Asia/Ho_Chi_Minh");
        body.Days.Should().HaveCount(30);
    }

    [DbFact]
    public async Task FromLonHonTo_TraVe400()
    {
        var client = CreateClient();

        var response = await client.SendAsync(InternalGet("/internal/stats/registrations?from=2026-09-17&to=2026-09-01"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("VALIDATION");
    }

    [DbFact]
    public async Task KhoangNgayVuotQua366_TraVe400()
    {
        var client = CreateClient();

        var response = await client.SendAsync(InternalGet("/internal/stats/registrations?from=2025-01-01&to=2026-09-17"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record StatsDto(string TimeZone, List<object> Days, int TotalAccounts, int DisabledAccounts, int ActiveLast7Days, int ActiveLast30Days);

    private sealed record ErrorDto(string Error, string Code);
}
