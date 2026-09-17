namespace AntFarm.Chinese.Application.Lessons.Dtos;

/// <summary>Một câu trả lời gửi lên (§6.1) — <c>optionId</c> thô, kiểm khớp lựa chọn ở Application (400 <c>VALIDATION</c> nếu sai định dạng).</summary>
public sealed record SubmitQuizAnswerDto(Guid QuestionId, string OptionId);

/// <summary>
/// <c>POST /api/lessons/{id}/quiz-attempts</c> (§6.1) — <c>clientAttemptId</c> do frontend sinh bằng
/// <c>crypto.randomUUID()</c> (R-LS9). <c>StartedAt</c> khai <see cref="DateTimeOffset"/> (KHÔNG
/// <see cref="DateTime"/>) — review F9: chuỗi ISO-8601 có offset khác "Z" (vd
/// <c>2026-09-20T08:10:00+07:00</c>, đồng hồ máy khách theo giờ địa phương) deserialize qua
/// <see cref="DateTime"/> ra <c>Kind=Local</c> (System.Text.Json giữ offset gốc, không quy về UTC),
/// Npgsql <c>timestamptz</c> chỉ nhận <c>Kind=Utc</c> ⇒ ném lúc <c>SaveChangesAsync</c> (500).
/// <see cref="DateTimeOffset"/> không có vấn đề Kind — Application tự <c>.UtcDateTime</c> khi cần
/// <see cref="DateTime"/> Utc (QuizSubmissionService).
/// </summary>
public sealed record SubmitQuizRequest(Guid ClientAttemptId, DateTimeOffset? StartedAt, IReadOnlyList<SubmitQuizAnswerDto> Answers);

/// <summary>Kết quả MỘT câu sau khi chấm (§6.1) — CHỈ trả trong kết quả nộp/lịch sử, KHÔNG BAO GIỜ ở chi tiết bài trước khi nộp (R-LS10).</summary>
public sealed record QuizResultItemDto(Guid QuestionId, string OptionId, bool Correct, string CorrectOptionId, string Explanation);

/// <summary>Phản hồi nộp quiz (§6.1) — 201 lần đầu, 200 khi phát lại đúng <c>clientAttemptId</c> (R-LS9).</summary>
public sealed record SubmitQuizResponseDto(
    Guid AttemptId, DateTime SubmittedAt, int Total, int Correct, int ScorePercent, bool Passed,
    int PassThresholdPercent, bool FirstCompletion, int SrsCardsAdded,
    IReadOnlyList<QuizResultItemDto> Results, LessonProgressDto Progress);

/// <summary>Một dòng lịch sử lần làm (§6.1, <c>GET /api/lessons/{id}/quiz-attempts</c>) — KHÔNG kèm chi tiết từng câu.</summary>
public sealed record QuizAttemptSummaryDto(Guid AttemptId, DateTime SubmittedAt, int Total, int Correct, int ScorePercent, bool Passed, int? DurationMs);

public sealed record QuizAttemptListResponseDto(IReadOnlyList<QuizAttemptSummaryDto> Items);
