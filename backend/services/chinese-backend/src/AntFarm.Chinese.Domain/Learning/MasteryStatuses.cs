namespace AntFarm.Chinese.Domain.Learning;

/// <summary>Trạng thái "thuộc chữ" của MỘT người dùng cho MỘT chữ Hán (R-W5, §3.2) — tính từ <see cref="CharacterWritingStats"/>, không lưu cột riêng (tính lại mỗi lần đọc).</summary>
public static class MasteryStatuses
{
    /// <summary>Chưa có lần viết nào.</summary>
    public const string New = "new";

    /// <summary>Có lần viết, chưa đủ điều kiện <see cref="Mastered"/>.</summary>
    public const string Practicing = "practicing";

    /// <summary><c>clean_recall_days &gt;= 2</c> — tự viết sạch (0 lỗi, 0 gợi ý) ở ít nhất 2 ngày lịch KHÁC NHAU theo múi giờ người dùng.</summary>
    public const string Mastered = "mastered";
}
