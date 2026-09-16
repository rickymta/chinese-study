using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Time;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Learning;

/// <inheritdoc cref="IStudyActivityRecorder"/>
public sealed class StudyActivityRecorder(IChineseDbContext db, TimeProvider timeProvider) : IStudyActivityRecorder
{
    public async Task<StudyEvent> RecordAsync(Guid userId, string kind, DateTime occurredAtUtc, int quantity, int? correct, Guid? refId, CancellationToken ct)
    {
        if (!StudyEventKinds.IsKnown(kind))
            throw new ArgumentException($"kind '{kind}' không thuộc danh mục StudyEventKinds đã biết.", nameof(kind));

        // R-T3: local_date tính theo múi giờ NGƯỜI DÙNG TẠI LÚC GHI — đọc access.users.time_zone
        // hiện tại, không phải claim JWT (có thể lệch pha nếu vừa đổi múi giờ nhưng chưa refresh).
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng để ghi study_events — UserProvisioningMiddleware lẽ ra đã tạo trước khi tới đây.");

        var localDate = UserLocalDate.From(occurredAtUtc, user.TimeZone);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var studyEvent = StudyEvent.Create(userId, kind, occurredAtUtc, localDate, quantity, correct, refId, nowUtc);
        db.StudyEvents.Add(studyEvent);

        return studyEvent;
    }
}
