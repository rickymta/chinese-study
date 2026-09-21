namespace AntFarm.Cms.Domain.Site;

/// <summary>
/// Một ngôn ngữ hiển thị trên website/portal (schema <c>site.languages</c>, §5.1.2 W3a).
/// <see cref="CoverMediaId"/> luôn null ở W3a (bảng media tạo ở W4). <see cref="Version"/> ánh xạ
/// cột hệ thống <c>xmin</c> (khuôn chinese-backend F10, R-CA3) — chống ghi đè đồng thời.
/// </summary>
public sealed class Language
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string NativeName { get; private set; } = null!;
    public string Tagline { get; private set; } = string.Empty;
    public string DescriptionMarkdown { get; private set; } = string.Empty;
    public LanguageStatus Status { get; private set; }
    public string? AppUrl { get; private set; }
    public string? AccentColor { get; private set; }
    public Guid? CoverMediaId { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public uint Version { get; private set; }

    // EF Core cần constructor không tham số — không lộ ra ngoài assembly để buộc luôn tạo qua Create().
    private Language()
    {
    }

    /// <summary><paramref name="actorId"/> null khi tạo bởi <c>SiteSeeder</c> (không có actor thật lúc khởi động).</summary>
    public static Language Create(
        string code, string name, string nativeName, string tagline, string descriptionMarkdown,
        LanguageStatus status, string? appUrl, string? accentColor, int sortOrder, Guid? actorId, DateTime now) => new()
    {
        Id = Guid.CreateVersion7(),
        Code = code,
        Name = name,
        NativeName = nativeName,
        Tagline = tagline,
        DescriptionMarkdown = descriptionMarkdown,
        Status = status,
        AppUrl = appUrl,
        AccentColor = accentColor,
        CoverMediaId = null,
        SortOrder = sortOrder,
        UpdatedAt = now,
        UpdatedBy = actorId
    };

    /// <summary>Sửa (§5.2.3) — KHÔNG đổi <see cref="Code"/> (bất biến sau khi tạo).</summary>
    public void Update(
        string name, string nativeName, string tagline, string descriptionMarkdown,
        LanguageStatus status, string? appUrl, string? accentColor, Guid actorId, DateTime now)
    {
        Name = name;
        NativeName = nativeName;
        Tagline = tagline;
        DescriptionMarkdown = descriptionMarkdown;
        Status = status;
        AppUrl = appUrl;
        AccentColor = accentColor;
        UpdatedAt = now;
        UpdatedBy = actorId;
    }

    /// <summary>Đặt lại thứ tự hiển thị (1..n) — dùng bởi <c>PUT /api/admin/languages/order</c>.</summary>
    public void Reorder(int sortOrder) => SortOrder = sortOrder;
}
