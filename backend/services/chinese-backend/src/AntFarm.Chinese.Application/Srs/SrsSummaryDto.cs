namespace AntFarm.Chinese.Application.Srs;

/// <summary>GET /api/srs/summary (§6.2) — cũng lồng vào phản hồi queue/review (R7-4..R7-6, R7-13).</summary>
public sealed record SrsSummaryDto(
    DateOnly LocalDate, string TimeZone,
    int DueToday, int DueNow,
    int ReviewedToday, int ReviewsDoneToday, int ReviewLimitRemaining, short DailyReviewLimit,
    int NewIntroducedToday, int NewAvailableToday, short DailyNewCards,
    int TotalCards, int MatureCards,
    DateTime? NextDueAt);
