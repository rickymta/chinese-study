using AntFarm.Chinese.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using DomainLearnerSettings = AntFarm.Chinese.Domain.Learning.LearnerSettings;

namespace AntFarm.Chinese.Application.Learning;

/// <summary>GET/PUT /api/me/learning-settings (§6.2) — GET không tự chèn dòng, PUT upsert (R7-14: chỉ ảnh hưởng các lượt chấm SAU).</summary>
public sealed class LearnerSettingsService(IChineseDbContext db, TimeProvider timeProvider)
{
    public async Task<LearnerSettingsDto> GetAsync(Guid userId, CancellationToken ct)
    {
        var settings = await db.LearnerSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
        return settings is null ? DefaultDto() : ToDto(settings, isDefault: false);
    }

    /// <summary>
    /// Ba số cấu hình dùng CHUNG bởi <c>SrsSummaryService</c>/<c>SrsQueueService</c>/<c>SrsReviewService</c>
    /// (tránh lặp "settings ?? mặc định" ở ba nơi) — trả mặc định (§5.1.2) khi người dùng chưa từng lưu cài đặt.
    /// </summary>
    public async Task<EffectiveLearnerSettings> GetEffectiveAsync(Guid userId, CancellationToken ct)
    {
        var settings = await db.LearnerSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
        return settings is null
            ? new EffectiveLearnerSettings(
                DomainLearnerSettings.Defaults.DailyNewCards, DomainLearnerSettings.Defaults.DailyReviewLimit,
                DomainLearnerSettings.Defaults.DesiredRetention)
            : new EffectiveLearnerSettings(settings.DailyNewCards, settings.DailyReviewLimit, settings.DesiredRetention);
    }

    public async Task<LearnerSettingsDto> UpdateAsync(Guid userId, UpdateLearnerSettingsCommand command, CancellationToken ct)
    {
        var settings = await db.LearnerSettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (settings is null)
        {
            settings = DomainLearnerSettings.CreateDefault(userId, now);
            db.LearnerSettings.Add(settings);
        }

        settings.Update(command.DailyNewCards, command.DailyReviewLimit, command.DesiredRetention, command.TtsRate, command.AutoPlayAudio, now);
        await db.SaveChangesAsync(ct);

        return ToDto(settings, isDefault: false);
    }

    private static LearnerSettingsDto DefaultDto() => new(
        DomainLearnerSettings.Defaults.DailyNewCards, DomainLearnerSettings.Defaults.DailyReviewLimit,
        DomainLearnerSettings.Defaults.DesiredRetention, DomainLearnerSettings.Defaults.TtsRate,
        DomainLearnerSettings.Defaults.AutoPlayAudio, IsDefault: true);

    private static LearnerSettingsDto ToDto(DomainLearnerSettings s, bool isDefault) =>
        new(s.DailyNewCards, s.DailyReviewLimit, s.DesiredRetention, s.TtsRate, s.AutoPlayAudio, isDefault);
}
