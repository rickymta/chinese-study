namespace AntFarm.Chinese.Domain.Progress;

/// <summary>Kết quả tính chuỗi ngày học (F11, R-PG3) — <see cref="Longest"/> luôn ≥ <see cref="Current"/>.</summary>
public readonly record struct StreakResult(int Current, int Longest, bool StudiedToday);

/// <summary>
/// Hàm THUẦN tính chuỗi ngày học liên tiếp (F11, R-PG3/R-PG4) — tách khỏi truy vấn DB
/// (<c>ProgressOverviewService</c>) để unit test không cần DB thật, cùng kỹ thuật
/// <c>ToneStatsCalculator</c> (F5).
/// </summary>
public static class StreakCalculator
{
    /// <param name="studiedDates">Mọi ngày lịch (địa phương) có ≥ 1 dòng <c>study_events.quantity &gt; 0</c> — KHÔNG cần sắp xếp/khử trùng trước, hàm tự lo.</param>
    /// <param name="today">"Hôm nay" theo múi giờ người dùng (R-PG2).</param>
    public static StreakResult Calculate(IReadOnlyCollection<DateOnly> studiedDates, DateOnly today)
    {
        var distinct = new HashSet<DateOnly>(studiedDates);
        if (distinct.Count == 0)
            return new StreakResult(0, 0, false);

        var studiedToday = distinct.Contains(today);

        // "Dài nhất": xét TOÀN BỘ lịch sử — kể cả ngày tương lai (do đổi múi giờ, R-PG3) — chỉ
        // "hiện tại" mới loại ngày tương lai. Sắp tăng dần rồi đo độ dài từng đoạn liên tiếp.
        var longest = 0;
        var run = 0;
        DateOnly? previous = null;
        foreach (var date in distinct.OrderBy(d => d))
        {
            run = previous is not null && date == previous.Value.AddDays(1) ? run + 1 : 1;
            longest = Math.Max(longest, run);
            previous = date;
        }

        // "Hiện tại": đếm lùi từ hôm nay (nếu đã học) hoặc hôm qua (nếu hôm nay chưa học, chuỗi
        // coi như CHƯA đứt tới hết ngày hôm nay — R-PG3); cursor không bao giờ vượt quá "today" nên
        // ngày tương lai tự động không được đếm.
        var current = 0;
        var cursor = studiedToday ? today : today.AddDays(-1);
        while (distinct.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-1);
        }

        return new StreakResult(current, longest, studiedToday);
    }
}
