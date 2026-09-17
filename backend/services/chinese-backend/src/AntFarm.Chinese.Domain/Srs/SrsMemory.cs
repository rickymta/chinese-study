namespace AntFarm.Chinese.Domain.Srs;

/// <summary>
/// Trạng thái bộ nhớ FSRS của một thẻ tại một thời điểm — đầu vào/đầu ra THUẦN của
/// <see cref="ISrsScheduler"/>, tách khỏi entity <c>SrsCard</c> (tầng Infrastructure/DB, ngoài
/// phạm vi tệp này) theo đúng nguyên tắc DDD (Domain scheduler không biết gì về EF/DB).
/// Mọi <see cref="DateTime"/> PHẢI có <c>Kind = Utc</c>.
/// </summary>
/// <param name="State">Trạng thái hiện tại của thẻ.</param>
/// <param name="Step">Chỉ số bước học/học lại — <c>null</c> khi <see cref="State"/> là <see cref="SrsState.Review"/>.</param>
/// <param name="Stability">Độ ổn định (ngày) — <c>null</c> khi thẻ chưa ôn lần nào (<see cref="SrsState.New"/>).</param>
/// <param name="Difficulty">Độ khó (1..10) — <c>null</c> khi thẻ chưa ôn lần nào.</param>
/// <param name="DueAt">Mốc đến hạn hiện tại — KHÔNG được thuật toán đọc, chỉ mang theo cho tiện dùng ở tầng gọi.</param>
/// <param name="LastReviewAt">Mốc chấm gần nhất — <c>null</c> khi thẻ chưa ôn lần nào.</param>
public readonly record struct SrsMemory(
    SrsState State,
    int? Step,
    double? Stability,
    double? Difficulty,
    DateTime DueAt,
    DateTime? LastReviewAt);

/// <summary>Kết quả một lượt lập lịch: trạng thái bộ nhớ SAU khi chấm + khoảng thời gian tới lượt ôn kế (dùng để tính <c>due_at = now + Interval</c> ở tầng gọi).</summary>
public readonly record struct SrsSchedulingResult(SrsMemory After, TimeSpan Interval);
