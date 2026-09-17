using AntFarm.Identity.Application.Common.Abstractions;
using AntFarm.Identity.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Identity.Application.Admin;

/// <summary><c>GET/PUT /internal/settings/registration</c> (§5.2.9, §6.5, D-W4) — bật/tắt đăng ký RUNTIME, không cần restart service.</summary>
public sealed class PlatformSettingsService(IIdentityDbContext db, IRegistrationGate registrationGate, TimeProvider timeProvider)
{
    public Task<RegistrationStateDto> GetRegistrationAsync(CancellationToken ct) => registrationGate.GetAsync(ct);

    public async Task<RegistrationStateDto> SetRegistrationAsync(Guid actorId, bool enabled, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var value = enabled ? "true" : "false";

        var setting = await db.Settings.FirstOrDefaultAsync(s => s.Key == PlatformSettingKeys.RegistrationEnabled, ct);
        if (setting is null)
        {
            setting = PlatformSetting.Create(PlatformSettingKeys.RegistrationEnabled, value, actorId, now);
            db.Settings.Add(setting);
        }
        else
        {
            setting.Update(value, actorId, now);
        }

        await db.SaveChangesAsync(ct);
        // Bắt buộc — nếu không, "tắt đăng ký" chỉ có hiệu lực sau tối đa 30s cache của RegistrationGate.
        registrationGate.Invalidate();

        return new RegistrationStateDto(enabled, "database", now);
    }
}
