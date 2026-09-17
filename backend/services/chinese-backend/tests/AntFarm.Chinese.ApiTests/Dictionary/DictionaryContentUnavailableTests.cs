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
/// §6.1 (review F6.2, Chặn 2) — <c>content.words</c> rỗng (học liệu chưa nạp xong hoặc nạp lỗi lúc
/// khởi động) phải trả 503 <c>CONTENT_UNAVAILABLE</c> ở cả 3 endpoint tra từ, KHÔNG trả 200 rỗng
/// (dễ khiến người dùng tưởng "không tìm thấy"). Xoá TẠM <c>content.words</c> bằng sao lưu/khôi
/// phục thô qua SQL (không đụng <c>content.import_runs</c> — hash không đổi nên KHÔNG kích hoạt
/// importer nạp lại đè lên khi factory nào đó khởi động lại trong lúc test chạy) rồi khôi phục
/// nguyên trạng trong <c>finally</c> — các lớp test khác trong CÙNG collection chạy TUẦN TỰ
/// (§9.2) nên không bị ảnh hưởng miễn khôi phục xong trước khi trả về.
///
/// F9 (review): <c>content.lesson_words</c> tham chiếu <c>content.words</c> bằng <c>ON DELETE
/// RESTRICT</c> (R-LS14/R-CA7 — từ đã dùng trong bài KHÔNG bị xoá cứng qua thao tác KHÁC) nên
/// <c>DELETE FROM content.words</c> KHÔNG unconditional được nữa một khi 5 bài seed đã nạp (chúng
/// dùng gần hết 500 từ); <c>learning.srs_cards</c> cũng tham chiếu <c>words</c> (CASCADE — F7), và
/// <c>learning.srs_review_logs</c> lại CASCADE theo <c>srs_cards</c> (F7) — xoá thẳng words sẽ ÂM
/// THẦM xoá dây chuyền cả thẻ SRS lẫn lịch sử chấm thẻ của các test KHÁC trong CÙNG DB dùng chung.
/// Sao lưu/xoá/khôi phục CẢ BA bảng con này CÙNG lúc với <c>words</c> (khôi phục <c>srs_review_logs</c>
/// SAU <c>srs_cards</c> — <c>card_id</c> phải tồn tại trước) để test này không còn phá dữ liệu của
/// các lớp test khác (đã từng "chạy được" chỉ vì F9 chưa tồn tại lúc viết test này).
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class DictionaryContentUnavailableTests : IClassFixture<ChineseDbApiFactory>
{
    private readonly ChineseDbApiFactory _factory;

    public DictionaryContentUnavailableTests(ChineseDbApiFactory factory)
    {
        _factory = factory;
        Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());
    }

    [DbFact]
    public async Task ContentWordsRong_CaBaEndpoint_Tra503()
    {
        var client = LearnerClient();
        (await client.GetAsync("/health/live")).EnsureSuccessStatusCode(); // buộc host (đã nạp học liệu thật) khởi động xong

        await using var db = TestDbContextFactory.Create();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS content.words_backup_f6");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS content.word_characters_backup_f6");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS content.lesson_words_backup_f9");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS learning.srs_cards_backup_f9");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS learning.srs_review_logs_backup_f9");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE content.words_backup_f6 AS TABLE content.words");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE content.word_characters_backup_f6 AS TABLE content.word_characters");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE content.lesson_words_backup_f9 AS TABLE content.lesson_words");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE learning.srs_cards_backup_f9 AS TABLE learning.srs_cards");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE learning.srs_review_logs_backup_f9 AS TABLE learning.srs_review_logs");

        try
        {
            // F9: content.lesson_words (RESTRICT) chặn DELETE thẳng nếu còn tham chiếu; learning.srs_cards
            // (CASCADE) và learning.srs_review_logs (CASCADE theo srs_cards) sẽ bị xoá ÂM THẦM theo —
            // xoá TRƯỚC cả ba rồi khôi phục lại ở finally (thay vì để cascade tự lo, tránh mất thẻ/lịch
            // sử chấm SRS của test khác đang dùng chung DB). Không cần DELETE riêng srs_review_logs —
            // xoá srs_cards đã cascade xoá nó, chỉ cần đã sao lưu trước.
            await db.Database.ExecuteSqlRawAsync("DELETE FROM content.lesson_words");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM learning.srs_cards");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM content.words"); // cascade xoá luôn content.word_characters

            var searchResponse = await client.GetAsync("/api/dictionary/search?q=" + HttpUtility.UrlEncode("爱"));
            searchResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            (await searchResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("CONTENT_UNAVAILABLE");

            var wordResponse = await client.GetAsync($"/api/dictionary/words/{Guid.NewGuid()}");
            wordResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            (await wordResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("CONTENT_UNAVAILABLE");

            var characterResponse = await client.GetAsync("/api/dictionary/characters/" + HttpUtility.UrlEncode("爱"));
            characterResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            (await characterResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("CONTENT_UNAVAILABLE");
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("INSERT INTO content.words SELECT * FROM content.words_backup_f6");
            await db.Database.ExecuteSqlRawAsync("INSERT INTO content.word_characters SELECT * FROM content.word_characters_backup_f6");
            await db.Database.ExecuteSqlRawAsync("INSERT INTO content.lesson_words SELECT * FROM content.lesson_words_backup_f9");
            await db.Database.ExecuteSqlRawAsync("INSERT INTO learning.srs_cards SELECT * FROM learning.srs_cards_backup_f9");
            // srs_review_logs.card_id → srs_cards.id: khôi phục SAU KHI srs_cards đã có lại (thứ tự trên).
            await db.Database.ExecuteSqlRawAsync("INSERT INTO learning.srs_review_logs SELECT * FROM learning.srs_review_logs_backup_f9");
            await db.Database.ExecuteSqlRawAsync("DROP TABLE content.words_backup_f6");
            await db.Database.ExecuteSqlRawAsync("DROP TABLE content.word_characters_backup_f6");
            await db.Database.ExecuteSqlRawAsync("DROP TABLE content.lesson_words_backup_f9");
            await db.Database.ExecuteSqlRawAsync("DROP TABLE learning.srs_cards_backup_f9");
            await db.Database.ExecuteSqlRawAsync("DROP TABLE learning.srs_review_logs_backup_f9");
        }

        // Khôi phục xong ⇒ tra từ hoạt động lại bình thường (xác nhận restore đúng, không chỉ "không lỗi").
        var afterRestore = await client.GetAsync("/api/dictionary/search?q=" + HttpUtility.UrlEncode("爱"));
        afterRestore.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private HttpClient LearnerClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFactory.CreateToken(Guid.NewGuid()));
        return client;
    }

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

    private sealed record ErrorDto(string Error, string Code);
}
