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

        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, timeZone);
        var toUtcExclusive = TimeZoneInfo.ConvertTimeToUtc(endLocal, timeZone);
        return (fromUtc, toUtcExclusive);
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var found))
            return found;

        return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
    }
}
