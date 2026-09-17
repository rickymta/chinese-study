using AntFarm.Chinese.Domain.Common;

namespace AntFarm.Chinese.Domain.Time;

/// <summary>
/// Quy đổi mốc UTC ⇄ "ngày học" theo múi giờ người dùng (R-T3, §1.3) — Domain thuần, chỉ dùng
/// <see cref="TimeZoneInfo"/> (BCL). Múi giờ lạ/không tìm được KHÔNG BAO GIỜ ném (RK36) — dùng
/// <see cref="TimeZoneCatalog.DefaultTimeZoneId"/> làm dự phòng để một chuỗi múi giờ hỏng trong
/// <c>access.users.time_zone</c> không chặn cả việc ghi <c>study_events</c>.
/// </summary>
public static class UserLocalDate
{
    public const string DefaultTimeZoneId = TimeZoneCatalog.DefaultTimeZoneId;

    /// <summary>Ngày lịch của mốc UTC <paramref name="utc"/> theo múi giờ <paramref name="timeZoneId"/>.</summary>
    public static DateOnly From(DateTime utc, string timeZoneId)
    {
        var timeZone = ResolveTimeZone(timeZoneId);
        var utcKind = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcKind, timeZone);
        return DateOnly.FromDateTime(local);
    }

    /// <summary>
    /// Khoảng UTC [FromUtc, ToUtcExclusive) tương ứng một ngày lịch <paramref name="date"/> ở múi
    /// giờ <paramref name="timeZoneId"/> — dùng để lọc theo ngày (nửa hở, R-T4) ở F7/F11. Cả hai
    /// mốc trả về đều có <c>Kind=Utc</c>.
    /// </summary>
    public static (DateTime FromUtc, DateTime ToUtcExclusive) DayRange(DateOnly date, string timeZoneId)
    {
        var timeZone = ResolveTimeZone(timeZoneId);
        var startLocal = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var endLocal = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(ResolveValidLocal(startLocal, timeZone), timeZone);
        var toUtcExclusive = TimeZoneInfo.ConvertTimeToUtc(ResolveValidLocal(endLocal, timeZone), timeZone);
        return (fromUtc, toUtcExclusive);
    }

    /// <summary>
    /// 00:00 của một ngày có thể rơi vào "giờ không tồn tại" khi múi giờ đó chuyển sang giờ mùa hè
    /// đúng lúc nửa đêm (không phải trường hợp <c>America/New_York</c>/<c>Asia/Ho_Chi_Minh</c> — hai
    /// múi kiểm ở test đã đúng bằng công thức thường — nhưng một số múi giờ khác, vd Brazil trước
    /// 2019, nhảy DST lúc 00:00) — <see cref="TimeZoneInfo.ConvertTimeToUtc(DateTime, TimeZoneInfo)"/>
    /// ném <see cref="ArgumentException"/> cho giờ không hợp lệ này. Cộng dồn 1 giờ tới khi hợp lệ
    /// (R7-3) thay vì để cả việc lọc "hôm nay" sập vì một mốc nửa đêm hiếm gặp.
    /// </summary>
    private static DateTime ResolveValidLocal(DateTime local, TimeZoneInfo timeZone)
    {
        while (timeZone.IsInvalidTime(local))
            local = local.AddHours(1);

        return local;
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var found))
            return found;

        return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
    }
}
