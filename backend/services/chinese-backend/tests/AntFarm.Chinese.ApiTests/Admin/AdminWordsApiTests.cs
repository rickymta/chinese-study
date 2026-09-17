using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Chinese.Infrastructure.Content;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Admin;

/// <summary>
/// §5.2.3/§6.3, test F10 mục 1, 8, 9, 10 — duyệt nghĩa từ vựng. Mỗi test dùng một từ GIẢ LẬP RIÊNG
/// nạp qua <c>ContentImporter</c> trực tiếp (khoá tự nhiên (simplified, pinyin) — chữ 鑫/淼 đã có ở
/// fixture <c>characters-protected.json</c>, âm tiết thứ ba NGẪU NHIÊN đảm bảo không đụng độ giữa
/// các test method/lần chạy) — KHÔNG đụng kho từ HSK1 thật (500 dòng) để không ảnh hưởng
/// <c>DictionarySearchTests</c>/<c>WordDetailTests</c>.
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class AdminWordsApiTests
{
    private static string TestDataPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "TestData", "content", fileName);

    [DbFact]
    public async Task HocVien_GoiPutWord_Tra403()
    {
        using var factory = new ChineseDbApiFactory();
        var learner = LearnerClient(factory);

        var (wordId, _) = await ImportWordAsync(hsk3Level: 3, meaningVi: "nghĩa ban đầu", hanViet: "hâm diểu");

        var response = await learner.PutAsJsonAsync($"/api/admin/words/{wordId}", new
        {
            version = 0u, meaningsVi = new[] { "x" }, meaningViStatus = "reviewed", hanViet = (string?)null, hanVietStatus = "derived"
        }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DbFact]
    public async Task SuaNghia_ThanhManual_NapLaiFileKhac_GiuBanAdmin_HskVanCapNhat()
    {
        using var factory = new ChineseDbApiFactory();
        var admin = await AdminClientAsync(factory);

        var (wordId, pinyin) = await ImportWordAsync(hsk3Level: 3, meaningVi: "nghĩa gốc trước khi admin sửa", hanViet: "hâm diểu");
        var before = (await (await admin.GetAsync($"/api/admin/words/{wordId}")).Content.ReadFromJsonAsync<AdminWordTestDto>(JsonDefaults.Options))!;
        before.MeaningViSource.Should().Be("machine");
        before.Hsk3Level.Should().Be(3);

        var updateResponse = await admin.PutAsJsonAsync($"/api/admin/words/{wordId}", new
        {
            version = before.Version,
            meaningsVi = new[] { "nghĩa do admin sửa tay" },
            meaningViStatus = "reviewed",
            hanViet = before.HanViet,
            hanVietStatus = "reviewed"
        }, JsonDefaults.Options);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<AdminWordTestDto>(JsonDefaults.Options))!;
        updated.MeaningViSource.Should().Be("manual");
        updated.EditedAt.Should().NotBeNull();

        // Nạp lại với file KHÁC CÙNG khoá tự nhiên (hsk3Level đổi 3 → 5, nghĩa cũng đổi) — R-CA10.
        var tempFile = await WriteWordFileAsync(pinyin, hsk3Level: 5, meaningVi: "nghĩa khác hoàn toàn từ file — KHÔNG được ghi đè", hanViet: "diểu hâm (đổi — KHÔNG được ghi đè)");
        try
        {
            await using var db = TestDbContextFactory.Create();
            var importer = new ContentImporter(db, TimeProvider.System, NullLogger<ContentImporter>.Instance);
            var result = await importer.ImportWordsAsync(tempFile, $"hsk-words-f10-{Guid.NewGuid():N}", CancellationToken.None);
            result.Protected.Should().Be(1);

            var afterReimport = await db.Words.AsNoTracking().FirstAsync(w => w.Id == wordId);
            afterReimport.MeaningsVi.Should().Equal("nghĩa do admin sửa tay"); // KHÔNG bị file đè (R-CA10)
            afterReimport.MeaningViSource.Should().Be("manual");
            afterReimport.Hsk3Level.Should().Be(5); // trường KHÔNG bị khoá vẫn cập nhật theo file (R-CA10)
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [DbFact]
    public async Task TimTuDienBangNghiaMoi_RaTuVuaSua_CacheDaLamMoi()
    {
        using var factory = new ChineseDbApiFactory();
        var admin = await AdminClientAsync(factory);
        var (wordId, _) = await ImportWordAsync(hsk3Level: 3, meaningVi: "nghĩa ban đầu", hanViet: "hâm diểu");

        var before = (await (await admin.GetAsync($"/api/admin/words/{wordId}")).Content.ReadFromJsonAsync<AdminWordTestDto>(JsonDefaults.Options))!;

        const string distinctiveMeaning = "nghiaxyz123duynhat";
        (await admin.PutAsJsonAsync($"/api/admin/words/{wordId}", new
        {
            version = before.Version, meaningsVi = new[] { distinctiveMeaning }, meaningViStatus = "reviewed",
            hanViet = before.HanViet, hanVietStatus = "reviewed"
        }, JsonDefaults.Options)).StatusCode.Should().Be(HttpStatusCode.OK);

        // R-CA9 "cache từ điển được làm mới sau khi sửa" — tìm NGAY (không cần khởi động lại) phải ra từ.
        var searchResponse = await admin.GetAsync($"/api/dictionary/search?q={distinctiveMeaning}");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchResult = await searchResponse.Content.ReadFromJsonAsync<SearchResultTestDto>(JsonDefaults.Options);
        searchResult!.Items.Should().Contain(i => i.Id == wordId);
    }

    [DbFact]
    public async Task DuyetHangLoat_MotMucSaiVersion_UpdatedHaiConflictMot()
    {
        using var factory = new ChineseDbApiFactory();
        var admin = await AdminClientAsync(factory);

        // hsk3Level=7 (KHÔNG PHẢI 1) — DictionarySearchTests/RealContentTests/WritingApiTests đếm
        // CHÍNH XÁC 500/300 từ/chữ ở cấp 1 từ kho HSK1 THẬT; dùng cấp 1 ở đây sẽ làm lệch số đó.
        var (wordId1, _) = await ImportWordAsync(hsk3Level: 7, meaningVi: "nghĩa 1", hanViet: null);
        var (wordId2, _) = await ImportWordAsync(hsk3Level: 7, meaningVi: "nghĩa 2", hanViet: null);
        var (wordId3, _) = await ImportWordAsync(hsk3Level: 7, meaningVi: "nghĩa 3", hanViet: null);

        var v1 = await GetVersionAsync(admin, wordId1);
        var v2 = await GetVersionAsync(admin, wordId2);
        var v3 = await GetVersionAsync(admin, wordId3);

        var response = await admin.PostAsJsonAsync("/api/admin/words/review", new
        {
            items = new object[]
            {
                new { id = wordId1, version = v1 },
                new { id = wordId2, version = v2 + 999u }, // version lệch cố ý
                new { id = wordId3, version = v3 }
            }
        }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BulkReviewResultTestDto>(JsonDefaults.Options);
        body!.Updated.Should().Be(2);
        body.Conflicts.Should().Equal(wordId2);
        body.NotFound.Should().BeEmpty();

        await using var db = TestDbContextFactory.Create();
        (await db.Words.AsNoTracking().Where(w => w.Id == wordId1).Select(w => w.MeaningViStatus).SingleAsync()).Should().Be("reviewed");
        (await db.Words.AsNoTracking().Where(w => w.Id == wordId2).Select(w => w.MeaningViStatus).SingleAsync()).Should().Be("machine"); // KHÔNG đổi
        (await db.Words.AsNoTracking().Where(w => w.Id == wordId3).Select(w => w.MeaningViStatus).SingleAsync()).Should().Be("reviewed");
    }

    // ---- tiện ích ----

    private static async Task<HttpClient> AdminClientAsync(ChineseDbApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.TokenFactory.CreateToken(Guid.NewGuid(), email: ChineseDbApiFactory.BootstrapAdminEmail));
        (await client.GetAsync("/api/me")).EnsureSuccessStatusCode();
        return client;
    }

    private static HttpClient LearnerClient(ChineseDbApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenFactory.CreateToken(Guid.NewGuid()));
        return client;
    }

    private static async Task<uint> GetVersionAsync(HttpClient admin, Guid wordId)
    {
        var dto = await (await admin.GetAsync($"/api/admin/words/{wordId}")).Content.ReadFromJsonAsync<AdminWordTestDto>(JsonDefaults.Options);
        return dto!.Version;
    }

    /// <summary>Chữ Hán "鑫淼" (đã có ở <c>characters-protected.json</c>) + MỘT âm tiết thứ ba NGẪU NHIÊN (chỉ chữ cái a-f + thanh) làm khoá tự nhiên (simplified, pinyin) DUY NHẤT cho mỗi lần gọi — tránh đụng độ giữa các test method/lần chạy trên CÙNG DB test dùng chung.</summary>
    private static string RandomPinyin()
    {
        var hex = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        var letters = new string([.. hex.Where(c => c is >= 'a' and <= 'f')]);
        if (letters.Length < 4)
            letters = (letters + "abcdef")[..4];
        return $"xin1 miao3 {letters[..4]}1";
    }

    private static async Task<string> WriteWordFileAsync(string pinyin, int hsk3Level, string meaningVi, string? hanViet)
    {
        var hanVietJson = hanViet is null ? "null" : $"\"{hanViet}\"";
        var hanVietStatusJson = hanViet is null ? "null" : "\"derived\"";
        var json = $$"""
            {
              "dataset": "hsk-words",
              "version": "test",
              "standard": "test",
              "license": "test",
              "counts": { "words": 1, "byHsk3Level": {}, "meaningViSource": { "machine": 1 } },
              "words": [
                {
                  "officialIndex": null,
                  "simplified": "鑫淼",
                  "traditional": null,
                  "variants": [],
                  "pinyin": "{{pinyin}}",
                  "hsk3Level": {{hsk3Level}},
                  "hsk2Level": null,
                  "hskExam2026Level": null,
                  "pathOrder": null,
                  "frequencyRank": null,
                  "pos": ["n"],
                  "usageNote": null,
                  "meaningsEn": ["F10 test word"],
                  "meaningsVi": ["{{meaningVi}}"],
                  "meaningViStatus": "machine",
                  "meaningViSource": "machine",
                  "hanViet": {{hanVietJson}},
                  "hanVietStatus": {{hanVietStatusJson}},
                  "sources": ["test"]
                }
              ]
            }
            """;

        var path = Path.Combine(Path.GetTempPath(), $"af-words-f10-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }

    /// <summary>Nạp characters (idempotent, dùng chung) + MỘT từ "鑫淼" pinyin ngẫu nhiên qua <c>ContentImporter</c> trực tiếp — trả <c>(wordId, pinyin)</c> để test sau tự tái nạp đúng khoá tự nhiên.</summary>
    private static async Task<(Guid WordId, string Pinyin)> ImportWordAsync(int hsk3Level, string meaningVi, string? hanViet)
    {
        var pinyin = RandomPinyin();
        var tempFile = await WriteWordFileAsync(pinyin, hsk3Level, meaningVi, hanViet);
        try
        {
            await using var db = TestDbContextFactory.Create();
            var importer = new ContentImporter(db, TimeProvider.System, NullLogger<ContentImporter>.Instance);
            await importer.ImportCharactersAsync(TestDataPath("characters-protected.json"), $"characters-f10-{Guid.NewGuid():N}", CancellationToken.None);
            var result = await importer.ImportWordsAsync(tempFile, $"hsk-words-f10-{Guid.NewGuid():N}", CancellationToken.None);
            result.Inserted.Should().Be(1);

            var wordId = await db.Words.AsNoTracking().Where(w => w.Simplified == "鑫淼" && w.Pinyin == pinyin).Select(w => w.Id).SingleAsync();
            return (wordId, pinyin);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private sealed record AdminWordTestDto(
        Guid Id, uint Version, string Simplified, string Pinyin, short? Hsk3Level,
        List<string> MeaningsVi, string MeaningViStatus, string MeaningViSource, string? HanViet, string? HanVietStatus, DateTime? EditedAt);
    private sealed record SearchItemTestDto(Guid Id);
    private sealed record SearchResultTestDto(List<SearchItemTestDto> Items);
    private sealed record BulkReviewResultTestDto(int Updated, List<Guid> Conflicts, List<Guid> NotFound);
}
