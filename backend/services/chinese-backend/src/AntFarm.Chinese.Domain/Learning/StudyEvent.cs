namespace AntFarm.Chinese.Domain.Learning;

/// <summary>
/// Sổ hoạt động học DÙNG CHUNG mọi loại bài tập của service (§5.1.1) — F5 ghi <c>tone_drill</c>,
/// F7 <c>srs_review</c>, F8 <c>writing</c>, F9 <c>quiz_submit</c>/<c>lesson_complete</c>. Dùng để
/// tính chuỗi ngày học liên tiếp (streak) và thống kê tiến độ ở các feature sau.
/// </summary>
public sealed class StudyEvent
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Kind { get; private set; } = null!;

    /// <summary>Mốc UTC của hoạt động (F5: <c>finishedAt</c> của phiên luyện) — R-T1.</summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>Ngày lịch theo múi giờ người dùng TẠI LÚC GHI (R-T3) — đổi múi giờ sau đó KHÔNG viết lại giá trị này.</summary>
    public DateOnly LocalDate { get; private set; }

    public int Quantity { get; private set; }
    public int? Correct { get; private set; }

    /// <summary>Trỏ tới bản ghi chi tiết (F5: <c>tone_drill_sessions.id</c>) — KHÔNG FK vì bảng này dùng chung nhiều loại nguồn khác nhau.</summary>
    public Guid? RefId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // EF Core cần constructor không tham số.
    private StudyEvent()
    {
    }

    public static StudyEvent Create(
        Guid userId, string kind, DateTime occurredAtUtc, DateOnly localDate, int quantity, int? correct, Guid? refId, DateTime nowUtc)
    {
        if (occurredAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("occurredAtUtc phải có Kind=Utc (Npgsql timestamptz chỉ nhận UTC).", nameof(occurredAtUtc));
        if (quantity < 0)
            throw new ArgumentException("quantity không được âm.", nameof(quantity));
        if (correct is not null && correct > quantity)
            throw new ArgumentException("correct không được lớn hơn quantity.", nameof(correct));

        return new StudyEvent
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Kind = kind,
            OccurredAt = occurredAtUtc,
            LocalDate = localDate,
            Quantity = quantity,
            Correct = correct,
            RefId = refId,
            CreatedAt = nowUtc
        };
    }
}
