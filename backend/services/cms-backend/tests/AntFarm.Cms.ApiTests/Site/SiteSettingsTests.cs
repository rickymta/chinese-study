using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Cms.ApiTests.Infrastructure;
using AntFarm.Cms.Domain.Site;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Cms.ApiTests.Site;

/// <summary>GET/PUT /api/admin/site-settings (§6.2, §5.2.3 W3a, test bắt buộc #2/#3).</summary>
[Collection(CmsApiCollection.Name)]
public class SiteSettingsTests(CmsDbApiFactory factory) : IClassFixture<CmsDbApiFactory>
{
    private HttpClient AdminClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(
            email: CmsDbApiFactory.BootstrapAdminEmail, audiences: ["af-cms"]));
        return client;
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    /// <summary>Provision một tài khoản 0 quyền (vai trò support không có site.manage — RoleCatalog).</summary>
    private async Task<HttpClient> SupportClientAsync()
    {
        var accountId = Guid.NewGuid();
        var email = $"support-{Guid.NewGuid():N}@vidu.com";
        var admin = AdminClient();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(accountId, email: email, audiences: ["af-cms"]));
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        var setRoles = await admin.PutAsJsonAsync($"/api/admin/users/{accountId}/roles", new { roles = new[] { "support" } }, JsonDefaults.Options);
        setRoles.StatusCode.Should().Be(HttpStatusCode.OK);

        return client;
    }

    private static Dictionary<string, string> AllKeysDefault() =>
        SiteSettingKeys.All.ToDictionary(d => d.Key, d => d.DefaultValue);

    [DbFact]
    public async Task KhongToken_TraVe401()
    {
        var client = factory.CreateClient();
        (await client.GetAsync("/api/admin/site-settings")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task VaiTroSupport_KhongCoSiteManage_TraVe403ChoMoiEndpoint()
    {
        var support = await SupportClientAsync();

        (await support.GetAsync("/api/admin/site-settings")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await support.PutAsJsonAsync("/api/admin/site-settings", new { values = AllKeysDefault() }, JsonDefaults.Options))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await support.GetAsync("/api/admin/languages")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DbFact]
    public async Task Get_SauSeed_TraVeDuMuoiMotKhoa()
    {
        var response = await AdminClient().GetAsync("/api/admin/site-settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SettingsDto>(JsonDefaults.Options);
        body!.Values.Should().HaveCount(11);
        body.Values.Keys.Should().BeEquivalentTo(SiteSettingKeys.All.Select(d => d.Key));
    }

    [DbFact]
    public async Task Put_ThieuKhoa_TraVe400()
    {
        var values = AllKeysDefault();
        values.Remove(SiteSettingKeys.SiteName);

        var response = await AdminClient().PutAsJsonAsync("/api/admin/site-settings", new { values }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("VALIDATION");
    }

    [DbFact]
    public async Task Put_KhoaLa_TraVe400()
    {
        var values = AllKeysDefault();
        values["khoa.la"] = "gi-do";

        var response = await AdminClient().PutAsJsonAsync("/api/admin/site-settings", new { values }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [DbFact]
    public async Task Put_GiaTriQuaDai_TraVe400()
    {
        var values = AllKeysDefault();
        values[SiteSettingKeys.SeoDefaultTitle] = new string('a', 71);

        var response = await AdminClient().PutAsJsonAsync("/api/admin/site-settings", new { values }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [DbFact]
    public async Task Put_HopLe_TraVe200VaDungMotDongAuditLog()
    {
        var client = AdminClient();
        var values = AllKeysDefault();
        var uniqueName = $"AntFarm {Guid.NewGuid():N}";
        values[SiteSettingKeys.SiteName] = uniqueName;

        var response = await client.PutAsJsonAsync("/api/admin/site-settings", new { values }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SettingsDto>(JsonDefaults.Options);
        body!.Values[SiteSettingKeys.SiteName].Should().Be(uniqueName);

        await using var db = TestDbContextFactory.Create();
        var logs = await db.AuditLogs
            .Where(a => a.Action == "site_settings.update" && a.ActorEmail == CmsDbApiFactory.BootstrapAdminEmail)
            .OrderByDescending(a => a.At)
            .Take(1)
            .ToListAsync();

        logs.Should().ContainSingle();
        logs[0].Summary.Should().Contain("site.name");
        logs[0].Summary.Should().NotContain(uniqueName); // KHÔNG chép giá trị dài/nhạy vào summary (§5.2.3)
    }

    private sealed record SettingsDto(Dictionary<string, string> Values, DateTime UpdatedAt);
    private sealed record ErrorDto(string Error, string Code);
}
