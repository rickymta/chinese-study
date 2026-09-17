namespace AntFarm.Chinese.Domain.Srs;

/// <summary>
/// Một dòng lịch sử MỘT LƯỢT CHẤM thẻ (§5.1.2, migration F7_Srs) — idempotent theo
/// <see cref="ClientReviewId"/> (R7-8). Lưu thêm <c>*_after</c>/<c>due_after</c>/<c>local_date</c>
/// so với hợp đồng gốc (đã ghi "Lệch hợp đồng gốc" ở §5.1.2) để F11 tính thống kê theo ngày không
/// phải quy đổi lại múi giờ lúc ĐỌC (chỉ múi giờ LÚC GHI mới đúng, R-T3).
/// </summary>
public sealed class SrsReviewLog
{
    public Guid Id { get; private set; }
    public Guid ClientReviewId { get; private set; }
    public Guid CardId { get; private set; }

    /// <summary>Phi chuẩn hoá, KHÔNG FK (§5.1.2) — thẻ đã CASCADE theo user, thêm FK trực tiếp ở đây tạo hai đường cascade cùng gốc access.users.</summary>
    public Guid UserId { get; private set; }

    public SrsRating Rating { get; private set; }
    public DateTime ReviewedAt { get; private set; }

    /// <summary>Ngày học theo múi giờ người dùng TẠI LÚC GHI (R-T3, giống <c>StudyEvent.LocalDate</c>).</summary>
    public DateOnly LocalDate { get; private set; }

    public SrsState StateBefore { get; private set; }
    public int? StepBefore { get; private set; }
    public double? StabilityBefore { get; private set; }
    public double? DifficultyBefore { get; private set; }
    public SrsState StateAfter { get; private set; }
    public double StabilityAfter { get; private set; }
    public double DifficultyAfter { get; private set; }
    public DateTime DueAfter { get; private set; }

    /// <summary>(reviewed_at − last_review_at) theo ngày thực — 0 lượt đầu tiên (§5.2.8 bước 8).</summary>
    public double ElapsedDays { get; private set; }

    /// <summary>(due_after − reviewed_at) theo ngày thực — bằng <see cref="SrsSchedulingResult.Interval"/> tính theo ngày.</summary>
    public double ScheduledDays { get; private set; }

    public int? DurationMs { get; private set; }

    // EF Core cần constructor không tham số.
    private SrsReviewLog()
    {
    }

    public static SrsReviewLog Create(
        Guid clientReviewId, Guid cardId, Guid userId, SrsRating rating, DateTime reviewedAtUtc, DateOnly localDate,
        SrsMemory before, SrsMemory after, double elapsedDays, double scheduledDays, int? durationMs)
    {
        if (after.Stability is null || after.Difficulty is null)
            throw new ArgumentException("Trạng thái SAU khi chấm phải có Stability/Difficulty (thẻ đã ôn ít nhất một lần).", nameof(after));

        return new SrsReviewLog
        {
            Id = Guid.CreateVersion7(),
            ClientReviewId = clientReviewId,
            CardId = cardId,
            UserId = userId,
            Rating = rating,
            ReviewedAt = reviewedAtUtc,
            LocalDate = localDate,
            StateBefore = before.State,
            StepBefore = before.Step,
            StabilityBefore = before.Stability,
            DifficultyBefore = before.Difficulty,
            StateAfter = after.State,
            StabilityAfter = after.Stability.Value,
            DifficultyAfter = after.Difficulty.Value,
            DueAfter = after.DueAt,
            ElapsedDays = elapsedDays,
            ScheduledDays = scheduledDays,
            DurationMs = durationMs
        };
    }
}
