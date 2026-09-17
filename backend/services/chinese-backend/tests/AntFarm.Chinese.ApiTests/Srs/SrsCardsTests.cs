using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Srs;

/// <summary>POST /api/srs/cards, PUT /api/srs/cards/{cardId}/suspension (§6.2, R7-2).</summary>
[Collection(ChineseApiCollection.Name)]
public class SrsCardsTests
{
    public SrsCardsTests() => Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());

    [DbFact]
    public async Task ThemThe_TuChuaCo_Created_TuDaCo_KhongTaoLai()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        await using var db = TestDbContextFactory.Create();
        var wordIds = await SrsTestData.GetPathWordIdsAsync(db, 2);

        var firstResponse = await client.PostAsJsonAsync("/api/srs/cards", new { wordIds = new[] { wordIds[0] } }, JsonDefaults.Options);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var first = await firstResponse.Content.ReadFromJsonAsync<AddCardsResultDto>(JsonDefaults.Options);
        first!.Added.Should().Be(1);
        first.Skipped.Should().Be(0);
        first.Cards.Single().Created.Should().BeTrue();

        // Gửi lại CẢ hai (từ đã có thẻ + từ mới) — chỉ 1 thẻ mới được tạo.
        var secondResponse = await client.PostAsJsonAsync("/api/srs/cards", new { wordIds }, JsonDefaults.Options);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var second = await secondResponse.Content.ReadFromJsonAsync<AddCardsResultDto>(JsonDefaults.Options);
        second!.Added.Should().Be(1);
        second.Skipped.Should().Be(1);

        (await db.SrsCards.CountAsync(c => c.UserId == userId)).Should().Be(2);
    }

    [DbFact]
    public async Task ThemThe_TuKhongTonTai_Tra422UnknownWord_KhongThemTheNao()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        await using var db = TestDbContextFactory.Create();
        var realWordId = (await SrsTestData.GetPathWordIdsAsync(db, 1)).Single();
        var fakeWordId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/api/srs/cards", new { wordIds = new[] { realWordId, fakeWordId } }, JsonDefaults.Options);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("UNKNOWN_WORD");
        (await db.SrsCards.CountAsync(c => c.UserId == userId)).Should().Be(0);
    }

    [DbFact]
    public async Task TamDungThe_DatCoTamDung_TheCuaNguoiKhac404()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        await using var db = TestDbContextFactory.Create();
        var wordId = (await SrsTestData.GetPathWordIdsAsync(db, 1)).Single();
        var addResponse = await client.PostAsJsonAsync("/api/srs/cards", new { wordIds = new[] { wordId } }, JsonDefaults.Options);
        var added = await addResponse.Content.ReadFromJsonAsync<AddCardsResultDto>(JsonDefaults.Options);
        var cardId = added!.Cards.Single().CardId;

        var suspendResponse = await client.PutAsJsonAsync($"/api/srs/cards/{cardId}/suspension", new { suspended = true }, JsonDefaults.Options);
        suspendResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var suspended = await suspendResponse.Content.ReadFromJsonAsync<CardDto>(JsonDefaults.Options);
        suspended!.IsSuspended.Should().BeTrue();

        var strangerClient = ClientFor(factory, Guid.NewGuid());
        var strangerResponse = await strangerClient.PutAsJsonAsync($"/api/srs/cards/{cardId}/suspension", new { suspended = false }, JsonDefaults.Options);
        strangerResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static HttpClient ClientFor(ChineseDbApiFactory factory, Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenFactory.CreateToken(userId));
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

    private sealed record AddCardResultItemDto(Guid WordId, Guid CardId, bool Created);
    private sealed record AddCardsResultDto(int Added, int Skipped, List<AddCardResultItemDto> Cards);
    private sealed record CardDto(Guid CardId, bool IsSuspended);
    private sealed record ErrorDto(string Error, string Code);
}
