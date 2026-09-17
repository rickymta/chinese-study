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
/// GET /api/srs/queue (§5.2.8, §6.2, R7-7) — dữ liệu HSK1 THẬT (500 từ, nạp bởi
/// <c>ContentImportRunner</c>). MỖI TEST tự dựng <see cref="ChineseDbApiFactory"/> RIÊNG (không
/// <c>IClassFixture</c> dùng chung) — lý do xem <c>SrsReviewTests</c> (FakeTimeProvider không lùi
/// giờ được, các test cần mốc giờ độc lập).
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class SrsQueueTests
{
    public SrsQueueTests() => Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());

    [DbFact]
    public async Task NguoiMoi_HangDoiTraDung10TheMoiTheoPathOrder_KhongNhanDoiKhiGoiLai()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        var first = await GetQueueAsync(client);
        first.Cards.Should().HaveCount(10);
        first.Cards.Should().OnlyContain(c => c.State == "new" && c.Queue == "new");

        await using var db = TestDbContextFactory.Create();
        var expectedWordIds = await SrsTestData.GetPathWordIdsAsync(db, 10);
        first.Cards.Select(c => c.Word.Id).Should().Equal(expectedWordIds);

        (await db.SrsCards.CountAsync(c => c.UserId == userId)).Should().Be(10);

        var second = await GetQueueAsync(client);
        second.Cards.Select(c => c.CardId).Should().Equal(first.Cards.Select(c => c.CardId));

        (await db.SrsCards.CountAsync(c => c.UserId == userId)).Should().Be(10); // không nhân đôi
    }

    /// <summary>
    /// Review F7.1: 2 tab cùng mở màn ôn tập cho một người MỚI TOANH gọi <c>GET /api/srs/queue</c>
    /// gần như đồng thời — cả hai request đều lười tạo 10 thẻ path ĐẦU TIÊN CHƯA TỒN TẠI (chưa kịp
    /// thấy nhau) ⇒ trước fix, request thứ hai ném <c>23505</c> vi phạm
    /// <c>ux_srs_cards_user_word_type</c> ⇒ 500 (StrictMode dev, F9 hoàn thành bài học cũng có thể
    /// đua theo cách này). <c>SrsCardService.InsertNewCardIfMissingAsync</c> dùng
    /// <c>INSERT ... ON CONFLICT DO NOTHING</c> nên cả hai phải 200, không nhân đôi thẻ.
    /// </summary>
    [DbFact]
    public async Task NguoiMoi_GoiSongSongHaiLanGetQueue_CaHai200_KhongNhanDoiThe()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        var t1 = client.GetAsync("/api/srs/queue?limit=20");
        var t2 = client.GetAsync("/api/srs/queue?limit=20");
        var responses = await Task.WhenAll(t1, t2);

        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

        await using var db = TestDbContextFactory.Create();
        (await db.SrsCards.CountAsync(c => c.UserId == userId)).Should().Be(10);
    }

    [DbFact]
    public async Task LuotDauGood_TheChuyenLearning_XuatHienONhomHocTruoc()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        var now = new DateTimeOffset(2026, 9, 20, 1, 0, 0, TimeSpan.Zero);
        factory.TimeProvider.AdjustTime(now);

        var queue = await GetQueueAsync(client);
        var cardId = queue.Cards[0].CardId;

        var reviewResponse = await client.PostAsJsonAsync(
            $"/api/srs/cards/{cardId}/reviews",
            new { clientReviewId = Guid.NewGuid(), rating = "good" }, JsonDefaults.Options);
        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviewBody = await reviewResponse.Content.ReadFromJsonAsync<ReviewResultDto>(JsonDefaults.Options);
        reviewBody!.Card.State.Should().Be("learning");
        reviewBody.Card.DueAt.Should().Be(now.UtcDateTime.AddMinutes(10));

        // 5 phút sau — thẻ chưa TỚI hạn (due = +10p) nhưng nằm trong cửa sổ "học trước" (R7-7 bước 4, ≤ 20 phút).
        factory.TimeProvider.AdjustTime(now.AddMinutes(5));
        var laterQueue = await GetQueueAsync(client);

        laterQueue.Cards.Should().Contain(c => c.CardId == cardId && c.Queue == "ahead");
    }

    [DbFact]
    public async Task DailyReviewLimit10_15TheDenHan_ChiTra10TheReview()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        var now = new DateTimeOffset(2026, 9, 21, 1, 0, 0, TimeSpan.Zero);
        factory.TimeProvider.AdjustTime(now);

        // dailyNewCards=0 để cô lập hẳn nhóm review (không lẫn thẻ mới tạo lười, R7-7 bước 3)
        // — phép thử chỉ nhắm đúng câu hỏi "reviewLimitRemaining có chặn đúng số thẻ review không".
        var putSettings = await client.PutAsJsonAsync(
            "/api/me/learning-settings",
            new { dailyNewCards = 0, dailyReviewLimit = 10, desiredRetention = 0.90, ttsRate = 0.80, autoPlayAudio = true },
            JsonDefaults.Options);
        putSettings.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var db = TestDbContextFactory.Create();
        var wordIds = await SrsTestData.GetPathWordIdsAsync(db, 15, skip: 50);
        foreach (var wordId in wordIds)
            await SrsTestData.InsertCardAsync(db, userId, wordId, "review", now.UtcDateTime.AddMinutes(-10), stability: 10, difficulty: 5);

        var queue = await GetQueueAsync(client);

        queue.Cards.Should().HaveCount(10);
        queue.Cards.Should().OnlyContain(c => c.Queue == "review");
    }

    [DbFact]
    public async Task NguoiDungBiGoHetVaiTro_Tra403()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK); // provision trước

        await using (var db = TestDbContextFactory.Create())
        {
            var roles = await db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            db.UserRoles.RemoveRange(roles);
            await db.SaveChangesAsync();
        }

        (await client.GetAsync("/api/srs/queue")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<QueueResultDto> GetQueueAsync(HttpClient client, int limit = 20)
    {
        var response = await client.GetAsync($"/api/srs/queue?limit={limit}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<QueueResultDto>(JsonDefaults.Options))!;
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

    private sealed record QueueWordDto(Guid Id, string Simplified);
    private sealed record QueueCardDto(Guid CardId, string State, string Queue, DateTime DueAt, QueueWordDto Word, Dictionary<string, string> Intervals);
    private sealed record QueueResultDto(DateTime GeneratedAt, List<QueueCardDto> Cards, SummaryDto Summary);
    private sealed record SummaryDto(int DueToday, int DueNow, int NewAvailableToday);
    private sealed record ReviewCardResultDto(string State, DateTime DueAt);
    private sealed record ReviewResultDto(Guid ReviewId, bool Duplicate, ReviewCardResultDto Card);
}
