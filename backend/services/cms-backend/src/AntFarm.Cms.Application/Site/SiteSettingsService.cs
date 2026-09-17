using AntFarm.Cms.Application.Common.Abstractions;
using AntFarm.Cms.Application.Common.Audit;
using AntFarm.Cms.Application.Common.Revalidation;
using AntFarm.Cms.Application.Site.Dtos;
using AntFarm.Cms.Domain.Site;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Cms.Application.Site;

/// <summary>
/// Cấu hình site/SEO (§5.2.3, §6.2 W3a) — 11 khoá cố định (<see cref="SiteSettingKeys"/>), một
/// người biên tập nên KHÔNG kiểm concurrency (last-write-wins, [BA-mặc định]). Chỉ khoá ĐỔI giá
/// trị mới cập nhật; không đổi gì ⇒ không ghi nhật ký, không revalidate.
/// </summary>
public sealed class SiteSettingsService(ICmsDbContext db, IAuditLogger auditLogger, IRevalidationNotifier revalidation, TimeProvider timeProvider)
{
    public async Task<SiteSettingsDto> GetAsync(CancellationToken ct)
    {
        var settings = await db.SiteSettings.AsNoTracking().ToListAsync(ct);
        return Map(settings);
    }

    public async Task<SiteSettingsDto> UpdateAsync(Actor actor, IReadOnlyDictionary<string, string> values, CancellationToken ct)
    {
        var settings = await db.SiteSettings.ToListAsync(ct);
        var byKey = settings.ToDictionary(s => s.Key);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var changedKeys = new List<string>();

        foreach (var definition in SiteSettingKeys.All)
        {
            var newValue = values.TryGetValue(definition.Key, out var raw) ? (raw ?? "").Trim() : definition.DefaultValue;

            if (!byKey.TryGetValue(definition.Key, out var setting))
            {
                // Không nên xảy ra (11 khoá luôn được seed sẵn) — phòng hờ dữ liệu bị xoá tay.
                db.SiteSettings.Add(SiteSetting.Create(definition.Key, newValue, now, actor.Id));
                changedKeys.Add(definition.Key);
                continue;
            }

            if (setting.Update(newValue, actor.Id, now))
                changedKeys.Add(definition.Key);
        }

        if (changedKeys.Count > 0)
        {
            auditLogger.Add(actor, "site_settings.update", "site_settings", null, $"Đổi: {string.Join(", ", changedKeys.OrderBy(k => k))}");
            await db.SaveChangesAsync(ct);
            await revalidation.NotifyAsync(["site"], ct);
        }

        var current = await db.SiteSettings.AsNoTracking().ToListAsync(ct);
        return Map(current);
    }

    private static SiteSettingsDto Map(List<SiteSetting> settings) => new(
        settings.ToDictionary(s => s.Key, s => s.Value),
        settings.Count > 0 ? settings.Max(s => s.UpdatedAt) : DateTime.MinValue);
}
