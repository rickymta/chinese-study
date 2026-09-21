using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Cms.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Cms.ApiTests.Site;

/// <summary>CRUD + order /api/admin/languages (§6.2, §5.2.3 W3a, test bắt buộc #4).</summary>
[Collection(CmsApiCollection.Name)]
public class LanguagesTests(CmsDbApiFactory factory) : IClassFixture<CmsDbApiFactory>
{
    private HttpClient AdminClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(
            email: CmsDbApiFactory.BootstrapAdminEmail, audiences: ["af-cms"]));
        return client;
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private static object ValidBody(string code, string status = "coming_soon", string? appUrl = null) => new
    {
        code,
        name = $"Ngôn ngữ {code}",
        nativeName = code,
        tagline = "",
        descriptionMarkdown = "",
        status,
        appUrl,
        accentColor = (string?)null
    };

    private async Task<LanguageDto> CreateAsync(HttpClient client, string? code = null, string status = "coming_soon", string? appUrl = null)
    {
        code ??= $"lang-{Guid.NewGuid():N}"[..12];
        var response = await client.PostAsJsonAsync("/api/admin/languages", ValidBody(code, status, appUrl), JsonDefaults.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LanguageDto>(JsonDefaults.Options))!;
    }

    [DbFact]
    public async Task Tao_TraVe201VaSortOrderCuoiCung()
    {
        var client = AdminClient();
        var before = await (await client.GetAsync("/api/admin/languages")).Content.ReadFromJsonAsync<List<LanguageDto>>(JsonDefaults.Options);
        var maxSortOrder = before!.Count > 0 ? before.Max(l => l.SortOrder) : 0;

        var created = await CreateAsync(client, appUrl: "https://vidu.antfarms.xyz");

        created.SortOrder.Should().Be(maxSortOrder + 1);
        created.Version.Should().NotBeNullOrEmpty();
    }

    [DbFact]
    public async Task TrungCode_TraVe409CodeTaken()
    {
        var client = AdminClient();
        var code = $"trung-{Guid.NewGuid():N}"[..12];
        await CreateAsync(client, code);

        var response = await client.PostAsJsonAsync("/api/admin/languages", ValidBody(code), JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("CODE_TAKEN");
    }

    [DbFact]
    public async Task StatusOpen_ThieuAppUrl_TraVe422AppUrlRequired()
    {
        var client = AdminClient();
        var code = $"open-{Guid.NewGuid():N}"[..12];

        var response = await client.PostAsJsonAsync("/api/admin/languages", ValidBody(code, "open", null), JsonDefaults.Options);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("APP_URL_REQUIRED");
    }

    [DbFact]
    public async Task Put_VersionCu_TraVe409ConcurrencyConflict()
    {
        var client = AdminClient();
        var created = await CreateAsync(client);

        var firstUpdate = await client.PutAsJsonAsync($"/api/admin/languages/{created.Id}", new
        {
            name = created.Name, nativeName = created.NativeName, tagline = "", descriptionMarkdown = "",
            status = "coming_soon", appUrl = (string?)null, accentColor = (string?)null, version = created.Version
        }, JsonDefaults.Options);
        firstUpdate.StatusCode.Should().Be(HttpStatusCode.OK);

        // Gửi lại version CŨ (đã lệch xmin thật sau lần sửa ở trên) — R-CA3.
        var staleUpdate = await client.PutAsJsonAsync($"/api/admin/languages/{created.Id}", new
        {
            name = created.Name, nativeName = created.NativeName, tagline = "", descriptionMarkdown = "",
            status = "coming_soon", appUrl = (string?)null, accentColor = (string?)null, version = created.Version
        }, JsonDefaults.Options);

        staleUpdate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await staleUpdate.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("CONCURRENCY_CONFLICT");
    }

    [DbFact]
    public async Task Order_ThieuId_TraVe422OrderMismatch()
    {
        var client = AdminClient();
        await CreateAsync(client);
        await CreateAsync(client);
        var all = await (await client.GetAsync("/api/admin/languages")).Content.ReadFromJsonAsync<List<LanguageDto>>(JsonDefaults.Options);
        var ids = all!.Select(l => l.Id).Skip(1).ToList(); // cố tình thiếu 1 id

        var response = await client.PutAsJsonAsync("/api/admin/languages/order", new { ids }, JsonDefaults.Options);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("ORDER_MISMATCH");
    }

    [DbFact]
    public async Task Order_DungThuTu_TraVe204VaGetTheoThuTuMoi()
    {
        var client = AdminClient();
        var a = await CreateAsync(client);
        var b = await CreateAsync(client);
        var all = await (await client.GetAsync("/api/admin/languages")).Content.ReadFromJsonAsync<List<LanguageDto>>(JsonDefaults.Options);
        var ids = all!.Select(l => l.Id).ToList();

        // Đảo NGƯỢC toàn bộ thứ tự hiện có — kiểm tra GET sau đó phản ánh đúng thứ tự mới.
        ids.Reverse();
        var reorderResponse = await client.PutAsJsonAsync("/api/admin/languages/order", new { ids }, JsonDefaults.Options);
        reorderResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await (await client.GetAsync("/api/admin/languages")).Content.ReadFromJsonAsync<List<LanguageDto>>(JsonDefaults.Options);
        after!.Select(l => l.Id).Should().BeEquivalentTo(ids, o => o.WithStrictOrdering());

        _ = a; _ = b; // giữ biến để rõ ý — không dùng trực tiếp giá trị, chỉ cần tồn tại trong DB
    }

    [DbFact]
    public async Task Xoa_TraVe204_XoaLai_TraVe404()
    {
        var client = AdminClient();
        var created = await CreateAsync(client);

        var deleteResponse = await client.DeleteAsync($"/api/admin/languages/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deleteAgainResponse = await client.DeleteAsync($"/api/admin/languages/{created.Id}");
        deleteAgainResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record LanguageDto(
        Guid Id, string Code, string Name, string NativeName, string Tagline, string DescriptionMarkdown,
        string Status, string? AppUrl, string? AccentColor, int SortOrder, string Version, DateTime UpdatedAt);

    private sealed record ErrorDto(string Error, string Code);
}
