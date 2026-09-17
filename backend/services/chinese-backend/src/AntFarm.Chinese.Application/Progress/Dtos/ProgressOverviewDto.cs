using System.Text.Json.Serialization;

namespace AntFarm.Chinese.Application.Progress.Dtos;

/// <summary>Chuỗi ngày học (§6.4, R-PG3) — luôn có mặt trong <see cref="ProgressOverviewDto"/>.</summary>
public sealed record ProgressStreakDto(int Current, int Longest, bool StudiedToday);

/// <summary>Việc đã làm HÔM NAY (§6.4) — luôn có mặt (kể cả toàn số 0 với người mới, R-PG7).</summary>
public sealed record ProgressTodayDto(
    int SrsReviews, int NewCards, int ToneDrillItems, int WritingAttempts,
    int Quizzes, int LessonsCompleted, int ActivityCount);

/// <summary>Khối SRS rút gọn cho tổng quan (§6.4, K13) — vắng khi hàng đợi SRS không dùng được (R-PG7).</summary>
public sealed record ProgressSrsDto(
    int DueToday, int DueNow, int NewAvailableToday, int NewIntroducedToday, int ReviewedToday, DateTime? NextDueAt);

/// <summary>Mục tiêu ngày (§6.4, R-PG5) — hiển thị RIÊNG, KHÔNG ảnh hưởng streak; vắng khi khối <c>srs</c> vắng.</summary>
public sealed record ProgressDailyGoalDto(int Done, int Total, bool Achieved);

/// <summary>Tiến độ từ vựng theo lộ trình HSK1 (§6.4, R-PG8).</summary>
public sealed record ProgressVocabularyDto(int TotalInPath, int Introduced, int Learning, int Mature);

/// <summary>Bài học kế tiếp / bài vừa hoàn thành gần nhất (§6.4) — chỉ cần slug + tiêu đề để điều hướng.</summary>
public sealed record ProgressLessonRefDto(string Slug, string Title);

/// <summary>Bài hoàn thành gần nhất kèm số chữ CHƯA từng viết của bài đó (§6.4, R-PG9 mục 4).</summary>
public sealed record ProgressLastCompletedLessonDto(string Slug, string Title, DateTime CompletedAt, int UnpracticedChars);

/// <summary>Tiến độ bài học (§6.4) — <see cref="Next"/>/<see cref="LastCompleted"/> vắng khi không có (mọi bài chưa xong / chưa hoàn thành bài nào).</summary>
public sealed record ProgressLessonsDto(
    int Published, int Completed, int InProgress, ProgressLessonRefDto? Next, ProgressLastCompletedLessonDto? LastCompleted);

/// <summary>Tiến độ luyện viết (§6.4) — rút gọn từ <c>WritingSummaryDto</c> (F8), bỏ <c>attemptsToday</c> (đã có ở <see cref="ProgressTodayDto.WritingAttempts"/>).</summary>
public sealed record ProgressWritingDto(int PracticedChars, int MasteredChars, int WeakChars, int TotalChars);

/// <summary>Tiến độ luyện thanh (§6.4) — rút gọn từ <c>ToneStatsResponse</c> (F5). <see cref="Accuracy"/> giữ khoá cả khi <c>null</c> (chưa có dữ liệu) — cùng quy ước <c>ToneStatDto</c>.</summary>
public sealed record ProgressToneDto(
    int TotalAnswered,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] double? Accuracy,
    IReadOnlyList<int> RecommendedFocus);

/// <summary>Một ngày trong lịch hoạt động 90 ngày (§6.4, R-PG6).</summary>
public sealed record ProgressActivityDayDto(DateOnly Date, int Count);

/// <summary>
/// <c>GET /api/progress/overview</c> (§6.4) — trang chủ. <see cref="LocalDate"/>/<see cref="TimeZone"/>/
/// <see cref="Streak"/>/<see cref="Today"/>/<see cref="Activity"/> LUÔN có mặt; các khối còn lại
/// VẮNG (bỏ khỏi JSON, cấu hình <c>WhenWritingNull</c> toàn cục) khi nguồn dữ liệu không dùng được
/// (R-PG7) — frontend coi vắng = ẩn khối, không phải lỗi.
/// </summary>
public sealed record ProgressOverviewDto(
    DateOnly LocalDate, string TimeZone,
    ProgressStreakDto Streak, ProgressTodayDto Today,
    ProgressSrsDto? Srs, ProgressDailyGoalDto? DailyGoal,
    ProgressVocabularyDto? Vocabulary, ProgressLessonsDto? Lessons,
    ProgressWritingDto? Writing, ProgressToneDto? Tone,
    IReadOnlyList<ProgressActivityDayDto> Activity);
