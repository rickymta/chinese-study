namespace AntFarm.Identity.Domain.Settings;

/// <summary>Danh sách khoá <see cref="PlatformSetting"/> hợp lệ (§5.1.7) — hiện chỉ một khoá.</summary>
public static class PlatformSettingKeys
{
    /// <summary>Giá trị lưu dạng chuỗi <c>"true"|"false"</c> (khớp §5.1.7) — không phải bool để nhất quán với cột <c>value text</c> dùng chung cho mọi khoá cài đặt tương lai.</summary>
    public const string RegistrationEnabled = "registration.enabled";
}
