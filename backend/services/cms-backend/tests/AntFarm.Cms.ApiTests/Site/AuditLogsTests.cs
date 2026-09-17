using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Cms.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Cms.ApiTests.Site;

/// <summary>GET /api/admin/audit-logs (§6.2, §5.2.3 W3b) — quyền users.manage.</summary>
[Collection(CmsApiCollection.Name)]
public class AuditLogsTests(CmsDbApiFactory factory) : IClassFixture<CmsDbApiFactory>
{
    private HttpClient AdminClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(
            email: CmsDbApiFactory.BootstrapAdminEmail, audiences: ["af-cms"]));
        return client;
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    /// <summary>Provision một tài khoản vai trò editor — có site.manage nhưng KHÔNG có users.manage (RoleCatalog).</summary>
    private async Task<HttpClient> EditorClientAsync()
    {
        var accountId = Guid.NewGuid();
        var email = $"editor-{Guid.NewGuid():N}@vidu.com";
        var admin = AdminClient();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(accountId, email: email, audiences: ["af-cms"]));
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        var setRoles = await admin.PutAsJsonAsync($"/api/admin/users/{accountId}/roles", new { roles = new[] { "editor" } }, JsonDefaults.Options);
        setRoles.StatusCode.Should().Be(HttpStatusCode.OK);

        return client;
    }

    [DbFact]
    public async Task VaiTroEditor_KhongCoUsersManage_TraVe403()
    {
        var editor = await EditorClientAsync();

        (await editor.GetAsync("/api/admin/audit-logs")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DbFact]
    public async Task VaiTroAdmin_TraVe200CoPhanTrang()
    {
        var client = AdminClient();
        // Sinh ít nhất 1 dòng nhật ký bằng một thao tác ghi thật (tạo ngôn ngữ).
        var code = $"log-{Guid.NewGuid():N}"[..12];
        await client.PostAsJsonAsync("/api/admin/languages", new
        {
            code, name = "x", nativeName = "x", tagline = "", descriptionMarkdown = "",
            status = "coming_soon", appUrl = (string?)null, accentColor = (string?)null
        }, JsonDefaults.Options);

        var response = await client.GetAsync("/api/admin/audit-logs?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PageDto>(JsonDefaults.Options);
        body!.Page.Should().Be(1);
        body.PageSize.Should().Be(5);
        body.Items.Should().NotBeEmpty();
        body.TotalCount.Should().BeGreaterThanOrEqualTo(body.Items.Count);
    }

    [DbFact]
    public async Task LocTargetTypeFaq_ChiTraVeDongFaq()
    {
        var client = AdminClient();
        var faqResponse = await client.PostAsJsonAsync("/api/admin/faqs", new
        {
            question = $"Loc theo faq {Guid.NewGuid():N}", answerMarkdown = "trả lời", groupKey = "general", isPublished = false
        }, JsonDefaults.Options);
        faqResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.GetAsync("/api/admin/audit-logs?targetType=faq&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PageDto>(JsonDefaults.Options);
        body!.Items.Should().NotBeEmpty();
        body.Items.Should().OnlyContain(i => i.TargetType == "faq");
    }

    private sealed record LogItemDto(
        Guid Id, DateTime At, Guid ActorId, string ActorEmail, string Action,
        string TargetType, string? TargetId, string Summary, bool Success);

    private sealed record PageDto(List<LogItemDto> Items, int Page, int PageSize, int TotalCount);
}
