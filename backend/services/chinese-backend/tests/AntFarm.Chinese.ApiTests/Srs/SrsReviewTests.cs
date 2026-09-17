using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Srs;

/// <summary>
/// POST /api/srs/cards/{cardId}/reviews (§5.2.8, §6.2, R7-8..R7-12) — dữ liệu HSK1 THẬT. MỖI TEST
/// tự dựng <see cref="ChineseDbApiFactory"/> RIÊNG (không dùng <c>IClassFixture</c> dùng chung một
/// factory cho cả lớp) — các test đặt mốc giờ tuyệt đối bằng <c>AdjustTime</c> (KHÔNG dùng <c>SetUtcNow</c>: hàm này cấm lùi giờ nên thành "bom hẹn giờ" khi giờ thật vượt mốc), mà các test ở đây
/// cần đặt mốc giờ ĐỘC LẬP nhau (không theo thứ tự tăng dần); DB vẫn dùng chung một
/// <c>af_chinese_test</c> qua <see cref="ChineseDbFixture"/> (§9.2), mỗi test một userId riêng nên không đụng nhau.
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class SrsReviewTests
{
    public SrsReviewTests() => Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());

    [DbFact]
    public async Task Cham10TheMoi_NewIntroducedToday10_NewAvailable0_TheThu11BiChan()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        // 2026-09-17T10:00Z = 17:00 giờ VN (cùng ngày 17/09).
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 10, 0, 0, TimeSpan.Zero));

        var queue = await GetQueueAsync(client);
        queue.Cards.Should().HaveCount(10);

        foreach (var card in queue.Cards)
            (await ReviewAsync(client, card.CardId, "good")).StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await GetSummaryAsync(client);
        summary.NewIntroducedToday.Should().Be(10);
        summary.NewAvailableToday.Should().Be(0);

        var queueAfter = await GetQueueAsync(client);
        queueAfter.Cards.Should().NotContain(c => c.Queue == "new");

        // Thêm một thẻ khác (thủ công) rồi cố chấm ⇒ 422 NEW_CARD_LIMIT_REACHED (R7-10) — hạn mức
        // áp cho MỌI thẻ 'new' bất kể nguồn, không chỉ thẻ lộ trình.
        await using var db = TestDbContextFactory.Create();
        var extraWordId = (await SrsTestData.GetPathWordIdsAsync(db, 1, skip: 10)).Single();
        var addResponse = await client.PostAsJsonAsync("/api/srs/cards", new { wordIds = new[] { extraWordId } }, JsonDefaults.Options);
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var added = await addResponse.Content.ReadFromJsonAsync<AddCardsResultDto>(JsonDefaults.Options);

        var blocked = await ReviewAsync(client, added!.Cards[0].CardId, "good");
        blocked.StatusCode.Should().Be((HttpStatusCode)422);
        var errorBody = await blocked.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        errorBody!.Code.Should().Be("NEW_CARD_LIMIT_REACHED");
    }

    [DbFact]
    public async Task QuaNuaDemGioVN_LocalDateSangNgayMoi_NewAvailableTinhLaiVe10()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        // 2026-09-17T23:30Z = 06:30 giờ VN NGÀY 18/09 — cắt theo UTC (chưa đổi ngày) sẽ SAI (R7-3).
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 23, 30, 0, TimeSpan.Zero));

        var summary = await GetSummaryAsync(client);
        summary.LocalDate.Should().Be(new DateOnly(2026, 9, 18));
        summary.NewAvailableToday.Should().Be(10); // KHÔNG phải 0 — bài kiểm bắt lỗi cắt theo UTC.

        var queue = await GetQueueAsync(client);
        var cardId = queue.Cards[0].CardId;
        (await ReviewAsync(client, cardId, "good")).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var db = TestDbContextFactory.Create();
        var localDate = await db.StudyEvents.AsNoTracking()
            .Where(e => e.UserId == userId && e.Kind == "srs_review")
            .Select(e => e.LocalDate)
            .SingleAsync();
        localDate.Should().Be(new DateOnly(2026, 9, 18));
    }

    [DbFact]
    public async Task TrungClientReviewId_LanHaiTraDuplicateTrue_KhongTaoLogThuHai()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 1, 0, 0, TimeSpan.Zero));

        var queue = await GetQueueAsync(client);
        var cardId = queue.Cards[0].CardId;
        var clientReviewId = Guid.NewGuid();

        var first = await ReviewAsync(client, cardId, "good", clientReviewId);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadFromJsonAsync<ReviewResultDto>(JsonDefaults.Options);
        firstBody!.Duplicate.Should().BeFalse();

        var second = await ReviewAsync(client, cardId, "good", clientReviewId);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<ReviewResultDto>(JsonDefaults.Options);
        secondBody!.Duplicate.Should().BeTrue();
        secondBody.Card.Reps.Should().Be(1);

        await using var db = TestDbContextFactory.Create();
        (await db.SrsReviewLogs.CountAsync(l => l.ClientReviewId == clientReviewId)).Should().Be(1);
        (await db.StudyEvents.CountAsync(e => e.UserId == userId && e.Kind == "srs_review")).Should().Be(1);

        // Cùng id, THẺ KHÁC ⇒ 409.
        var otherCardId = queue.Cards[1].CardId;
        var conflict = await ReviewAsync(client, otherCardId, "good", clientReviewId);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var conflictBody = await conflict.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        conflictBody!.Code.Should().Be("CLIENT_REVIEW_ID_CONFLICT");
    }

    [DbFact]
    public async Task HaiRequestSongSongCungClientReviewId_ChiTaoMotLog()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 1, 0, 0, TimeSpan.Zero));

        var queue = await GetQueueAsync(client);
        var cardId = queue.Cards[0].CardId;
        var clientReviewId = Guid.NewGuid();

        var t1 = ReviewAsync(client, cardId, "good", clientReviewId);
        var t2 = ReviewAsync(client, cardId, "good", clientReviewId);
        var results = await Task.WhenAll(t1, t2);

        results.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

        await using var db = TestDbContextFactory.Create();
        (await db.SrsReviewLogs.CountAsync(l => l.ClientReviewId == clientReviewId)).Should().Be(1);
    }

    [DbFact]
    public async Task TheCuaNguoiKhac_Tra404()
    {
        using var factory = new ChineseDbApiFactory();
        var owner = Guid.NewGuid();
        var ownerClient = ClientFor(factory, owner);
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 1, 0, 0, TimeSpan.Zero));
        var queue = await GetQueueAsync(ownerClient);
        var cardId = queue.Cards[0].CardId;

        var stranger = Guid.NewGuid();
        var strangerClient = ClientFor(factory, stranger);
        var response = await ReviewAsync(strangerClient, cardId, "good");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [DbFact]
    public async Task TheTamDung_Tra422CardSuspended()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        var now = new DateTimeOffset(2026, 9, 17, 1, 0, 0, TimeSpan.Zero);
        factory.TimeProvider.AdjustTime(now);
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK); // provision trước khi chèn thẳng SQL (FK access.users)

        await using var db = TestDbContextFactory.Create();
        var wordId = (await SrsTestData.GetPathWordIdsAsync(db, 1)).Single();
        var cardId = await SrsTestData.InsertCardAsync(db, userId, wordId, "new", now.UtcDateTime, isSuspended: true);

        var response = await ReviewAsync(client, cardId, "good");

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("CARD_SUSPENDED");
    }

    [DbFact]
    public async Task RatingKhongHopLe_Tra400Validation()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 1, 0, 0, TimeSpan.Zero));
        var queue = await GetQueueAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/srs/cards/{queue.Cards[0].CardId}/reviews",
            new { clientReviewId = Guid.NewGuid(), rating = "ok" }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("VALIDATION");
    }

    private async Task<QueueResultDto> GetQueueAsync(HttpClient client, int limit = 20)
    {
        var response = await client.GetAsync($"/api/srs/queue?limit={limit}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<QueueResultDto>(JsonDefaults.Options))!;
    }

    private async Task<SummaryDto> GetSummaryAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/srs/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SummaryDto>(JsonDefaults.Options))!;
    }

    private static Task<HttpResponseMessage> ReviewAsync(HttpClient client, Guid cardId, string rating, Guid? clientReviewId = null) =>
        client.PostAsJsonAsync(
            $"/api/srs/cards/{cardId}/reviews",
            new { clientReviewId = clientReviewId ?? Guid.NewGuid(), rating }, JsonDefaults.Options);

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

    private sealed record QueueCardDto(Guid CardId, string State, string Queue);
    private sealed record QueueResultDto(List<QueueCardDto> Cards);
    private sealed record SummaryDto(DateOnly LocalDate, int NewIntroducedToday, int NewAvailableToday);
    private sealed record ReviewCardResultDto(int Reps);
    private sealed record ReviewResultDto(Guid ReviewId, bool Duplicate, ReviewCardResultDto Card);
    private sealed record AddCardResultItemDto(Guid WordId, Guid CardId, bool Created);
    private sealed record AddCardsResultDto(int Added, int Skipped, List<AddCardResultItemDto> Cards);
    private sealed record ErrorDto(string Error, string Code);
}
