using System.Xml;
using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Common.Time;
using AntFarm.Chinese.Application.Learning;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Srs;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Srs;

/// <summary>GET /api/srs/queue (§6.2, R7-7) — dựng hàng đợi ôn tập theo 4 nhóm ưu tiên, tạo LƯỜI thẻ lộ trình còn thiếu (R7-2, K12).</summary>
public sealed class SrsQueueService(
    IChineseDbContext db,
    IUserDayContext userDayContext,
    LearnerSettingsService learnerSettingsService,
    ISrsCardService srsCardService,
    SrsSummaryService summaryService)
{
    /// <summary>"Học trước" (R7-7 bước 4) — thẻ learning/relearning chưa đến hạn NGAY nhưng sắp tới trong 20 phút nữa, để người học ôn liền một mạch thay vì phải quay lại app nhiều lần/giờ.</summary>
    private static readonly TimeSpan AheadWindow = TimeSpan.FromMinutes(20);

    public async Task<SrsQueueDto> GetQueueAsync(Guid userId, int limit, CancellationToken ct)
    {
        var day = await userDayContext.GetAsync(userId, ct);
        var settings = await learnerSettingsService.GetEffectiveAsync(userId, ct);

        // Gọi summary TRƯỚC khi tạo thẻ path mới — reviewLimitRemaining/newAvailableToday không
        // đổi khi một từ lộ trình "chưa có thẻ" chuyển thành thẻ new (chỉ dịch chuyển GIỮA hai vế
        // của availableNewCount, không đổi tổng — xem SrsSummaryService.CountAvailableNewCardsAsync)
        // nên dùng LẠI được nguyên object này cho cả việc chọn nhóm 3 lẫn phản hồi cuối cùng.
        var summary = await summaryService.GetAsync(userId, ct);

        var dueLearning = await db.SrsCards
            .Where(c => c.UserId == userId && !c.IsSuspended &&
                        (c.State == SrsState.Learning || c.State == SrsState.Relearning) && c.DueAt <= day.NowUtc)
            .OrderBy(c => c.DueAt)
            .Take(limit)
            .ToListAsync(ct);

        var remaining = limit - dueLearning.Count;

        List<SrsCard> dueReview = remaining <= 0
            ? []
            : await db.SrsCards
                .Where(c => c.UserId == userId && !c.IsSuspended && c.State == SrsState.Review && c.DueAt < day.EndUtc)
                .OrderBy(c => c.DueAt)
                .Take(Math.Min(remaining, summary.ReviewLimitRemaining))
                .ToListAsync(ct);

        remaining = limit - dueLearning.Count - dueReview.Count;

        List<SrsCard> newCards = remaining > 0
            ? await SelectNewCardsAsync(userId, Math.Min(remaining, summary.NewAvailableToday), ct)
            : [];

        remaining = limit - dueLearning.Count - dueReview.Count - newCards.Count;

        var dueLearningIds = dueLearning.Select(c => c.Id).ToHashSet();
        var aheadCutoff = day.NowUtc + AheadWindow;
        List<SrsCard> ahead = remaining <= 0
            ? []
            : await db.SrsCards
                .Where(c => c.UserId == userId && !c.IsSuspended &&
                            (c.State == SrsState.Learning || c.State == SrsState.Relearning) &&
                            c.DueAt <= aheadCutoff && !dueLearningIds.Contains(c.Id))
                .OrderBy(c => c.DueAt)
                .Take(remaining)
                .ToListAsync(ct);

        var grouped = new List<(SrsCard Card, string Queue)>(dueLearning.Count + dueReview.Count + newCards.Count + ahead.Count);
        grouped.AddRange(dueLearning.Select(c => (c, "learning")));
        grouped.AddRange(dueReview.Select(c => (c, "review")));
        grouped.AddRange(newCards.Select(c => (c, "new")));
        grouped.AddRange(ahead.Select(c => (c, "ahead")));

        var items = await BuildQueueItemsAsync(grouped, (double)settings.DesiredRetention, day.NowUtc, ct);

        return new SrsQueueDto(day.NowUtc, items, summary);
    }

    /// <summary>Nhóm 3 (R7-7 bước 3, R-LS6/K12): thẻ <c>new</c> ĐÃ TỒN TẠI trước (theo <c>created_at</c>) — kể cả nguồn <c>lesson</c>/<c>manual</c> — RỒI MỚI tới từ lộ trình chưa có thẻ (theo <c>path_order</c>, tạo lười qua <see cref="ISrsCardService.EnsureCardsAsync"/>).</summary>
    private async Task<List<SrsCard>> SelectNewCardsAsync(Guid userId, int take, CancellationToken ct)
    {
        if (take <= 0)
            return [];

        var result = new List<SrsCard>(take);

        // ThenBy(path_order): nhiều thẻ tạo LƯỜI cùng một lượt gọi (vd 10 thẻ path đầu tiên) có
        // CÙNG created_at (một biến `now` dùng chung cho cả vòng lặp, EnsureCardsAsync) —
        // created_at KHÔNG đủ để sắp ổn định (Postgres không cam kết thứ tự khi khoá sắp bằng
        // nhau, ĐÃ kiểm chứng Guid.CreateVersion7() cũng KHÔNG đơn điệu trong cùng mili-giây, phần
        // ngẫu nhiên sau dấu thời gian). path_order NULL (thẻ manual/lesson) xếp SAU cùng.
        var existingNew = await (
                from c in db.SrsCards
                join w in db.Words on c.WordId equals w.Id
                where c.UserId == userId && c.State == SrsState.New && !c.IsSuspended
                orderby c.CreatedAt, w.PathOrder ?? int.MaxValue
                select c)
            .Take(take)
            .ToListAsync(ct);
        result.AddRange(existingNew);

        var stillNeeded = take - existingNew.Count;
        if (stillNeeded <= 0)
            return result;

        var existingWordIds = await db.SrsCards.Where(c => c.UserId == userId).Select(c => c.WordId).ToListAsync(ct);
        var pathWordIds = await db.Words
            .Where(w => w.Hsk3Level == 1 && w.PathOrder != null && !existingWordIds.Contains(w.Id))
            .OrderBy(w => w.PathOrder)
            .Take(stillNeeded)
            .Select(w => w.Id)
            .ToListAsync(ct);

        if (pathWordIds.Count == 0)
            return result;

        // EnsureCardsAsync ghi NGAY qua SQL (ON CONFLICT DO NOTHING, không qua ChangeTracker) —
        // KHÔNG cần SaveChangesAsync ở đây (review F7.1: 2 tab cùng GET queue cho người mới không
        // còn ném 23505 ⇒ 500, xem SrsCardService.InsertNewCardIfMissingAsync).
        await srsCardService.EnsureCardsAsync(userId, pathWordIds, SrsCardSources.Path, ct);

        var createdByWordId = await db.SrsCards
            .Where(c => c.UserId == userId && pathWordIds.Contains(c.WordId))
            .ToDictionaryAsync(c => c.WordId, ct);

        // Giữ đúng thứ tự path_order (thứ tự đã Take ở trên) thay vì thứ tự Dictionary trả về.
        foreach (var wordId in pathWordIds)
            if (createdByWordId.TryGetValue(wordId, out var card))
                result.Add(card);

        return result;
    }

    private async Task<List<SrsQueueCardDto>> BuildQueueItemsAsync(
        IReadOnlyList<(SrsCard Card, string Queue)> grouped, double desiredRetention, DateTime nowUtc, CancellationToken ct)
    {
        if (grouped.Count == 0)
            return [];

        var wordIds = grouped.Select(x => x.Card.WordId).Distinct().ToList();
        var wordsById = await db.Words.AsNoTracking().Where(w => wordIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, ct);

        var scheduler = new FsrsScheduler(FsrsOptions.Default(desiredRetention));
        var items = new List<SrsQueueCardDto>(grouped.Count);
        foreach (var (card, queue) in grouped)
        {
            var word = wordsById[card.WordId];
            var preview = scheduler.Preview(card.ToMemory(), nowUtc);
            items.Add(new SrsQueueCardDto(card.Id, card.State, queue, card.DueAt, ToWordDto(word), ToIntervalsDto(preview)));
        }

        return items;
    }

    private static SrsQueueWordDto ToWordDto(Word w) =>
        new(w.Id, w.Simplified, w.Traditional, w.Pinyin, w.HanViet, w.MeaningsVi.Take(3).ToList(), w.MeaningViStatus);

    private static IReadOnlyDictionary<string, string> ToIntervalsDto(IReadOnlyDictionary<SrsRating, TimeSpan> preview) =>
        new Dictionary<string, string>
        {
            ["again"] = XmlConvert.ToString(preview[SrsRating.Again]),
            ["hard"] = XmlConvert.ToString(preview[SrsRating.Hard]),
            ["good"] = XmlConvert.ToString(preview[SrsRating.Good]),
            ["easy"] = XmlConvert.ToString(preview[SrsRating.Easy])
        };
}
