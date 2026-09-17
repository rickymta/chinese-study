namespace AntFarm.Chinese.Domain.Learning;

/// <summary>
/// Tiến độ của MỘT người học trên MỘT bài (§5.1.1, migration F9_Lessons, khoá chính ghép
/// <c>(UserId, LessonId)</c>) — không khoá tuần tự (R-LS4): mọi bài <c>published</c> đều mở, đây chỉ
/// ghi lại người học đã bắt đầu/hoàn thành tới đâu.
/// </summary>
public sealed class LessonProgress
{
    public Guid UserId { get; private set; }
    public Guid LessonId { get; private set; }
    public string Status { get; private set; } = LessonProgressStatuses.InProgress;
    public DateTime StartedAt { get; private set; }

    /// <summary>Lần ĐẠT (≥ 80%) đầu tiên — không đổi ở các lần đạt sau (R-LS5).</summary>
    public DateTime? CompletedAt { get; private set; }

    public short? BestScorePercent { get; private set; }
    public int AttemptsCount { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core cần constructor không tham số.
    private LessonProgress()
    {
    }

    /// <summary>R-LS12: tạo mới khi học viên "bắt đầu bài" lần đầu — idempotent ở tầng Application (kiểm tồn tại trước khi gọi).</summary>
    public static LessonProgress Start(Guid userId, Guid lessonId, DateTime startedAtUtc, DateTime nowUtc) => new()
    {
        UserId = userId,
        LessonId = lessonId,
        Status = LessonProgressStatuses.InProgress,
        StartedAt = startedAtUtc,
        AttemptsCount = 0,
        UpdatedAt = nowUtc
    };

    /// <summary>
    /// Ghi nhận một lần nộp quiz đã chấm (R-LS5) — tăng <see cref="AttemptsCount"/>, cập nhật
    /// <see cref="BestScorePercent"/> = max, <see cref="LastAttemptAt"/>. Lần ĐẠT ĐẦU TIÊN (chưa
    /// từng <c>completed</c>) ⇒ chuyển <see cref="Status"/>, đặt <see cref="CompletedAt"/> =
    /// <paramref name="nowUtc"/>, trả <c>true</c> để Application thêm thẻ SRS + ghi
    /// <c>lesson_complete</c> (K12) — trượt SAU KHI đã đạt KHÔNG hạ <see cref="Status"/> trở lại.
    /// </summary>
    public bool ApplyAttempt(int scorePercent, bool passed, DateTime nowUtc)
    {
        AttemptsCount++;
        LastAttemptAt = nowUtc;
        BestScorePercent = BestScorePercent is null
            ? (short)scorePercent
            : Math.Max(BestScorePercent.Value, (short)scorePercent);
        UpdatedAt = nowUtc;

        if (passed && Status != LessonProgressStatuses.Completed)
        {
            Status = LessonProgressStatuses.Completed;
            CompletedAt = nowUtc;
            return true;
        }

        return false;
    }
}
