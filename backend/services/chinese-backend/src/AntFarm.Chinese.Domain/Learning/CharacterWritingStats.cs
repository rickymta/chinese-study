namespace AntFarm.Chinese.Domain.Learning;

/// <summary>
/// Số liệu luyện viết TÍCH LUỸ của MỘT người dùng cho MỘT chữ Hán (§5.1.2, migration F8_Writing,
/// khoá chính ghép <c>(UserId, Hanzi)</c>) — cập nhật trong CÙNG transaction với
/// <see cref="WritingAttempt"/> vừa ghi (§5.2.2). <see cref="MasteryStatus"/>/<see cref="IsWeak"/>
/// TÍNH LẠI mỗi lần đọc (R-W5), không lưu cột riêng — tránh lệch giữa cột lưu và dữ liệu gốc.
/// </summary>
public sealed class CharacterWritingStats
{
    public Guid UserId { get; private set; }
    public string Hanzi { get; private set; } = null!;
    public int Attempts { get; private set; }
    public int GuidedAttempts { get; private set; }
    public int RecallAttempts { get; private set; }
    public string LastMode { get; private set; } = null!;
    public short LastMistakes { get; private set; }
    public short LastHints { get; private set; }

    /// <summary><c>NULL</c> khi chưa có lần <c>recall</c> nào — số lỗi NHỎ NHẤT trong các lần tự viết.</summary>
    public short? BestRecallMistakes { get; private set; }

    /// <summary>Số NGÀY LỊCH khác nhau (theo múi giờ người dùng) có ít nhất một lần <c>recall</c> sạch — R-W5.</summary>
    public short CleanRecallDays { get; private set; }

    public DateOnly? LastCleanRecallDate { get; private set; }
    public DateTime FirstPracticedAt { get; private set; }
    public DateTime LastPracticedAt { get; private set; }

    // EF Core cần constructor không tham số.
    private CharacterWritingStats()
    {
    }

    /// <summary>Dòng rỗng trước lần viết ĐẦU TIÊN — Application tạo (INSERT ON CONFLICT DO NOTHING) rồi gọi NGAY <see cref="Apply"/> với lần viết vừa nhận (§5.2.2).</summary>
    public static CharacterWritingStats CreateEmpty(Guid userId, string hanzi, DateTime nowUtc) => new()
    {
        UserId = userId,
        Hanzi = hanzi,
        Attempts = 0,
        GuidedAttempts = 0,
        RecallAttempts = 0,
        // Bị Apply() ghi đè NGAY sau khi tạo — chỉ để cột NOT NULL có giá trị hợp lệ trước đó.
        LastMode = WritingModes.Guided,
        LastMistakes = 0,
        LastHints = 0,
        BestRecallMistakes = null,
        CleanRecallDays = 0,
        LastCleanRecallDate = null,
        FirstPracticedAt = nowUtc,
        LastPracticedAt = nowUtc
    };

    /// <summary>R-W5 — <c>new</c>: chưa viết; <c>mastered</c>: <see cref="CleanRecallDays"/> ≥ 2; còn lại <c>practicing</c>.</summary>
    public string MasteryStatus =>
        Attempts == 0 ? MasteryStatuses.New
        : CleanRecallDays >= 2 ? MasteryStatuses.Mastered
        : MasteryStatuses.Practicing;

    /// <summary>"Cần luyện" (R-W5) — có lần viết, chưa thuộc, và lần gần nhất tệ (≥ 2 lỗi hoặc ≥ 1 gợi ý).</summary>
    public bool IsWeak => Attempts > 0 && MasteryStatus != MasteryStatuses.Mastered && (LastMistakes >= 2 || LastHints >= 1);

    /// <summary>Ghi nhận MỘT lần viết vừa hoàn tất (§5.2.2) — tăng đếm, cập nhật "lần gần nhất", và <see cref="CleanRecallDays"/> nếu lần này sạch VÀ khác ngày lịch với lần sạch gần nhất (R-W5: lần sạch thứ hai TRONG CÙNG NGÀY không tăng).</summary>
    public void Apply(WritingAttempt attempt, DateOnly localDate)
    {
        Attempts++;
        if (attempt.Mode == WritingModes.Guided)
            GuidedAttempts++;
        else
            RecallAttempts++;

        LastMode = attempt.Mode;
        LastMistakes = attempt.TotalMistakes;
        LastHints = attempt.HintsUsed;
        LastPracticedAt = attempt.CompletedAt;

        if (attempt.Mode == WritingModes.Recall)
        {
            BestRecallMistakes = BestRecallMistakes is null
                ? attempt.TotalMistakes
                : Math.Min(BestRecallMistakes.Value, attempt.TotalMistakes);
        }

        if (attempt.IsClean && localDate != LastCleanRecallDate)
        {
            CleanRecallDays++;
            LastCleanRecallDate = localDate;
        }
    }
}
