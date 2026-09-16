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
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE content.words_backup_f6 AS TABLE content.words");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE content.word_characters_backup_f6 AS TABLE content.word_characters");

        try
        {
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
            await db.Database.ExecuteSqlRawAsync("DROP TABLE content.words_backup_f6");
            await db.Database.ExecuteSqlRawAsync("DROP TABLE content.word_characters_backup_f6");
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
