using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Common.Time;
using AntFarm.Chinese.Application.Lessons;
using AntFarm.Chinese.Application.Pinyin;
using AntFarm.Chinese.Application.Progress.Dtos;
using AntFarm.Chinese.Application.Srs;
using AntFarm.Chinese.Application.Writing;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Progress;
using AntFarm.Chinese.Domain.Srs;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Chinese.Application.Progress;

/// <summary>
/// <c>GET /api/progress/overview</c> (§5.2.4, §6.4) — trang chủ: chuỗi ngày học, việc hôm nay, tiến
/// độ SRS/từ vựng/bài học/viết/thanh điệu, lịch hoạt động 90 ngày, TẤT CẢ theo múi giờ hồ sơ
/// (<see cref="IUserDayContext"/>, K5). Gọi TUẦN TỰ trên MỘT <see cref="IChineseDbContext"/> (không
/// song song — DbContext không an toàn đa luồng). Mỗi khối ngoài streak/today/activity bọc
/// try/catch <see cref="ServiceUnavailableException"/> ⇒ <c>null</c> + log Warning (R-PG7) — lỗi
/// khác ném bình thường (500), đúng theo hợp đồng.
/// </summary>
public sealed class ProgressOverviewService(
    IChineseDbContext db,
    IUserDayContext userDayContext,
    SrsSummaryService srsSummaryService,
    LessonQueryService lessonQueryService,
    WritingService writingService,
    WritingCharacterQueryService writingCharacterQueryService,
    ToneStatsService toneStatsService,
    IPinyinCatalog pinyinCatalog,
    ILogger<ProgressOverviewService> logger)
{
    /// <summary>Số ngày của lịch hoạt động (R-PG6) — <c>[today-89, today]</c>, 90 phần tử.</summary>
    private const int ActivityWindowDays = 90;

    /// <summary>Một bài HSK1 seed dùng tối đa vài chục chữ — đủ rộng để lấy TRỌN bộ chữ của MỘT bài trong một trang (R-W6), tránh phải phân trang khi đếm <c>unpracticedChars</c>.</summary>
    private const int LessonCharacterPageSize = 500;

    public async Task<ProgressOverviewDto> GetAsync(Guid userId, CancellationToken ct)
    {
        var day = await userDayContext.GetAsync(userId, ct);

        // R-PG3/R-PG4: chuỗi tính trên TOÀN BỘ lịch sử (không giới hạn cửa sổ 90 ngày của lịch hoạt động).
        var studiedDates = await db.StudyEvents.AsNoTracking()
            .Where(e => e.UserId == userId && e.Quantity > 0)
            .Select(e => e.LocalDate)
            .Distinct()
            .ToListAsync(ct);
        var streakResult = StreakCalculator.Calculate(studiedDates, day.LocalDate);
        var streak = new ProgressStreakDto(streakResult.Current, streakResult.Longest, streakResult.StudiedToday);

        var (activity, todayEvents) = await GetActivityAndTodayEventsAsync(userId, day.LocalDate, ct);

        var srs = await GetOrNullAsync(() => GetSrsAsync(userId, ct), "srs", ct);
        var dailyGoal = srs is null ? null : BuildDailyGoal(srs);

        // "today" LUÔN có mặt (R-PG7) — newCards lấy từ khối srs nếu có, 0 nếu srs vắng.
        var today = new ProgressTodayDto(
            todayEvents.SrsReviews, srs?.NewIntroducedToday ?? 0, todayEvents.ToneDrillItems,
            todayEvents.WritingAttempts, todayEvents.Quizzes, todayEvents.LessonsCompleted, todayEvents.ActivityCount);

        var vocabulary = await GetOrNullAsync(() => GetVocabularyAsync(userId, ct), "vocabulary", ct);
        var lessons = await GetOrNullAsync(() => GetLessonsAsync(userId, ct), "lessons", ct);
        var writing = await GetOrNullAsync(() => GetWritingAsync(userId, ct), "writing", ct);
        var tone = await GetOrNullAsync(() => GetToneAsync(userId, ct), "tone", ct);

        return new ProgressOverviewDto(
            day.LocalDate, day.TimeZoneId, streak, today, srs, dailyGoal, vocabulary, lessons, writing, tone, activity);
    }

    /// <summary>Một dòng gộp theo (ngày, loại) trong cửa sổ 90 ngày — nguồn CHUNG cho cả lịch hoạt động và việc hôm nay, MỘT truy vấn duy nhất (không N+1).</summary>
    private sealed record DailyKindRow(DateOnly LocalDate, string Kind, int Quantity);

    private sealed record TodayEventStats(int SrsReviews, int ToneDrillItems, int WritingAttempts, int Quizzes, int LessonsCompleted, int ActivityCount);

    /// <summary>R-PG6 + khối "today" (§5.2.4) — MỘT truy vấn cho cả lịch 90 ngày và tổng hôm nay, gộp trong bộ nhớ (số dòng nhỏ, một người dùng).</summary>
    private async Task<(IReadOnlyList<ProgressActivityDayDto> Activity, TodayEventStats Today)> GetActivityAndTodayEventsAsync(
        Guid userId, DateOnly today, CancellationToken ct)
    {
        var from = today.AddDays(-(ActivityWindowDays - 1));
        var rows = await db.StudyEvents.AsNoTracking()
            .Where(e => e.UserId == userId && e.LocalDate >= from && e.LocalDate <= today)
            .Select(e => new DailyKindRow(e.LocalDate, e.Kind, e.Quantity))
            .ToListAsync(ct);

        var sumByDate = rows.GroupBy(r => r.LocalDate).ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));
        var activity = new List<ProgressActivityDayDto>(ActivityWindowDays);
        for (var i = 0; i < ActivityWindowDays; i++)
        {
            var date = from.AddDays(i);
            activity.Add(new ProgressActivityDayDto(date, sumByDate.GetValueOrDefault(date)));
        }

        var todayRows = rows.Where(r => r.LocalDate == today).ToList();
        // R-PG9/quizzes/lessonsCompleted đếm SỐ LẦN (COUNT) — quantity của quiz_submit là SỐ CÂU
        // (K3), không phải 1 lần nộp; các kind còn lại đếm SUM(quantity) (đúng ngữ nghĩa của chúng).
        var todayStats = new TodayEventStats(
            SrsReviews: todayRows.Where(r => r.Kind == StudyEventKinds.SrsReview).Sum(r => r.Quantity),
            ToneDrillItems: todayRows.Where(r => r.Kind == StudyEventKinds.ToneDrill).Sum(r => r.Quantity),
            WritingAttempts: todayRows.Where(r => r.Kind == StudyEventKinds.Writing).Sum(r => r.Quantity),
            Quizzes: todayRows.Count(r => r.Kind == StudyEventKinds.QuizSubmit),
            LessonsCompleted: todayRows.Count(r => r.Kind == StudyEventKinds.LessonComplete),
            ActivityCount: todayRows.Sum(r => r.Quantity));

        return (activity, todayStats);
    }

    private async Task<ProgressSrsDto> GetSrsAsync(Guid userId, CancellationToken ct)
    {
        var summary = await srsSummaryService.GetAsync(userId, ct);
        return new ProgressSrsDto(
            summary.DueToday, summary.DueNow, summary.NewAvailableToday, summary.NewIntroducedToday,
            summary.ReviewedToday, summary.NextDueAt);
    }

    /// <summary>R-PG5: đạt khi KHÔNG còn thẻ đến hạn VÀ đã học đủ thẻ mới trong hạn mức; mẫu số 0 (không có gì để làm) tự nhiên rơi vào <c>done == total</c> ⇒ frontend hiện thanh đầy.</summary>
    private static ProgressDailyGoalDto BuildDailyGoal(ProgressSrsDto srs)
    {
        var done = srs.ReviewedToday + srs.NewIntroducedToday;
        var total = done + srs.DueToday + srs.NewAvailableToday;
        var achieved = srs.DueToday == 0 && srs.NewAvailableToday == 0;
        return new ProgressDailyGoalDto(done, total, achieved);
    }

    /// <summary>R-PG8 — <c>totalInPath</c> đọc <c>content.words</c> (khác bảng với phần còn lại); <c>introduced</c>/<c>mature</c> đọc MỘT LẦN <c>learning.srs_cards</c> của người dùng rồi đếm trong bộ nhớ (số thẻ một người nhỏ, không cần SQL cửa sổ).</summary>
    private async Task<ProgressVocabularyDto> GetVocabularyAsync(Guid userId, CancellationToken ct)
    {
        var totalInPath = await db.Words.AsNoTracking().CountAsync(w => w.PathOrder != null, ct);

        var cards = await db.SrsCards.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new { c.FirstReviewedAt, c.State, c.Stability })
            .ToListAsync(ct);

        var introduced = cards.Count(c => c.FirstReviewedAt is not null);
        var mature = cards.Count(c => c.State == SrsState.Review && c.Stability >= 21);
        var learning = introduced - mature;

        return new ProgressVocabularyDto(totalInPath, introduced, learning, mature);
    }

    /// <summary>Dùng lại <see cref="LessonQueryService.ListPublishedAsync"/> (R-LS4) — MỘT lời gọi cho cả published/completed/inProgress/next, tránh tính lại logic "bài kế tiếp" ở hai nơi.</summary>
    private async Task<ProgressLessonsDto> GetLessonsAsync(Guid userId, CancellationToken ct)
    {
        var list = await lessonQueryService.ListPublishedAsync(userId, ct);

        var published = list.Items.Count;
        var completed = list.Items.Count(i => i.Progress?.Status == LessonProgressStatuses.Completed);
        var inProgress = list.Items.Count(i => i.Progress?.Status == LessonProgressStatuses.InProgress);

        var next = list.NextLessonSlug is null
            ? null
            : list.Items.Where(i => i.Slug == list.NextLessonSlug)
                .Select(i => new ProgressLessonRefDto(i.Slug, i.Title))
                .FirstOrDefault();

        var lastCompletedItem = list.Items
            .Where(i => i.Progress?.CompletedAt is not null)
            .OrderByDescending(i => i.Progress!.CompletedAt)
            .FirstOrDefault();

        ProgressLastCompletedLessonDto? lastCompleted = null;
        if (lastCompletedItem is not null)
        {
            var unpracticedChars = await CountUnpracticedCharsAsync(userId, lastCompletedItem.Slug, ct);
            lastCompleted = new ProgressLastCompletedLessonDto(
                lastCompletedItem.Slug, lastCompletedItem.Title, lastCompletedItem.Progress!.CompletedAt!.Value, unpracticedChars);
        }

        return new ProgressLessonsDto(published, completed, inProgress, next, lastCompleted);
    }

    /// <summary>R-PG9 mục 4: số chữ của bài (R-W6, bộ <c>lesson:&lt;slug&gt;</c>) CHƯA có dòng <c>character_writing_stats</c> — <see cref="WritingCharacterQueryService.ListAsync"/> trả <c>lastPracticedAt=null</c> đúng cho trường hợp này.</summary>
    private async Task<int> CountUnpracticedCharsAsync(Guid userId, string slug, CancellationToken ct)
    {
        var characters = await writingCharacterQueryService.ListAsync(userId, $"lesson:{slug}", page: 1, pageSize: LessonCharacterPageSize, ct);
        return characters.Items.Count(i => i.LastPracticedAt is null);
    }

    private async Task<ProgressWritingDto> GetWritingAsync(Guid userId, CancellationToken ct)
    {
        var summary = await writingService.GetSummaryAsync(userId, ct);
        return new ProgressWritingDto(summary.PracticedChars, summary.MasteredChars, summary.WeakChars, summary.TotalChars);
    }

    /// <summary><see cref="ToneStatsService"/> tự nó KHÔNG kiểm <see cref="IPinyinCatalog.IsAvailable"/> (chỉ đọc câu trả lời đã lưu trong DB, không đọc học liệu) — kiểm TƯỜNG MINH ở đây để tổng quan tuân R-PG7 giống các endpoint pinyin khác (<c>PinyinController.EnsureCatalogAvailable</c>).</summary>
    private async Task<ProgressToneDto> GetToneAsync(Guid userId, CancellationToken ct)
    {
        if (!pinyinCatalog.IsAvailable)
            throw new ServiceUnavailableException("CONTENT_UNAVAILABLE", "Học liệu pinyin chưa sẵn sàng — báo quản trị viên.");

        var stats = await toneStatsService.GetAsync(userId, ct);
        return new ProgressToneDto(stats.TotalAnswered, stats.Accuracy, stats.RecommendedFocus);
    }

    /// <summary>R-PG7: khối không dùng được (học liệu lỗi) ⇒ <c>null</c> + log Warning, KHÔNG làm sập cả trang tổng quan; lỗi khác (vd lỗi DB thật) vẫn ném để lộ ra 500 bình thường.</summary>
    private async Task<T?> GetOrNullAsync<T>(Func<Task<T>> loader, string blockName, CancellationToken ct) where T : class
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            return await loader();
        }
        catch (ServiceUnavailableException ex)
        {
            logger.LogWarning(ex, "Khối '{Block}' không dùng được ở tổng quan tiến độ — ẩn khối, các khối khác vẫn hiển thị (R-PG7).", blockName);
            return null;
        }
    }
}
