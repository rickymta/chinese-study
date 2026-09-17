using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Domain.Time;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Common.Time;

/// <inheritdoc cref="IUserDayContext"/>
public sealed class UserDayContext(IChineseDbContext db, TimeProvider timeProvider) : IUserDayContext
{
    public async Task<UserDay> GetAsync(Guid userId, CancellationToken ct)
    {
        // R-T3: đọc access.users.time_zone HIỆN TẠI (không phải claim JWT có thể lệch pha nếu vừa
        // đổi múi giờ nhưng chưa refresh) — cùng kỹ thuật StudyActivityRecorder (F5).
        var timeZone = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.TimeZone)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng — UserProvisioningMiddleware lẽ ra đã tạo trước khi tới đây.");

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var localDate = UserLocalDate.From(nowUtc, timeZone);
        var (startUtc, endUtc) = UserLocalDate.DayRange(localDate, timeZone);

        return new UserDay(timeZone, nowUtc, localDate, startUtc, endUtc);
    }
}
