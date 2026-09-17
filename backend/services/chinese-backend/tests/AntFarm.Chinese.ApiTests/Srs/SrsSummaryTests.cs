using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Srs;

/// <summary>GET /api/srs/summary (§6.2, R7-4) — ranh giới "đến hạn hôm nay" (nửa hở, R-T4) và "đến hạn NGAY". Mỗi test tự dựng factory riêng (lý do xem <c>SrsReviewTests</c>).</summary>
[Collection(ChineseApiCollection.Name)]
public class SrsSummaryTests
{
    public SrsSummaryTests() => Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());

    [DbFact]
    public async Task RanhGioiDueToday_DueNow_TheoDungMocNuaHo()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        // Mốc "bây giờ" ban đầu để provision user (time_zone=Asia/Ho_Chi_Minh mặc định của TestTokenFactory).
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 15, 0, 0, TimeSpan.Zero));
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var db = TestDbContextFactory.Create();
        var wordIds = await SrsTestData.GetPathWordIdsAsync(db, 2);

        // Thẻ A: due_at = 2026-09-17T16:30Z.
        await SrsTestData.InsertCardAsync(db, userId, wordIds[0], "review",
            new DateTime(2026, 9, 17, 16, 30, 0, DateTimeKind.Utc), stability: 10, difficulty: 5);
        // Thẻ B: due_at = 2026-09-18T17:00Z (ngày mai, giờ VN) — KHÔNG được tính dueToday lúc 17:30Z hôm nay.
        await SrsTestData.InsertCardAsync(db, userId, wordIds[1], "review",
            new DateTime(2026, 9, 18, 17, 0, 0, DateTimeKind.Utc), stability: 10, difficulty: 5);

        // 16:00Z (23:00 VN 17/09): dueToday tính thẻ A (16:30Z < endOfTodayUtc 17:00Z 17/09→18/09); dueNow KHÔNG (16:30 > 16:00).
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 16, 0, 0, TimeSpan.Zero));
        var summaryBefore = await GetSummaryAsync(client);
        summaryBefore.DueToday.Should().Be(1);
        summaryBefore.DueNow.Should().Be(0);

        // 17:30Z (00:30 VN 18/09 — ngày MỚI): dueNow tính thẻ A (16:30 <= 17:30); thẻ B (due 18/09 17:00Z)
        // KHÔNG tính dueToday hôm nay (đúng mốc nửa hở — "hôm nay" giờ là 18/09, endOfTodayUtc = 19/09T17:00Z,
        // 18/09T17:00Z < 19/09T17:00Z lẽ ra đúng NHƯNG so bằng chính xác 18/09T17:00Z là NGOÀI "hôm nay" 18/09
        // theo local time — kiểm bằng dueNow đã tăng lên 1 thay vì suy luận dueToday một mình).
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 17, 30, 0, TimeSpan.Zero));
        var summaryAfter = await GetSummaryAsync(client);
        summaryAfter.LocalDate.Should().Be(new DateOnly(2026, 9, 18));
        summaryAfter.DueNow.Should().Be(1); // thẻ A (đã quá hạn từ hôm qua vẫn tính)
        summaryAfter.DueToday.Should().Be(1); // thẻ B (due đúng 18/09T17:00Z = đầu ngày 19/09) KHÔNG tính — nửa hở
    }

    private async Task<SummaryDto> GetSummaryAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/srs/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SummaryDto>(JsonDefaults.Options))!;
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

    private sealed record SummaryDto(DateOnly LocalDate, int DueToday, int DueNow);
}
