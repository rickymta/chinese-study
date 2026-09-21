namespace AntFarm.Cms.Domain.Site;

/// <summary>
/// Một khoá cấu hình site/SEO (schema <c>site.settings</c>, §5.1.2 W3a) — danh mục hệ thống, KHÔNG
/// xoá được (chỉ 11 khoá cố định theo <see cref="SiteSettingKeys"/>), chỉ đổi <see cref="Value"/>.
/// </summary>
public sealed class SiteSetting
{
    public string Key { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public DateTime UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    // EF Core cần constructor không tham số — không lộ ra ngoài assembly để buộc luôn tạo qua Create().
    private SiteSetting()
    {
    }

    /// <summary>Chèn bù lúc seed (§5.2.3) — <paramref name="updatedBy"/> null vì không có actor thật lúc khởi động.</summary>
    public static SiteSetting Create(string key, string value, DateTime now, Guid? updatedBy = null) => new()
    {
        Key = key,
        Value = value,
        UpdatedAt = now,
        UpdatedBy = updatedBy
    };

    /// <summary>
    /// Chỉ đổi <see cref="UpdatedAt"/>/<see cref="UpdatedBy"/> khi giá trị THẬT SỰ khác (§5.2.3) —
    /// trả về true nếu có đổi, để service quyết định có ghi nhật ký/revalidate hay không.
    /// </summary>
    public bool Update(string value, Guid actorId, DateTime now)
    {
        if (Value == value)
            return false;

        Value = value;
        UpdatedAt = now;
        UpdatedBy = actorId;
        return true;
    }
}
