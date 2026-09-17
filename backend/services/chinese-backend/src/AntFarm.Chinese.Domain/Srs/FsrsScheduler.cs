namespace AntFarm.Chinese.Domain.Srs;

/// <summary>
/// Cài đặt THUẦN thuật toán FSRS-6, chép đúng hành vi <c>Scheduler.review_card</c> của
/// <c>py-fsrs</c> v6.3.2 (github.com/open-spaced-repetition/py-fsrs, commit
/// <c>9446cb06605c597a063aeee49f7d188d42e34dc2</c>, tệp <c>fsrs/scheduler.py</c>; MIT,
/// Copyright (c) Open Spaced Repetition). Không IO, không DB, không sinh số ngẫu nhiên
/// (fuzz luôn tắt — R7-1); mọi <see cref="DateTime"/> đầu vào/đầu ra đều <c>Kind = Utc</c>.
///
/// Đối chiếu lại bằng cách cài trực tiếp <c>py-fsrs</c> đúng commit ghim (qua <c>uv</c>, Python
/// 3.12) và chạy song song — không chỉ chép số từ hợp đồng — nên các công thức dưới đây bám
/// SÁT TỪNG DÒNG mã nguồn gốc (kể cả thứ tự tính S mới bằng D CŨ rồi mới cập nhật D, và quy tắc
/// "cùng ngày" dùng công thức ngắn hạn cho cả <see cref="SrsState.Review"/>, không riêng
/// Learning/Relearning như một số mô tả rút gọn dễ hiểu lầm).
/// </summary>
public sealed class FsrsScheduler : ISrsScheduler
{
    private readonly FsrsOptions _options;

    /// <summary>DECAY = −w20 (một số ÂM, vd −0,1542) — giữ tên/dấu như biến <c>_DECAY</c> của py-fsrs để dễ đối chiếu công thức.</summary>
    private readonly double _decay;

    /// <summary>FACTOR = 0,9^(1/DECAY) − 1.</summary>
    private readonly double _factor;

    public FsrsScheduler(FsrsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateWeights(options.Weights);

        _options = options;
        _decay = -options.Weights[20];
        _factor = Math.Pow(0.9, 1.0 / _decay) - 1;
    }

    public SrsSchedulingResult Review(SrsMemory card, SrsRating rating, DateTime nowUtc)
    {
        RequireUtc(nowUtc);
        return ReviewCore(card, rating, nowUtc);
    }

    public IReadOnlyDictionary<SrsRating, TimeSpan> Preview(SrsMemory card, DateTime nowUtc)
    {
        RequireUtc(nowUtc);

        var result = new Dictionary<SrsRating, TimeSpan>(4);
        foreach (SrsRating rating in Enum.GetValues<SrsRating>())
            result[rating] = ReviewCore(card, rating, nowUtc).Interval;

        return result;
    }

    public double Retrievability(SrsMemory card, DateTime nowUtc)
    {
        RequireUtc(nowUtc);
        return GetRetrievability(card.Stability, card.LastReviewAt, nowUtc);
    }

    private SrsSchedulingResult ReviewCore(SrsMemory card, SrsRating rating, DateTime nowUtc)
    {
        // Thẻ "New" của AntFarm ≡ Learning/step 0/S=D=null của py-fsrs (§5.2.6).
        var effectiveState = card.State == SrsState.New ? SrsState.Learning : card.State;
        var effectiveStep = card.State == SrsState.New ? 0 : card.Step;

        double stability;
        double difficulty; // Luôn tính từ D CŨ (card.Difficulty) — KHÔNG dùng `stability` mới ở trên.

        if (card.Stability is null || card.Difficulty is null)
        {
            stability = InitialStability(rating);
            difficulty = InitialDifficulty(rating, clamp: true);
        }
        else
        {
            var daysSinceLast = card.LastReviewAt is { } last ? ElapsedDaysFloor(last, nowUtc) : (double?)null;
            var sameDay = daysSinceLast is { } d && d < 1;

            if (sameDay)
            {
                stability = ShortTermStability(card.Stability.Value, rating);
            }
            else
            {
                var retrievability = GetRetrievability(card.Stability, card.LastReviewAt, nowUtc);
                stability = NextStability(card.Difficulty.Value, card.Stability.Value, retrievability, rating);
            }

            difficulty = NextDifficulty(card.Difficulty.Value, rating);
        }

        var (afterState, afterStep, interval) = effectiveState switch
        {
            SrsState.Learning => ResolveStepTransition(_options.LearningSteps, effectiveStep!.Value, rating, stability, SrsState.Learning),
            SrsState.Relearning => ResolveStepTransition(_options.RelearningSteps, effectiveStep!.Value, rating, stability, SrsState.Relearning),
            SrsState.Review => ResolveReviewTransition(rating, stability),
            _ => throw new InvalidOperationException($"Trạng thái thẻ không hợp lệ: {card.State}.")
        };

        var dueAt = nowUtc + interval;
        var after = new SrsMemory(afterState, afterStep, stability, difficulty, dueAt, nowUtc);
        return new SrsSchedulingResult(after, interval);
    }

    /// <summary>Thẻ Review — R7-1: "Quên" (Again) chuyển Relearning (hoặc lập lịch thẳng nếu không cấu hình bước học lại); mọi mức khác ở lại Review với khoảng <c>I(S)</c> mới.</summary>
    private (SrsState State, int? Step, TimeSpan Interval) ResolveReviewTransition(SrsRating rating, double stability)
    {
        if (rating != SrsRating.Again)
            return (SrsState.Review, null, IntervalFromStability(stability));

        if (_options.RelearningSteps.Count == 0)
            return (SrsState.Review, null, IntervalFromStability(stability));

        return (SrsState.Relearning, 0, _options.RelearningSteps[0]);
    }

    /// <summary>
    /// Thẻ Learning/Relearning — máy trạng thái theo bước học (§5.2.6), giống hệt hai nhánh
    /// <c>State.Learning</c>/<c>State.Relearning</c> của py-fsrs (chỉ khác mảng bước truyền vào).
    /// <paramref name="continuingState"/> nhận TƯỜNG MINH từ nơi gọi (review F7.1) — trước đây suy
    /// bằng <c>ReferenceEquals(steps, _options.LearningSteps)</c>, một cách "đoán" mong manh: nếu
    /// sau này <c>FsrsOptions</c> đổi cách khởi tạo (vd dùng chung một mảng rỗng singleton cho cả
    /// hai loại bước) thì so tham chiếu sai lặng lẽ, không lỗi biên dịch, không ném lúc chạy.
    /// </summary>
    private (SrsState State, int? Step, TimeSpan Interval) ResolveStepTransition(
        IReadOnlyList<TimeSpan> steps, int step, SrsRating rating, double stability, SrsState continuingState)
    {
        // Biên "tràn bước": bộ bước rỗng, hoặc thẻ đã đi quá số bước hiện cấu hình (cấu hình đổi
        // giữa vòng đời thẻ) — CHỈ áp dụng cho Hard/Good/Easy, "Again" luôn quay lại bước 0.
        if (steps.Count == 0 || (step >= steps.Count && rating != SrsRating.Again))
            return (SrsState.Review, null, IntervalFromStability(stability));

        switch (rating)
        {
            case SrsRating.Again:
                return (continuingState, 0, steps[0]);

            case SrsRating.Hard:
                var hardInterval = step switch
                {
                    0 when steps.Count == 1 => steps[0] * 1.5,
                    0 => (steps[0] + steps[1]) / 2.0,
                    _ => steps[step]
                };
                return (continuingState, step, hardInterval);

            case SrsRating.Good:
                if (step + 1 == steps.Count)
                    return (SrsState.Review, null, IntervalFromStability(stability));
                return (continuingState, step + 1, steps[step + 1]);

            case SrsRating.Easy:
                return (SrsState.Review, null, IntervalFromStability(stability));

            default:
                throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating không hợp lệ.");
        }
    }

    // ---- Công thức FSRS-6 thuần (đối chiếu 1-1 với fsrs/scheduler.py) --------------------------

    private double InitialStability(SrsRating rating) =>
        Math.Max(_options.Weights[(int)rating - 1], 0.001);

    /// <summary>internal (không phải private) để <c>FsrsGoldenTests</c> kiểm trực tiếp bảng D0 (§5.2.7 V7) mà không phải lách qua máy trạng thái.</summary>
    internal double InitialDifficulty(SrsRating rating, bool clamp)
    {
        var value = _options.Weights[4] - Math.Exp(_options.Weights[5] * ((int)rating - 1)) + 1;
        return clamp ? ClampDifficulty(value) : value;
    }

    private double NextDifficulty(double difficulty, SrsRating rating)
    {
        // Mốc "quy hồi về trung bình" LUÔN dùng D0(Easy) KHÔNG kẹp — kể cả khi rating hiện tại khác Easy.
        var meanReversionTarget = InitialDifficulty(SrsRating.Easy, clamp: false);

        var deltaDifficulty = -(_options.Weights[6] * ((int)rating - 3));
        var damped = difficulty + (10.0 - difficulty) * deltaDifficulty / 9.0;

        var next = _options.Weights[7] * meanReversionTarget + (1 - _options.Weights[7]) * damped;
        return ClampDifficulty(next);
    }

    private double ShortTermStability(double stability, SrsRating rating)
    {
        var increase = Math.Exp(_options.Weights[17] * ((int)rating - 3 + _options.Weights[18])) * Math.Pow(stability, -_options.Weights[19]);
        if (rating != SrsRating.Again)
            increase = Math.Max(increase, 1.0);

        return ClampStability(stability * increase);
    }

    private double NextStability(double difficulty, double stability, double retrievability, SrsRating rating)
    {
        var next = rating == SrsRating.Again
            ? NextForgetStability(difficulty, stability, retrievability)
            : NextRecallStability(difficulty, stability, retrievability, rating);

        return ClampStability(next);
    }

    private double NextForgetStability(double difficulty, double stability, double retrievability)
    {
        var longTerm = _options.Weights[11]
            * Math.Pow(difficulty, -_options.Weights[12])
            * (Math.Pow(stability + 1, _options.Weights[13]) - 1)
            * Math.Exp((1 - retrievability) * _options.Weights[14]);

        var shortTerm = stability / Math.Exp(_options.Weights[17] * _options.Weights[18]);

        return Math.Min(longTerm, shortTerm);
    }

    private double NextRecallStability(double difficulty, double stability, double retrievability, SrsRating rating)
    {
        var hardPenalty = rating == SrsRating.Hard ? _options.Weights[15] : 1;
        var easyBonus = rating == SrsRating.Easy ? _options.Weights[16] : 1;

        return stability * (1
            + Math.Exp(_options.Weights[8])
            * (11 - difficulty)
            * Math.Pow(stability, -_options.Weights[9])
            * (Math.Exp((1 - retrievability) * _options.Weights[10]) - 1)
            * hardPenalty * easyBonus);
    }

    private double GetRetrievability(double? stability, DateTime? lastReviewAt, DateTime nowUtc)
    {
        if (stability is null || lastReviewAt is null)
            return 0;

        var elapsedDays = Math.Max(0, ElapsedDaysFloor(lastReviewAt.Value, nowUtc));
        return Math.Pow(1 + _factor * elapsedDays / stability.Value, _decay);
    }

    private TimeSpan IntervalFromStability(double stability) => TimeSpan.FromDays(NextIntervalDays(stability));

    /// <summary>internal (không phải private) để <c>FsrsGoldenTests</c> kiểm trực tiếp hàm I(S) (§5.2.7 V7) độc lập với máy trạng thái chấm thẻ.</summary>
    internal int NextIntervalDays(double stability)
    {
        var raw = stability / _factor * (Math.Pow(_options.DesiredRetention, 1.0 / _decay) - 1);
        var rounded = Math.Round(raw, MidpointRounding.ToEven); // giống round() của Python (ties-to-even)
        var clamped = Math.Clamp(rounded, 1.0, _options.MaximumInterval); // kẹp TRONG double trước khi ép int, tránh tràn số với S rất lớn
        return (int)clamped;
    }

    private static double ClampDifficulty(double difficulty) => Math.Clamp(difficulty, 1.0, 10.0);

    private static double ClampStability(double stability) => Math.Max(stability, 0.001);

    /// <summary>floor((to − from) / 1 ngày) — CHỦ Ý cho phép âm (không kẹp ở đây, việc kẹp ≥0 dành riêng cho <see cref="GetRetrievability"/> — đúng như <c>days_since_last_review</c> ở py-fsrs không kẹp còn <c>elapsed_days</c> trong <c>get_card_retrievability</c> mới kẹp).</summary>
    private static double ElapsedDaysFloor(DateTime from, DateTime to) => Math.Floor((to - from).TotalDays);

    private static void RequireUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("nowUtc phải có Kind=Utc (Npgsql timestamptz chỉ nhận UTC).", nameof(value));
    }

    private static void ValidateWeights(IReadOnlyList<double> weights)
    {
        if (weights.Count != FsrsOptions.LowerBounds.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weights), weights.Count,
                $"Cần đúng {FsrsOptions.LowerBounds.Count} trọng số FSRS-6, nhận {weights.Count}.");
        }

        for (var i = 0; i < weights.Count; i++)
        {
            if (weights[i] < FsrsOptions.LowerBounds[i] || weights[i] > FsrsOptions.UpperBounds[i])
            {
                throw new ArgumentOutOfRangeException(
                    nameof(weights), weights[i],
                    $"weights[{i}] = {weights[i]} nằm ngoài cận hợp lệ [{FsrsOptions.LowerBounds[i]}, {FsrsOptions.UpperBounds[i]}] của py-fsrs.");
            }
        }
    }
}
