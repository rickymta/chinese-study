using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Lessons.Dtos;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Lessons;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Lessons;

/// <summary><c>POST /api/lessons/{id}/start</c> (R-LS12, §6.1) — tạo <c>lesson_progress(status='in_progress')</c> nếu chưa có; idempotent; KHÔNG ghi <c>study_events</c> (chưa có kết quả).</summary>
public sealed class LessonProgressService(IChineseDbContext db, TimeProvider timeProvider)
{
    public async Task<LessonProgressDto> StartAsync(Guid userId, Guid lessonId, CancellationToken ct)
    {
        var lessonExists = await db.Lessons.AsNoTracking()
            .AnyAsync(l => l.Id == lessonId && l.Status == LessonStatuses.Published, ct);
        if (!lessonExists)
            throw new NotFoundException($"Không tìm thấy bài học '{lessonId}'.");

        var existing = await db.LessonProgress.FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, ct);
        if (existing is not null)
            return LessonQueryService.ToProgressDto(existing); // R-LS12: đã có ⇒ trả bản hiện có, KỂ CẢ completed.

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var progress = LessonProgress.Start(userId, lessonId, nowUtc, nowUtc);
        db.LessonProgress.Add(progress);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Đua hiếm: hai request "bắt đầu bài" gần như đồng thời — người thua gỡ ChangeTracker rồi
            // đọc lại đúng dòng người thắng vừa tạo (cùng kỹ thuật UserProvisioningService, F3).
            db.ClearTracking();
            var winner = await db.LessonProgress.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, ct);
            if (winner is null)
                throw;

            return LessonQueryService.ToProgressDto(winner);
        }

        return LessonQueryService.ToProgressDto(progress);
    }
}
