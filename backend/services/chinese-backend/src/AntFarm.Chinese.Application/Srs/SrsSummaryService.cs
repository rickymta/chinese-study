using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Common.Time;
using AntFarm.Chinese.Application.Learning;
using AntFarm.Chinese.Domain.Srs;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Srs;

/// <summary>GET /api/srs/summary (§6.2, R7-4..R7-6, R7-13) — dùng lại cho khối "summary" lồng trong queue/review (K13, F11).</summary>
public sealed class SrsSummaryService(IChineseDbContext db, IUserDayContext userDayContext, LearnerSettingsService learnerSettingsService)
{
    public async Task<SrsSummaryDto> GetAsync(Guid userId, CancellationToken ct)
    {
        var day = await userDayContext.GetAsync(userId, ct);
        var settings = await learnerSettingsService.GetEffectiveAsync(userId, ct);

        // R7-4: dueToday = state<>'new', không tạm dừng, due_at < endOfTodayUtc (nửa hở); dueNow
        // dùng CÙNG bộ lọc nhưng cận trên là "bây giờ" — hai con số khác nhau đúng bởi mốc cắt.
        var dueToday = await db.SrsCards.AsNoTracking().CountAsync(c =>
            c.UserId == userId && c.State != SrsState.New && !c.IsSuspended && c.DueAt < day.EndUtc, ct);
        var dueNow = await db.SrsCards.AsNoTracking().CountAsync(c =>
            c.UserId == userId && c.State != SrsState.New && !c.IsSuspended && c.DueAt <= day.NowUtc, ct);

        var reviewedToday = await db.SrsReviewLogs.AsNoTracking()
            .CountAsync(l => l.UserId == userId && l.LocalDate == day.LocalDate, ct);
        var reviewsDoneToday = await db.SrsReviewLogs.AsNoTracking()
            .CountAsync(l => l.UserId == userId && l.LocalDate == day.LocalDate && l.StateBefore == SrsState.Review, ct);
        var reviewLimitRemaining = Math.Max(0, settings.DailyReviewLimit - reviewsDoneToday);

        // R7-5: newIntroducedToday đếm theo mốc first_reviewed_local_date (đặt MỘT LẦN, SrsCard.Apply).
        var newIntroducedToday = await db.SrsCards.AsNoTracking()
            .CountAsync(c => c.UserId == userId && c.FirstReviewedLocalDate == day.LocalDate, ct);

        var availableNewCount = await CountAvailableNewCardsAsync(userId, ct);
        var newAvailableToday = Math.Max(0, Math.Min(settings.DailyNewCards - newIntroducedToday, availableNewCount));

        var totalCards = await db.SrsCards.AsNoTracking().CountAsync(c => c.UserId == userId && c.State != SrsState.New, ct);
        var matureCards = await db.SrsCards.AsNoTracking()
            .CountAsync(c => c.UserId == userId && c.State == SrsState.Review && c.Stability >= 21, ct);

        var nextDueAt = await db.SrsCards.AsNoTracking()
            .Where(c => c.UserId == userId && c.State != SrsState.New && !c.IsSuspended && c.DueAt > day.NowUtc)
            .OrderBy(c => c.DueAt)
            .Select(c => (DateTime?)c.DueAt)
            .FirstOrDefaultAsync(ct);

        return new SrsSummaryDto(
            day.LocalDate, day.TimeZoneId,
            dueToday, dueNow,
            reviewedToday, reviewsDoneToday, reviewLimitRemaining, settings.DailyReviewLimit,
            newIntroducedToday, newAvailableToday, settings.DailyNewCards,
            totalCards, matureCards, nextDueAt);
    }

    /// <summary>
    /// R7-5 "thẻ mới khả dụng" = thẻ <c>new</c> không tạm dừng đã có (tạo lười lượt trước, hoặc
    /// <c>manual</c>/<c>lesson</c>) CỘNG từ lộ trình HSK1 chưa có thẻ nào (sẽ được tạo lười khi lấy
    /// hàng đợi, <see cref="SrsQueueService"/>) — đây CHỈ ĐẾM, không tạo thẻ.
    /// </summary>
    internal async Task<int> CountAvailableNewCardsAsync(Guid userId, CancellationToken ct)
    {
        var existingNewCount = await db.SrsCards.AsNoTracking()
            .CountAsync(c => c.UserId == userId && c.State == SrsState.New && !c.IsSuspended, ct);

        var pathWithoutCardCount = await db.Words.AsNoTracking()
            .Where(w => w.Hsk3Level == 1 && w.PathOrder != null)
            .Where(w => !db.SrsCards.Any(c => c.UserId == userId && c.WordId == w.Id))
            .CountAsync(ct);

        return existingNewCount + pathWithoutCardCount;
    }
}
