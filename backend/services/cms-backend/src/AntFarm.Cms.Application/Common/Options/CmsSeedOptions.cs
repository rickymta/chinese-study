namespace AntFarm.Cms.Application.Common.Options;

/// <summary>
/// Danh sách ngôn ngữ seed ban đầu (chỉ chèn khi <c>site.languages</c> RỖNG, R-W25) — bind từ section
/// "CmsSeed". Thiếu cấu hình (mảng rỗng) ⇒ <c>SiteSeeder</c> dùng bộ mặc định trong code (§5.2.3).
/// </summary>
public sealed class CmsSeedOptions
{
    public sealed class LanguageSeedItem
    {
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
        public string NativeName { get; init; } = "";
        public string Tagline { get; init; } = "";
        public string Status { get; init; } = "coming_soon";
        public string? AppUrl { get; init; }
        public int SortOrder { get; init; }
    }

    public LanguageSeedItem[] Languages { get; init; } = [];
}
