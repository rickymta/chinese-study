namespace AntFarm.Chinese.Application.Writing.Dtos;

/// <summary><c>POST /api/writing/attempts</c> (§6.2) — một LẦN VIẾT hoàn tất (R-W3), idempotent theo <see cref="ClientAttemptId"/> (R-W8).</summary>
public sealed record RecordWritingAttemptRequest(
    Guid ClientAttemptId, string Hanzi, string Mode, int TotalStrokes, int TotalMistakes, int HintsUsed, int? DurationMs);

/// <summary>Số liệu luyện viết tích luỹ của một chữ (§6.2) — dùng lại ở cả kết quả ghi lần viết, chi tiết chữ, và danh sách chữ theo bộ.</summary>
public sealed record CharacterWritingStatsDto(
    string Hanzi, int Attempts, int GuidedAttempts, int RecallAttempts,
    short LastMistakes, short LastHints, short? BestRecallMistakes, short CleanRecallDays,
    string MasteryStatus, bool IsWeak, DateTime FirstPracticedAt, DateTime LastPracticedAt);

/// <summary>Kết quả <c>POST /api/writing/attempts</c> (§6.2) — <see cref="BecameMastered"/> = lần viết NÀY vừa làm chữ chuyển sang <c>mastered</c> (chưa mastered trước đó).</summary>
public sealed record RecordWritingAttemptResponseDto(
    Guid AttemptId, DateTime CompletedAt, bool IsClean, CharacterWritingStatsDto Stats, bool BecameMastered);

/// <summary>Một dòng <c>GET /api/writing/characters</c> (§6.2) — <see cref="LastMistakes"/>/<see cref="LastPracticedAt"/> vắng khi <c>masteryStatus=new</c>.</summary>
public sealed record WritingCharacterListItemDto(
    string Hanzi, IReadOnlyList<string> PinyinReadings, IReadOnlyList<string> HanViet, short? StrokeCount,
    string MasteryStatus, short? LastMistakes, DateTime? LastPracticedAt);

public sealed record WritingCharacterListResponseDto(
    string Set, IReadOnlyList<WritingCharacterListItemDto> Items, int Page, int PageSize, int TotalCount);

/// <summary>Một từ chứa chữ, dùng trong chi tiết chữ (§6.2, tối đa 5, sắp theo <c>path_order</c>).</summary>
public sealed record WritingCharacterWordDto(
    Guid Id, string Simplified, string Pinyin, IReadOnlyList<string> MeaningsVi, string MeaningViStatus, short? Hsk3Level);

/// <summary><c>GET /api/writing/characters/{hanzi}</c> (§6.2) — <see cref="Stats"/> vắng khi chưa từng viết chữ này.</summary>
public sealed record WritingCharacterDetailDto(
    string Hanzi, IReadOnlyList<string> TraditionalVariants, IReadOnlyList<string> PinyinReadings, IReadOnlyList<string> HanViet,
    short? StrokeCount, string? Radical, IReadOnlyList<WritingCharacterWordDto> Words, CharacterWritingStatsDto? Stats);

/// <summary><c>GET /api/writing/summary</c> (§6.2).</summary>
public sealed record WritingSummaryDto(int PracticedChars, int MasteredChars, int WeakChars, int AttemptsToday, int TotalChars);
