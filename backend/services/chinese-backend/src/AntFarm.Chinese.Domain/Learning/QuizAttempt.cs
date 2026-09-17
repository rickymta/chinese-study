namespace AntFarm.Chinese.Domain.Learning;

/// <summary>
/// Một lần nộp quiz đã chấm (§5.1.1, migration F9_Lessons) — idempotent theo
/// <see cref="ClientAttemptId"/> (R-LS9). <see cref="Answers"/> lưu ẢNH CHỤP đầy đủ (câu hỏi, lựa
/// chọn, đáp án đúng, đúng/sai — R-LS11) để lịch sử vẫn đọc được dù admin sửa/xoá câu hỏi sau đó.
/// </summary>
public sealed class QuizAttempt
{
    public Guid Id { get; private set; }
    public Guid ClientAttemptId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid LessonId { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public int? DurationMs { get; private set; }
    public short Total { get; private set; }
    public short Correct { get; private set; }
    public short ScorePercent { get; private set; }
    public bool Passed { get; private set; }

    /// <summary>jsonb — ảnh chụp R-LS11 (mảng, xem định dạng ở §5.1.1 hợp đồng F8-F11).</summary>
    public string Answers { get; private set; } = null!;

    // EF Core cần constructor không tham số.
    private QuizAttempt()
    {
    }

    public static QuizAttempt Create(
        Guid clientAttemptId, Guid userId, Guid lessonId, DateTime? startedAt, DateTime submittedAt,
        int? durationMs, QuizGrade grade, string answersJson) => new()
    {
        Id = Guid.CreateVersion7(),
        ClientAttemptId = clientAttemptId,
        UserId = userId,
        LessonId = lessonId,
        StartedAt = startedAt,
        SubmittedAt = submittedAt,
        DurationMs = durationMs,
        Total = (short)grade.Total,
        Correct = (short)grade.Correct,
        ScorePercent = (short)grade.ScorePercent,
        Passed = grade.Passed,
        Answers = answersJson
    };
}
