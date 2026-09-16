namespace AntFarm.Chinese.Domain.Pinyin;

/// <summary>
/// Chế độ bài luyện thanh (R5-7). JSON dùng snake_case (<c>listen_tone</c>, <c>tone_pair</c>) qua
/// <c>JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)</c> đã đăng ký chung ở Program.cs;
/// EF Core lưu CHUỖI snake_case (không lưu số) qua <c>HasConversion</c> tường minh trong
/// <c>ToneDrillSessionConfiguration</c> để đọc trực tiếp bằng SQL cũng ra giá trị dễ hiểu.
/// </summary>
public enum ToneDrillMode
{
    /// <summary>Mỗi câu phát MỘT chữ minh hoạ, người học chọn 1 thanh (R5-7).</summary>
    ListenTone,

    /// <summary>Mỗi câu phát HAI chữ minh hoạ liền nhau (âm tiết khác nhau, không cùng thanh 3), người học chọn 2 thanh (R5-7).</summary>
    TonePair
}
