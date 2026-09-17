using System.Text.Json;

namespace AntFarm.Chinese.Application.Lessons.Dtos;

/// <summary>Tiến độ của người gọi trên một bài (§6.1) — VẮNG trong JSON khi chưa bắt đầu (cấu hình <c>WhenWritingNull</c>, Program.cs).</summary>
public sealed record LessonProgressDto(
    string Status, short? BestScorePercent, int AttemptsCount, DateTime StartedAt, DateTime? CompletedAt, DateTime? LastAttemptAt);

/// <summary>Một dòng <c>GET /api/lessons</c> (§6.1).</summary>
public sealed record LessonListItemDto(
    Guid Id, string Slug, string Title, string Topic, int OrderIndex, string Summary, short EstimatedMinutes,
    int WordCount, int QuestionCount, string ReviewStatus, LessonProgressDto? Progress);

public sealed record LessonListResponseDto(IReadOnlyList<LessonListItemDto> Items, string? NextLessonSlug);

public sealed record LessonGlossaryItemDto(string Hanzi, string Pinyin, string Vi);

/// <summary><see cref="Payload"/> là <see cref="JsonElement"/> — hình dạng tuỳ <c>Type</c>, xem <c>Domain.Lessons.Payloads</c>.</summary>
public sealed record LessonBlockDto(Guid Id, string Type, JsonElement Payload);

/// <summary>Một từ của bài trong chi tiết bài học (§6.1) — <see cref="InSrs"/> = người gọi ĐÃ có thẻ SRS cho từ này (bất kỳ nguồn).</summary>
public sealed record LessonWordDto(
    Guid Id, string Simplified, string? Traditional, string Pinyin, string? HanViet,
    IReadOnlyList<string> MeaningsVi, string MeaningViStatus, bool InSrs);

/// <summary>Một lựa chọn quiz AN TOÀN cho học viên (KHÔNG có thông tin đúng/sai) — R-LS10.</summary>
public sealed record LessonQuizOptionDto(string Id, string Text, string Lang);

/// <summary>Một câu quiz cho học viên TRƯỚC KHI NỘP (§6.1) — KHÔNG có <c>correctOptionId</c>/<c>explanation</c>/<c>key</c> (R-LS10).</summary>
public sealed record LessonQuizQuestionDto(
    Guid Id, string Type, string Prompt, string PromptLang, string? PromptPinyin, string? AudioText,
    IReadOnlyList<LessonQuizOptionDto> Options);

/// <summary><c>GET /api/lessons/{slug}</c> (§6.1) — chỉ trả bài <c>published</c> (404 nếu không).</summary>
public sealed record LessonDetailDto(
    Guid Id, string Slug, string Title, string Topic, int OrderIndex, string Summary,
    IReadOnlyList<string> Objectives, short EstimatedMinutes, string ReviewStatus,
    IReadOnlyList<LessonGlossaryItemDto> Glossary, IReadOnlyList<LessonBlockDto> Blocks,
    IReadOnlyList<LessonWordDto> Words, IReadOnlyList<LessonQuizQuestionDto> Quiz, LessonProgressDto? Progress);
