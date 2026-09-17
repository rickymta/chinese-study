namespace AntFarm.Chinese.Domain.Srs;

/// <summary>
/// Một thẻ ôn tập FSRS-6 của một người học cho một từ (§5.1.2, migration F7_Srs). Khoá tự nhiên
/// <c>(UserId, WordId, CardType)</c> — R7-2: một từ = một thẻ <c>hanzi_to_meaning</c>/người học.
/// Bọc <see cref="ISrsScheduler"/> (thuần, không biết DB) bằng <see cref="ToMemory"/>/<see cref="Apply"/>
/// để tầng Application không tự tay lắp/tháo <see cref="SrsMemory"/> ở nhiều nơi.
/// </summary>
public sealed class SrsCard
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid WordId { get; private set; }
    public string CardType { get; private set; } = SrsCardTypes.HanziToMeaning;
    public SrsState State { get; private set; }
    public int? Step { get; private set; }
    public DateTime DueAt { get; private set; }
    public double? Stability { get; private set; }
    public double? Difficulty { get; private set; }
    public int Reps { get; private set; }
    public int Lapses { get; private set; }
    public DateTime? LastReviewAt { get; private set; }
    public DateTime? FirstReviewedAt { get; private set; }
    public DateOnly? FirstReviewedLocalDate { get; private set; }
    public bool IsSuspended { get; private set; }
    public string Source { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core cần constructor không tham số.
    private SrsCard()
    {
    }

    /// <summary>Thẻ mới toanh (R7-2) — <c>due_at = created_at</c> (§5.1.2, chưa từng ôn nên "đến hạn" ngay từ đầu để lọt vào hàng đợi thẻ mới).</summary>
    public static SrsCard CreateNew(Guid userId, Guid wordId, string source, DateTime nowUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        WordId = wordId,
        CardType = SrsCardTypes.HanziToMeaning,
        State = SrsState.New,
        Step = null,
        DueAt = nowUtc,
        Stability = null,
        Difficulty = null,
        Reps = 0,
        Lapses = 0,
        IsSuspended = false,
        Source = source,
        CreatedAt = nowUtc,
        UpdatedAt = nowUtc
    };

    /// <summary>"Vững" (R7-13) — đang ở Review với độ ổn định đủ lớn để không cần ôn dồn dập nữa.</summary>
    public bool IsMature => State == SrsState.Review && Stability is >= 21;

    public SrsMemory ToMemory() => new(State, Step, Stability, Difficulty, DueAt, LastReviewAt);

    /// <summary>
    /// Ghi nhận một lượt chấm (§5.2.8 bước 7) — <paramref name="rating"/> chỉ dùng để quyết định
    /// <see cref="Lapses"/> (R7-11: chỉ tăng khi thẻ ĐANG Ở REVIEW mà bị chấm Quên — máy trạng thái
    /// đã đổi <see cref="State"/> nên phải chốt "trước" TRƯỚC khi gán trạng thái mới).
    /// </summary>
    public void Apply(SrsSchedulingResult result, SrsRating rating, DateTime nowUtc, DateOnly localDate)
    {
        var wasReview = State == SrsState.Review;

        State = result.After.State;
        Step = result.After.Step;
        Stability = result.After.Stability;
        Difficulty = result.After.Difficulty;
        DueAt = result.After.DueAt;
        LastReviewAt = nowUtc;

        Reps++;
        if (wasReview && rating == SrsRating.Again)
            Lapses++;

        // Chỉ đặt lần ĐẦU TIÊN (R7-5: newIntroducedToday đếm theo mốc này) — các lượt ôn lại sau
        // của CÙNG thẻ trong các ngày khác không được ghi đè giá trị đã chốt.
        if (FirstReviewedAt is null)
        {
            FirstReviewedAt = nowUtc;
            FirstReviewedLocalDate = localDate;
        }

        UpdatedAt = nowUtc;
    }

    public void SetSuspended(bool suspended, DateTime nowUtc)
    {
        IsSuspended = suspended;
        UpdatedAt = nowUtc;
    }
}
