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

/// <summary>GET /api/dictionary/words/{id} và GET /api/dictionary/characters/{hanzi} (§6.1) — dữ liệu HSK1 thật.</summary>
[Collection(ChineseApiCollection.Name)]
public class WordDetailTests : IClassFixture<ChineseDbApiFactory>
{
    private readonly ChineseDbApiFactory _factory;

    public WordDetailTests(ChineseDbApiFactory factory)
    {
        _factory = factory;
        Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());
    }

    [DbFact]
    public async Task GetWord_TuAi_CoChuVaHanViet()
    {
        var id = await GetWordIdAsync("爱", "ai4");

        var response = await LearnerClient().GetAsync($"/api/dictionary/words/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<WordDetailResponse>(JsonDefaults.Options);
        body!.Simplified.Should().Be("爱");
        body.HanViet.Should().Be("ái");
        body.Characters.Should().ContainSingle(c => c.Hanzi == "爱");
        body.Srs.Should().BeNull(); // F6: chưa có SRS
    }

    [DbFact]
    public async Task GetWord_IdLa_Tra404()
    {
        var response = await LearnerClient().GetAsync($"/api/dictionary/words/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [DbFact]
    public async Task GetCharacter_Ai_TraVeTuChua爱()
    {
        var response = await LearnerClient().GetAsync("/api/dictionary/characters/" + HttpUtility.UrlEncode("爱"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<CharacterDetailResponse>(JsonDefaults.Options);
        body!.Hanzi.Should().Be("爱");
        body.Words.Select(w => w.Simplified).Should().Contain(["爱", "爱好"]);
    }

    [DbFact]
    public async Task GetCharacter_HaiKyTu_Tra400Validation()
    {
        var response = await LearnerClient().GetAsync("/api/dictionary/characters/ab");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("VALIDATION");
    }

    [DbFact]
    public async Task GetCharacter_ChuKhongCo_Tra404()
    {
        // 龘 không nằm trong 300 chữ HSK1 thật.
        var response = await LearnerClient().GetAsync("/api/dictionary/characters/" + HttpUtility.UrlEncode("龘"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> GetWordIdAsync(string simplified, string pinyin)
    {
        await using var db = TestDbContextFactory.Create();
        var word = await db.Words.AsNoTracking().FirstAsync(w => w.Simplified == simplified && w.Pinyin == pinyin);
        return word.Id;
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

    private sealed record WordCharacterResponse(string Hanzi, List<string> PinyinReadings, List<string> HanViet, short? StrokeCount);

    private sealed record WordDetailResponse(Guid Id, string Simplified, string? HanViet, List<WordCharacterResponse> Characters, object? Srs);

    private sealed record CharacterWordResponse(Guid Id, string Simplified);

    private sealed record CharacterDetailResponse(string Hanzi, List<CharacterWordResponse> Words);

    private sealed record ErrorDto(string Error, string Code);
}
