namespace AntFarm.Chinese.Domain.Learning;

/// <summary>
/// Một LẦN VIẾT hoàn tất (§5.1.2, migration F8_Writing) — một lượt <c>quiz()</c> của hanzi-writer
/// chạy tới <c>onComplete</c> (R-W3); bỏ giữa chừng không ghi. Idempotent theo
/// <see cref="ClientAttemptId"/> (R-W8, cùng kỹ thuật <c>QuizAttempt</c> của F9).
/// </summary>
public sealed class WritingAttempt
{
    public Guid Id { get; private set; }
    public Guid ClientAttemptId { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Khoá thay thế trỏ tới <c>content.characters.hanzi</c> (UNIQUE index, F6) — KHÔNG dùng <c>character_id</c> vì bảng này chỉ cần biết CHỮ, không cần các cột khác của Character.</summary>
    public string Hanzi { get; private set; } = null!;

    public string Mode { get; private set; } = null!;
    public short TotalStrokes { get; private set; }
    public short TotalMistakes { get; private set; }
    public short HintsUsed { get; private set; }
    public int? DurationMs { get; private set; }

    /// <summary><c>mode=recall AND totalMistakes=0 AND hintsUsed=0</c> (R-W2/R-W5) — bước "Tô theo" (guided) KHÔNG BAO GIỜ sạch dù không lỗi/gợi ý (chỉ tự viết mới chứng minh đã thuộc).</summary>
    public bool IsClean { get; private set; }

    /// <summary>Giờ SERVER lúc nhận (không phải đồng hồ máy khách) — R-T1 cùng quy ước StudyEvent.</summary>
    public DateTime CompletedAt { get; private set; }

    /// <summary>Ngày lịch theo múi giờ người dùng TẠI LÚC GHI (R-T3, dùng cho R-W5 "ngày lịch khác nhau").</summary>
    public DateOnly LocalDate { get; private set; }

    // EF Core cần constructor không tham số.
    private WritingAttempt()
    {
    }

    public static WritingAttempt Create(
        Guid clientAttemptId, Guid userId, string hanzi, string mode,
        int totalStrokes, int totalMistakes, int hintsUsed, int? durationMs,
        DateTime completedAtUtc, DateOnly localDate)
    {
        if (completedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("completedAtUtc phải có Kind=Utc (Npgsql timestamptz chỉ nhận UTC).", nameof(completedAtUtc));
        if (!WritingModes.IsKnown(mode))
            throw new ArgumentException($"mode '{mode}' không hợp lệ.", nameof(mode));

        var isClean = mode == WritingModes.Recall && totalMistakes == 0 && hintsUsed == 0;

        return new WritingAttempt
        {
            Id = Guid.CreateVersion7(),
            ClientAttemptId = clientAttemptId,
            UserId = userId,
            Hanzi = hanzi,
            Mode = mode,
            TotalStrokes = (short)totalStrokes,
            TotalMistakes = (short)totalMistakes,
            HintsUsed = (short)hintsUsed,
            DurationMs = durationMs,
            IsClean = isClean,
            CompletedAt = completedAtUtc,
            LocalDate = localDate
        };
    }
}
