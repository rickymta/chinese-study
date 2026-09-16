namespace AntFarm.Chinese.Domain.Common;

/// <summary>
/// Chuẩn hoá ID múi giờ IANA lấy từ claim "zoneinfo"/hồ sơ người dùng (R-T2). Domain thuần —
/// chỉ dùng <see cref="TimeZoneInfo"/> (BCL), không I/O. Trình duyệt (đặc biệt Chrome cũ) có thể
/// trả bí danh CLDR cũ (vd "Asia/Saigon") thay vì tên IANA hiện hành — chuẩn hoá về tên hiện
/// hành trước khi kiểm hợp lệ (RK36).
/// </summary>
public static class TimeZoneCatalog
{
    public const string DefaultTimeZoneId = "Asia/Ho_Chi_Minh";

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Asia/Saigon"] = "Asia/Ho_Chi_Minh"
    };

    /// <summary>
    /// Trả về ID múi giờ hợp lệ — ID rỗng/không tìm được (kể cả sau khi quy bí danh) ⇒
    /// <see cref="DefaultTimeZoneId"/>, KHÔNG BAO GIỜ ném (R-T2: không được chặn cả request chỉ
    /// vì trình duyệt gửi một chuỗi múi giờ lạ).
    /// </summary>
    public static string Normalize(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return DefaultTimeZoneId;

        var candidate = timeZoneId.Trim();
        if (Aliases.TryGetValue(candidate, out var mapped))
            candidate = mapped;

        return TimeZoneInfo.TryFindSystemTimeZoneById(candidate, out _) ? candidate : DefaultTimeZoneId;
    }

    public static bool IsValid(string? timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
}
