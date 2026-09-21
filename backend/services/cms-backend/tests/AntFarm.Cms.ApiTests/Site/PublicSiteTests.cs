using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Cms.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Cms.ApiTests.Site;

/// <summary>GET /api/public/site (§6.2, §5.2.3 W3a, test bắt buộc #5) — ẩn danh.</summary>
[Collection(CmsApiCollection.Name)]
public class PublicSiteTests(CmsDbApiFactory factory) : IClassFixture<CmsDbApiFactory>
{
    private HttpClient AdminClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenFactory.CreateToken(
            email: CmsDbApiFactory.BootstrapAdminEmail, audiences: ["af-cms"]));
        return client;
    }

    [DbFact]
    public async Task AnDanh_TraVe200KemCacheControlVaKhongThieu()
    {
        var client = factory.CreateClient(); // KHÔNG gắn Authorization — mô phỏng khách ẩn danh

        var response = await client.GetAsync("/api/public/site");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.ToString().Should().Be("public, max-age=60");

        var body = await response.Content.ReadFromJsonAsync<PublicSiteDto>(JsonDefaults.Options);
        body!.Faqs.Should().NotBeNull(); // W3b điền — nội dung cụ thể (chỉ isPublished=true) được kiểm ở FaqsTests
        body.Languages.Should().NotBeEmpty();
        body.Languages.Should().OnlyContain(l => l.Status != "hidden");
        body.Languages.Select(l => l.Code).Should().Contain("chinese"); // seed mặc định (§5.2.3)
    }

    [DbFact]
    public async Task AnDanh_KhoaRongBiBo_ConLaiSapDungThuTu()
    {
        var response = await factory.CreateClient().GetAsync("/api/public/site");
        var body = await response.Content.ReadFromJsonAsync<PublicSiteDto>(JsonDefaults.Options);

        // "" là mặc định nhiều khoá SEO/social chưa cấu hình — không xuất hiện trong settings public.
        body!.Settings.Values.Should().NotContain("");
        body.Languages.Select(l => l.Name).Should().NotBeEmpty();
    }

    [DbFact]
    public async Task NgonNguHidden_KhongXuatHienOPublic()
    {
        var admin = AdminClient();
        var code = $"hidden-{Guid.NewGuid():N}"[..12];
        var create = await admin.PostAsJsonAsync("/api/admin/languages", new
        {
            code, name = "Ẩn thử", nativeName = "Ẩn thử", tagline = "", descriptionMarkdown = "",
            status = "hidden", appUrl = (string?)null, accentColor = (string?)null
        }, JsonDefaults.Options);
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await factory.CreateClient().GetAsync("/api/public/site");
        var body = await response.Content.ReadFromJsonAsync<PublicSiteDto>(JsonDefaults.Options);

        body!.Languages.Select(l => l.Code).Should().NotContain(code);
    }

    private sealed record PublicLanguageDto(string Code, string Name, string NativeName, string Status);
    private sealed record PublicFaqDto(Guid Id, string Question, string AnswerMarkdown, string GroupKey);
    private sealed record PublicSiteDto(
        Dictionary<string, string> Settings, string? OgImageUrl,
        List<PublicLanguageDto> Languages, List<PublicFaqDto> Faqs);
}
