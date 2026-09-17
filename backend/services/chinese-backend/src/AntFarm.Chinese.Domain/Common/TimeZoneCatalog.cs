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
    /// vì trình duyệt gửi một chuỗi múi giờ lạ). Dùng khi TẠO MỚI người dùng (không có múi giờ
    /// "cũ" nào để giữ lại) — F4 đồng bộ hồ sơ NGƯỜI ĐÃ TỒN TẠI dùng <see cref="TryNormalize"/>
    /// để giữ nguyên múi giờ cũ thay vì âm thầm đổi về mặc định (R4-4).
    /// </summary>
    public static string Normalize(string? timeZoneId) =>
        TryNormalize(timeZoneId, out var normalized) ? normalized : DefaultTimeZoneId;

    /// <summary>
    /// F4 (R4-4, RK36): thử chuẩn hoá — trả <c>false</c> khi rỗng hoặc không phải ID IANA hợp lệ
    /// (kể cả sau khi quy bí danh) thay vì tự ý rơi về <see cref="DefaultTimeZoneId"/>, để nơi gọi
    /// (vd đồng bộ hồ sơ người dùng đã có) tự quyết định GIỮ giá trị cũ khi claim hỏng.
    /// </summary>
    public static bool TryNormalize(string? timeZoneId, out string normalized)
    {
        normalized = DefaultTimeZoneId;
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return false;

        var candidate = timeZoneId.Trim();
        if (Aliases.TryGetValue(candidate, out var mapped))
            candidate = mapped;

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(candidate, out _))
            return false;

        normalized = candidate;
        return true;
    }

    public static bool IsValid(string? timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
}
