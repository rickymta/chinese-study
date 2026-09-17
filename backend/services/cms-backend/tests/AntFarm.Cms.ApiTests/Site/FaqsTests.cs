using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Cms.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Cms.ApiTests.Site;

/// <summary>CRUD + order /api/admin/faqs + hiển thị public (§6.2, §5.2.3 W3b, test bắt buộc).</summary>
[Collection(CmsApiCollection.Name)]
public class FaqsTests(CmsDbApiFactory factory) : IClassFixture<CmsDbApiFactory>
{
    private HttpClient AdminClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(
            email: CmsDbApiFactory.BootstrapAdminEmail, audiences: ["af-cms"]));
        return client;
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    /// <summary>Provision một tài khoản vai trò support — KHÔNG có site.manage (RoleCatalog).</summary>
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

    private static object ValidBody(string question, string groupKey = "general", bool isPublished = false) => new
    {
        question,
        answerMarkdown = "Trả lời cho: " + question,
        groupKey,
        isPublished
    };

    private async Task<FaqDto> CreateAsync(HttpClient client, string? question = null, string groupKey = "general", bool isPublished = false)
    {
        question ??= $"Câu hỏi {Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/admin/faqs", ValidBody(question, groupKey, isPublished), JsonDefaults.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<FaqDto>(JsonDefaults.Options))!;
    }

    [DbFact]
    public async Task KhongCoSiteManage_TraVe403ChoMoiEndpoint()
    {
        var support = await SupportClientAsync();

        (await support.GetAsync("/api/admin/faqs")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await support.PostAsJsonAsync("/api/admin/faqs", ValidBody("x"), JsonDefaults.Options))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await support.PutAsJsonAsync("/api/admin/faqs/order", new { groupKey = "general", ids = Array.Empty<Guid>() }, JsonDefaults.Options))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DbFact]
    public async Task Tao_TraVe201VaSortOrderCuoiCungTrongNhom()
    {
        var client = AdminClient();
        var groupKey = $"nhom-{Guid.NewGuid():N}"[..12];
        var first = await CreateAsync(client, groupKey: groupKey);
        var second = await CreateAsync(client, groupKey: groupKey);

        second.SortOrder.Should().Be(first.SortOrder + 1);
        second.Version.Should().NotBeNullOrEmpty();
    }

    [DbFact]
    public async Task Put_VersionCu_TraVe409ConcurrencyConflict()
    {
        var client = AdminClient();
        var created = await CreateAsync(client);

        var firstUpdate = await client.PutAsJsonAsync($"/api/admin/faqs/{created.Id}", new
        {
            question = created.Question, answerMarkdown = created.AnswerMarkdown, groupKey = created.GroupKey,
            isPublished = true, version = created.Version
        }, JsonDefaults.Options);
        firstUpdate.StatusCode.Should().Be(HttpStatusCode.OK);

        // Gửi lại version CŨ (đã lệch xmin thật sau lần sửa ở trên) — R-CA3.
        var staleUpdate = await client.PutAsJsonAsync($"/api/admin/faqs/{created.Id}", new
        {
            question = created.Question, answerMarkdown = created.AnswerMarkdown, groupKey = created.GroupKey,
            isPublished = true, version = created.Version
        }, JsonDefaults.Options);

        staleUpdate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await staleUpdate.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("CONCURRENCY_CONFLICT");
    }

    [DbFact]
    public async Task Order_ThieuId_TraVe422OrderMismatch()
    {
        var client = AdminClient();
        var groupKey = $"nhom-{Guid.NewGuid():N}"[..12];
        var a = await CreateAsync(client, groupKey: groupKey);
        await CreateAsync(client, groupKey: groupKey);

        var response = await client.PutAsJsonAsync("/api/admin/faqs/order", new { groupKey, ids = new[] { a.Id } }, JsonDefaults.Options);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("ORDER_MISMATCH");
    }

    [DbFact]
    public async Task Order_DungThuTu_TraVe204VaGetTheoThuTuMoi()
    {
        var client = AdminClient();
        var groupKey = $"nhom-{Guid.NewGuid():N}"[..12];
        var a = await CreateAsync(client, groupKey: groupKey);
        var b = await CreateAsync(client, groupKey: groupKey);

        var reorderResponse = await client.PutAsJsonAsync("/api/admin/faqs/order",
            new { groupKey, ids = new[] { b.Id, a.Id } }, JsonDefaults.Options);
        reorderResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterB = await (await client.GetAsync($"/api/admin/faqs/{b.Id}")).Content.ReadFromJsonAsync<FaqDto>(JsonDefaults.Options);
        var afterA = await (await client.GetAsync($"/api/admin/faqs/{a.Id}")).Content.ReadFromJsonAsync<FaqDto>(JsonDefaults.Options);
        afterB!.SortOrder.Should().BeLessThan(afterA!.SortOrder);
    }

    [DbFact]
    public async Task Xoa_TraVe204_XoaLai_TraVe404()
    {
        var client = AdminClient();
        var created = await CreateAsync(client);

        var deleteResponse = await client.DeleteAsync($"/api/admin/faqs/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deleteAgainResponse = await client.DeleteAsync($"/api/admin/faqs/{created.Id}");
        deleteAgainResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [DbFact]
    public async Task MoiThaoTacGhi_SinhMotDongNhatKyDungActor()
    {
        var client = AdminClient();
        var created = await CreateAsync(client);

        await using var db = TestDbContextFactory.Create();
        var log = await db.AuditLogs
            .Where(l => l.Action == "faq.create" && l.TargetId == created.Id.ToString())
            .OrderByDescending(l => l.At)
            .FirstOrDefaultAsync();

        log.Should().NotBeNull();
        log!.ActorEmail.Should().Be(CmsDbApiFactory.BootstrapAdminEmail);
        log.TargetType.Should().Be("faq");
    }

    [DbFact]
    public async Task Public_ChiTraVeCauHoiDaXuatBan()
    {
        var client = AdminClient();
        var groupKey = $"nhom-{Guid.NewGuid():N}"[..12];
        var published = await CreateAsync(client, question: $"Da xuat ban {Guid.NewGuid():N}", groupKey: groupKey, isPublished: true);
        var draft = await CreateAsync(client, question: $"Chua xuat ban {Guid.NewGuid():N}", groupKey: groupKey, isPublished: false);

        var response = await factory.CreateClient().GetAsync("/api/public/site");
        var body = await response.Content.ReadFromJsonAsync<PublicSiteDto>(JsonDefaults.Options);

        body!.Faqs.Select(f => f.Id).Should().Contain(published.Id);
        body.Faqs.Select(f => f.Id).Should().NotContain(draft.Id);
    }

    private sealed record FaqDto(
        Guid Id, string Question, string AnswerMarkdown, string GroupKey, int SortOrder,
        bool IsPublished, string Version, DateTime UpdatedAt);

    private sealed record PublicFaqDto(Guid Id, string Question, string AnswerMarkdown, string GroupKey);
    private sealed record PublicSiteDto(
        Dictionary<string, string> Settings, string? OgImageUrl, List<object> Languages, List<PublicFaqDto> Faqs);

    private sealed record ErrorDto(string Error, string Code);
}
