namespace AntFarm.Chinese.Domain.Srs;

/// <summary>
/// Tham số cấu hình một phiên bản <see cref="FsrsScheduler"/> (§5.2.6). Trọng số mặc định + cận
/// trên/dưới CHÉP NGUYÊN từ <c>py-fsrs</c> v6.3.2 (github.com/open-spaced-repetition/py-fsrs,
/// commit <c>9446cb06605c597a063aeee49f7d188d42e34dc2</c>, tệp <c>fsrs/scheduler.py</c>,
/// hằng <c>DEFAULT_PARAMETERS</c>/<c>LOWER_BOUNDS_PARAMETERS</c>/<c>UPPER_BOUNDS_PARAMETERS</c>;
/// MIT, Copyright (c) Open Spaced Repetition) — đã đối chiếu lại bằng cách chạy trực tiếp thư
/// viện Python đúng commit, không chỉ chép tay từ hợp đồng.
/// </summary>
/// <param name="Weights">21 trọng số <c>w0..w20</c>, đúng thứ tự.</param>
/// <param name="DesiredRetention">Độ nhớ mục tiêu (0,80–0,97; ràng buộc thực hiện ở tầng Application/DB — Domain không tự kẹp).</param>
/// <param name="LearningSteps">Các bước học (thẻ <c>New</c>/<c>Learning</c>), mặc định <c>[1 phút, 10 phút]</c>.</param>
/// <param name="RelearningSteps">Các bước học lại (thẻ <c>Relearning</c>, sau khi "Quên" một thẻ <c>Review</c>), mặc định <c>[10 phút]</c>.</param>
/// <param name="MaximumInterval">Khoảng ôn tối đa tính bằng ngày, mặc định 36500 (100 năm) — đúng hằng số của py-fsrs.</param>
public sealed record FsrsOptions(
    IReadOnlyList<double> Weights,
    double DesiredRetention,
    IReadOnlyList<TimeSpan> LearningSteps,
    IReadOnlyList<TimeSpan> RelearningSteps,
    int MaximumInterval)
{
    /// <summary>21 trọng số mặc định của FSRS-6 (py-fsrs <c>DEFAULT_PARAMETERS</c>).</summary>
    public static readonly IReadOnlyList<double> DefaultWeights =
    [
        0.212, 1.2931, 2.3065, 8.2956, 6.4133, 0.8334, 3.0194, 0.001, 1.8722, 0.1666,
        0.796, 1.4835, 0.0614, 0.2629, 1.6483, 0.6014, 1.8729, 0.5425, 0.0912, 0.0658,
        0.1542
    ];

    /// <summary>Cận dưới hợp lệ của từng trọng số (py-fsrs <c>LOWER_BOUNDS_PARAMETERS</c>).</summary>
    public static readonly IReadOnlyList<double> LowerBounds =
    [
        0.001, 0.001, 0.001, 0.001, 1.0, 0.001, 0.001, 0.001, 0.0, 0.0,
        0.001, 0.001, 0.001, 0.001, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0,
        0.1
    ];

    /// <summary>Cận trên hợp lệ của từng trọng số (py-fsrs <c>UPPER_BOUNDS_PARAMETERS</c>).</summary>
    public static readonly IReadOnlyList<double> UpperBounds =
    [
        100.0, 100.0, 100.0, 100.0, 10.0, 4.0, 4.0, 0.75, 4.5, 0.8,
        3.5, 5.0, 0.25, 0.9, 4.0, 1.0, 6.0, 2.0, 2.0, 0.8,
        0.8
    ];

    /// <summary>Cấu hình mặc định của AntFarm: 21 trọng số mặc định, tắt fuzz (R7-1), bước học/học lại và khoảng tối đa như py-fsrs mặc định.</summary>
    public static FsrsOptions Default(double desiredRetention = 0.9) => new(
        DefaultWeights,
        desiredRetention,
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10)],
        [TimeSpan.FromMinutes(10)],
        36500);
}
