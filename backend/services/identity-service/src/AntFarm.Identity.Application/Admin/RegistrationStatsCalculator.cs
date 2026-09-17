using AntFarm.Core.Errors;

namespace AntFarm.Identity.Application.Admin;

/// <summary>
/// Phần THUẦN của thống kê đăng ký (§5.2.9) — tách khỏi <see cref="RegistrationStatsService"/> để
/// unit test được ranh giới ngày Việt Nam mà không cần PostgreSQL thật.
/// </summary>
public static class RegistrationStatsCalculator
{
    public const int MaxRangeDays = 366;

    public static TimeZoneInfo VietnamTimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public static (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to, DateOnly todayVn)
    {
        var toDate = to ?? todayVn;
        var fromDate = from ?? toDate.AddDays(-29);
        return (fromDate, toDate);
    }

    /// <summary>400 VALIDATION: <c>from &gt; to</c> hoặc khoảng vượt quá <see cref="MaxRangeDays"/> ngày (§6.5).</summary>
    public static void EnsureValidRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new ValidationAppException("Dữ liệu gửi lên không hợp lệ.",
                new Dictionary<string, string[]> { ["from"] = ["from phải nhỏ hơn hoặc bằng to."] });

        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
            throw new ValidationAppException("Dữ liệu gửi lên không hợp lệ.",
                new Dictionary<string, string[]> { ["to"] = [$"Khoảng ngày tối đa {MaxRangeDays} ngày."] });
    }

    /// <summary>Quy đổi mốc UTC (lưu DB) về ngày lịch VIỆT NAM — CLAUDE.md: ngày vận hành/học tập tính theo múi giờ người dùng, không phải UTC.</summary>
    public static DateOnly ToVietnamDate(DateTime utcCreatedAt)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcCreatedAt, DateTimeKind.Utc), VietnamTimeZone));

    /// <summary>00:00 giờ Việt Nam của <paramref name="date"/> quy về UTC (Kind=Utc tường minh — Npgsql timestamptz chỉ nhận Kind=Utc, CLAUDE.md).</summary>
    public static DateTime ToUtcMidnight(DateOnly date)
        => TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), VietnamTimeZone);

    /// <summary>Đủ MỌI ngày trong khoảng — ngày không có đăng ký nào ⇒ <c>count: 0</c> (§6.5).</summary>
    public static IReadOnlyList<RegistrationDayDto> BuildDays(DateOnly from, DateOnly to, IReadOnlyDictionary<DateOnly, int> counts)
    {
        var totalDays = to.DayNumber - from.DayNumber + 1;
        var days = new List<RegistrationDayDto>(totalDays);
        for (var day = from; day <= to; day = day.AddDays(1))
            days.Add(new RegistrationDayDto(day, counts.GetValueOrDefault(day)));
        return days;
    }
}
