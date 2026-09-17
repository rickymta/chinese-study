namespace AntFarm.Chinese.Domain.Srs;

/// <summary>Bộ lập lịch SRS thuần — không IO, không DB, không random (fuzz luôn tắt, R7-1).</summary>
public interface ISrsScheduler
{
    /// <summary>Chấm một lượt và trả về trạng thái bộ nhớ mới + khoảng ôn tới lượt kế.</summary>
    /// <exception cref="ArgumentException"><paramref name="nowUtc"/> không có <c>Kind = Utc</c>.</exception>
    SrsSchedulingResult Review(SrsMemory card, SrsRating rating, DateTime nowUtc);

    /// <summary>Xem trước khoảng ôn của cả 4 mức chấm — KHÔNG làm đổi <paramref name="card"/> (dùng hiển thị 4 nút ở màn ôn tập, §5.2.9 V8).</summary>
    /// <exception cref="ArgumentException"><paramref name="nowUtc"/> không có <c>Kind = Utc</c>.</exception>
    IReadOnlyDictionary<SrsRating, TimeSpan> Preview(SrsMemory card, DateTime nowUtc);

    /// <summary>Khả năng nhớ ước tính tại <paramref name="nowUtc"/> — 0 khi thẻ chưa ôn lần nào (§5.2.6).</summary>
    /// <exception cref="ArgumentException"><paramref name="nowUtc"/> không có <c>Kind = Utc</c>.</exception>
    double Retrievability(SrsMemory card, DateTime nowUtc);
}
