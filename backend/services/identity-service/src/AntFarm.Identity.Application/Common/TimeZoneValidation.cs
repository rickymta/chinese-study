namespace AntFarm.Identity.Application.Common;

/// <summary>
/// R-T2: hồ sơ chỉ nhận ID múi giờ IANA (vd "Asia/Ho_Chi_Minh") — KHÔNG dùng
/// <c>TimeZoneInfo.FindSystemTimeZoneById</c> làm kiểm hợp lệ vì trên Windows nó CŨNG chấp
/// nhận ID kiểu Windows ("SE Asia Standard Time"), lẫn vào DB rồi thì service khác (đọc IANA
/// để tính "ngày học hôm nay" theo R-T3) sẽ vỡ. <c>TryConvertIanaIdToWindowsId</c> chỉ trả true
/// cho ID IANA thật (đã kiểm: "SE Asia Standard Time" ⇒ false).
/// </summary>
public static class TimeZoneValidation
{
    public static bool IsValidIana(string timeZone) => TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone, out _);
}
