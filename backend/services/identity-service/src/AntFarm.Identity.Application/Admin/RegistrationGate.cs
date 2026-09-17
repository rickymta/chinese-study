using AntFarm.Identity.Application.Common.Abstractions;
using AntFarm.Identity.Application.Common.Options;
using AntFarm.Identity.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AntFarm.Identity.Application.Admin;

/// <summary>D-W4/§5.2.9: nguồn sự thật cho "đăng ký có đang mở hay không" — <c>AuthService.RegisterAsync</c> hỏi qua đây thay vì đọc thẳng <see cref="AuthOptions.AllowRegistration"/>.</summary>
public interface IRegistrationGate
{
    Task<RegistrationStateDto> GetAsync(CancellationToken ct);

    /// <summary>Gọi ngay sau khi admin PUT cài đặt — bắt buộc để "tắt đăng ký" có hiệu lực NGAY thay vì chờ hết cache (test W10 mục 5).</summary>
    void Invalidate();
}

/// <summary>
/// D-W4: "đăng ký mở" là cài đặt RUNTIME (bảng identity.settings) — <c>Auth:AllowRegistration</c>
/// chỉ là giá trị KHỞI TẠO dùng khi chưa có dòng nào trong DB (R-W18). Cache 30 giây: đọc DB rẻ
/// nhưng <c>RegisterAsync</c> gọi trên MỌI request đăng ký — tránh cộng thêm một round-trip DB
/// cho path phổ biến nhất. Singleton + <see cref="IServiceScopeFactory"/>: <c>IIdentityDbContext</c>
/// là Scoped (theo request), còn gate này sống suốt vòng đời ứng dụng nên phải tự mở scope khi
/// cần đọc DB (khuôn PermissionResolver của chinese-backend).
/// </summary>
public sealed class RegistrationGate(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    AuthOptions authOptions,
    ILogger<RegistrationGate> logger) : IRegistrationGate
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private readonly object _lock = new();
    private RegistrationStateDto? _cached;
    private DateTimeOffset _cachedAt;

    public async Task<RegistrationStateDto> GetAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            if (_cached is not null && timeProvider.GetUtcNow() - _cachedAt < CacheDuration)
                return _cached;
        }

        var state = await LoadAsync(ct);

        lock (_lock)
        {
            _cached = state;
            _cachedAt = timeProvider.GetUtcNow();
        }

        return state;
    }

    public void Invalidate()
    {
        lock (_lock)
            _cached = null;
    }

    private async Task<RegistrationStateDto> LoadAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityDbContext>();

        var setting = await db.Settings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == PlatformSettingKeys.RegistrationEnabled, ct);

        if (setting is null)
            return new RegistrationStateDto(authOptions.AllowRegistration, "configuration", null);

        if (setting.Value is "true" or "false")
            return new RegistrationStateDto(setting.Value == "true", "database", setting.UpdatedAt);

        logger.LogWarning(
            "Giá trị identity.settings[{Key}] không hợp lệ ({Value}) — dùng cấu hình mặc định.",
            PlatformSettingKeys.RegistrationEnabled, setting.Value);
        return new RegistrationStateDto(authOptions.AllowRegistration, "configuration", null);
    }
}
