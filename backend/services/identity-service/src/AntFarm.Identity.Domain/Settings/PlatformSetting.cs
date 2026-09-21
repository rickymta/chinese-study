namespace AntFarm.Identity.Domain.Settings;

/// <summary>
/// Cài đặt nền tảng runtime (W10, §5.1.7) — hiện chỉ có <see cref="PlatformSettingKeys.RegistrationEnabled"/>.
/// KHÔNG seed: thiếu dòng ⇒ Application dùng giá trị cấu hình tĩnh làm mặc định (R-W18, D-W4).
/// </summary>
public sealed class PlatformSetting
{
    public string Key { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public DateTime UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    // EF Core cần constructor không tham số (qua reflection) — không lộ ra ngoài assembly để
    // buộc code khác luôn tạo qua Create()/Update() (cùng khuôn Account).
    private PlatformSetting()
    {
    }

    public static PlatformSetting Create(string key, string value, Guid actorId, DateTime now)
        => new() { Key = key, Value = value, UpdatedAt = now, UpdatedBy = actorId };

    public void Update(string value, Guid actorId, DateTime now)
    {
        Value = value;
        UpdatedAt = now;
        UpdatedBy = actorId;
    }
}
