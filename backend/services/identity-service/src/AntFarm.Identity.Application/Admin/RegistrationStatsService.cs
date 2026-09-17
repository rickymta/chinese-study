using AntFarm.Identity.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Identity.Application.Admin;

/// <summary>
/// <c>GET /internal/stats/registrations</c> (§5.2.9, §6.5) — phần THUẦN (ranh giới ngày Việt Nam,
/// kiểm khoảng ngày) sống ở <see cref="RegistrationStatsCalculator"/> để unit test không cần DB.
/// Kéo CHỈ cột <c>CreatedAt</c> trong khoảng đã lọc về bộ nhớ rồi nhóm theo ngày VN — quy mô MVP
/// vài nghìn tài khoản/năm nên chấp nhận được, tránh phải viết biểu thức chuyển múi giờ trong SQL.
/// </summary>
public sealed class RegistrationStatsService(IIdentityDbContext db, TimeProvider timeProvider)
{
    public async Task<RegistrationStatsDto> GetAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var todayVn = RegistrationStatsCalculator.ToVietnamDate(nowUtc);

        var (fromDate, toDate) = RegistrationStatsCalculator.ResolveRange(from, to, todayVn);
        RegistrationStatsCalculator.EnsureValidRange(fromDate, toDate);

        // Cận trên NỬA HỞ (< toDate+1) — CLAUDE.md: lọc khoảng ngày luôn `>= from && < to+1`.
        var fromUtc = RegistrationStatsCalculator.ToUtcMidnight(fromDate);
        var toUtcExclusive = RegistrationStatsCalculator.ToUtcMidnight(toDate.AddDays(1));

        var createdTimestamps = await db.Accounts.AsNoTracking()
            .Where(a => a.CreatedAt >= fromUtc && a.CreatedAt < toUtcExclusive)
            .Select(a => a.CreatedAt)
            .ToListAsync(ct);

        var counts = new Dictionary<DateOnly, int>();
        foreach (var createdAt in createdTimestamps)
        {
            var dayVn = RegistrationStatsCalculator.ToVietnamDate(createdAt);
            counts[dayVn] = counts.GetValueOrDefault(dayVn) + 1;
        }

        var days = RegistrationStatsCalculator.BuildDays(fromDate, toDate, counts);

        var totalAccounts = await db.Accounts.CountAsync(ct);
        var disabledAccounts = await db.Accounts.CountAsync(a => !a.IsActive, ct);
        var activeLast7Days = await db.Accounts.CountAsync(a => a.LastLoginAt != null && a.LastLoginAt >= nowUtc.AddDays(-7), ct);
        var activeLast30Days = await db.Accounts.CountAsync(a => a.LastLoginAt != null && a.LastLoginAt >= nowUtc.AddDays(-30), ct);

        return new RegistrationStatsDto("Asia/Ho_Chi_Minh", days, totalAccounts, disabledAccounts, activeLast7Days, activeLast30Days);
    }
}
