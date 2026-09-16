using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Dictionary;

/// <summary>
/// GET /api/dictionary/search (§5.2.3, §6.1) — dữ liệu HSK1 THẬT (500 từ, đã nạp bởi
/// <c>ContentImportRunner</c> lúc factory khởi động). LƯU Ý: "你好" KHÔNG thuộc danh sách chính
/// thức 500 từ HSK 3.0 cấp 1 (R6-1 — kiểm tra bằng dữ liệu thật ngày 17/09/2026), khác với ví dụ
/// minh hoạ ở §5.2.3 hợp đồng F6/F7 (ĐIỂM LỆCH đã báo lại ở bàn giao F6.2) — dùng "妈妈" (ma1 ma5,
/// CÓ trong kho) thay thế để kiểm chứng tách âm tiết liền pinyin dấu/số nhiều âm tiết.
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class DictionarySearchTests : IClassFixture<ChineseDbApiFactory>
{
    private readonly ChineseDbApiFactory _factory;

    public DictionarySearchTests(ChineseDbApiFactory factory)
    {
        _factory = factory;
        Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());
    }

    [DbTheory]
    [InlineData("爱")]
    [InlineData("ai4")]
    [InlineData("ài")]
    [InlineData("ai")]
    [InlineData("yêu")]
    [InlineData("yeu")]
    [InlineData("ái")]
    public async Task TimAi_MoiCachHieu_RaChu爱(string q)
    {
        var result = await SearchAsync(q);
        result!.Items.Should().Contain(i => i.Simplified == "爱");
    }

    [DbFact]
    public async Task TimAi4_ChuAiOViTriDau()
    {
        var result = await SearchAsync("ai4");
        result!.Items.Should().NotBeEmpty();
        result.Items[0].Simplified.Should().Be("爱");
        result.Items[0].MatchKind.Should().Be("pinyin");
    }

    [DbFact]
    public async Task TimYeuCoDau_KhongLanSangYeu()
    {
        var result = await SearchAsync("yêu");
        // "yêu" (còn dấu) không được ra bất kỳ mục nào MÀ nghĩa CHỈ khớp "yếu" (khác dấu) — 爱 (yêu; thích) phải có mặt.
        result!.Items.Should().Contain(i => i.Simplified == "爱");
        result.Items.Should().NotContain(i => i.MeaningsVi.Any(m => m.Contains("yếu", StringComparison.Ordinal) && !m.Contains("yêu", StringComparison.Ordinal)));
    }

    [DbTheory]
    [InlineData("ma1ma5")]
    [InlineData("ma1 ma5")]
    [InlineData("māma")]
    [InlineData("mama")]
    public async Task TimMaMa_MoiCachHieu_RaTu妈妈(string q)
    {
        var result = await SearchAsync(q);
        result!.Items.Should().Contain(i => i.Simplified == "妈妈");
    }

    [DbTheory]
    [InlineData("ma1ma5")]
    [InlineData("māma")]
    public async Task TimMaMaLien_KhopChinhXacPinyinCompact_DungDauTien(string q)
    {
        // "ma1ma5"/"māma" khớp ĐÚNG pinyin_compact của 妈妈 (rank 1, tách âm tiết liền đúng "ma"+"ma")
        // — phải đứng vị trí 0, không lẫn với các từ khác chỉ khớp tiền tố (rank 4).
        var result = await SearchAsync(q);
        result!.Items.Should().NotBeEmpty();
        result.Items[0].Simplified.Should().Be("妈妈");
        result.Items[0].MatchKind.Should().Be("pinyin");
    }

    [DbFact]
    public async Task TimBa_QuaVariants_RaTu爸爸()
    {
        var result = await SearchAsync("爸");
        result!.Items.Should().Contain(i => i.Simplified == "爸爸");
    }

    [DbTheory]
    [InlineData("Beijing")]
    [InlineData("beijing")]
    public async Task TimBeijing_KhongPhanBietHoaThuong_RaTu北京(string q)
    {
        var result = await SearchAsync(q);
        result!.Items.Should().Contain(i => i.Simplified == "北京");
    }

    [DbFact]
    public async Task TimKyTuDacBiet_KhongLoi200()
    {
        var response = await LearnerClient().GetAsync("/api/dictionary/search?q=" + HttpUtility.UrlEncode("100%"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DbFact]
    public async Task PhanTrang_PageSize5_TotalCountDung()
    {
        // Lọc hsk=1 để CHẮC CHẮN totalCount=500 dù DB test dùng chung có thể còn từ giả của
        // ContentImporterTests (hsk3Level=null, không rơi vào bộ lọc này) — content.words KHÔNG
        // scope theo dataset, các lớp test trong CÙNG collection chia sẻ một af_chinese_test.
        var result = await SearchAsync(null, hsk: 1, pageSize: 5);
        result!.Items.Should().HaveCount(5);
        result.PageSize.Should().Be(5);
        result.TotalCount.Should().Be(500); // rỗng ⇒ liệt kê toàn bộ 500 từ HSK1 (R6-21)
    }

    [DbFact]
    public async Task QQua64KyTu_Tra400Validation()
    {
        var longQuery = new string('a', 65);
        var response = await LearnerClient().GetAsync("/api/dictionary/search?q=" + longQuery);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("VALIDATION");
    }

    [DbFact]
    public async Task Learner_CoQuyen_Tra200() =>
        (await LearnerClient().GetAsync("/api/dictionary/search?q=爱")).StatusCode.Should().Be(HttpStatusCode.OK);

    [DbFact]
    public async Task NguoiDungBiGoHetVaiTro_Tra403()
    {
        var accountId = Guid.NewGuid();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(_factory.TokenFactory.CreateToken(accountId));
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var db = TestDbContextFactory.Create())
        {
            var roles = await db.UserRoles.Where(ur => ur.UserId == accountId).ToListAsync();
            db.UserRoles.RemoveRange(roles);
            await db.SaveChangesAsync();
        }

        (await client.GetAsync("/api/dictionary/search?q=爱")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DbFact]
    public async Task KhongToken_Tra401() =>
        (await _factory.CreateClient().GetAsync("/api/dictionary/search?q=爱")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    private async Task<SearchResultDto?> SearchAsync(string? q, short? hsk = null, int page = 1, int pageSize = 20)
    {
        var query = $"?page={page}&pageSize={pageSize}" + (q is null ? "" : $"&q={HttpUtility.UrlEncode(q)}") + (hsk is null ? "" : $"&hsk={hsk}");
        var response = await LearnerClient().GetAsync("/api/dictionary/search" + query);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<SearchResultDto>(JsonDefaults.Options);
    }

    private HttpClient LearnerClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(_factory.TokenFactory.CreateToken(Guid.NewGuid()));
        return client;
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private static string ResolveRealContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "content", "chinese");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return "content/chinese";
    }

    private sealed record SearchItemDto(Guid Id, string Simplified, string? Traditional, string Pinyin, short? Hsk3Level, short? Hsk2Level, string? HanViet, List<string> MeaningsVi, string MeaningViStatus, string MatchKind);

    private sealed record SearchResultDto(List<SearchItemDto> Items, int Page, int PageSize, int TotalCount);

    private sealed record ErrorDto(string Error, string Code);
}
