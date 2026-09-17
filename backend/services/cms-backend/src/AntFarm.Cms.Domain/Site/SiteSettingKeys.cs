using System.Text.RegularExpressions;

namespace AntFarm.Cms.Domain.Site;

/// <summary>
/// Whitelist khoá <c>site.settings</c> + giá trị mặc định + luật hợp lệ (§5.2.3 W3a) — NGUỒN DUY
/// NHẤT: <c>UpdateSiteSettingsRequestValidator</c> (400 thiếu/lạ khoá) và <c>SiteSeeder</c> (chèn
/// bù khoá thiếu) đều đọc từ đây, không gõ tay lại danh sách. Luật áp dụng SAU khi <c>Trim()</c>
/// giá trị gửi lên.
/// </summary>
public static partial class SiteSettingKeys
{
    public const string SiteName = "site.name";
    public const string SiteTagline = "site.tagline";
    public const string SeoDefaultTitle = "seo.default_title";
    public const string SeoDefaultDescription = "seo.default_description";
    public const string SeoOgImageMediaId = "seo.og_image_media_id";
    public const string ContactEmail = "contact.email";
    public const string SocialFacebook = "social.facebook";
    public const string SocialYoutube = "social.youtube";
    public const string SocialTiktok = "social.tiktok";
    public const string FooterText = "footer.text";
    public const string HomeHeroMode = "home.hero_mode";

    public sealed record KeyDefinition(string Key, string DefaultValue, Func<string, bool> IsValid, string RuleDescription);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^https://\S+$")]
    private static partial Regex HttpsUrlPattern();

    private static bool IsEmptyOr(string value, int maxLength, Func<string, bool> isValid) =>
        value.Length == 0 || (value.Length <= maxLength && isValid(value));

    public static readonly IReadOnlyList<KeyDefinition> All =
    [
        new(SiteName, "AntFarm", v => v.Length is >= 1 and <= 100, "bắt buộc, 1–100 ký tự"),
        new(SiteTagline, "Học ngoại ngữ trực tuyến cho người Việt", v => v.Length <= 160, "≤ 160 ký tự"),
        new(SeoDefaultTitle, "AntFarm — Học ngoại ngữ trực tuyến", v => v.Length <= 70, "≤ 70 ký tự"),
        new(SeoDefaultDescription, "", v => v.Length <= 160, "≤ 160 ký tự"),
        new(SeoOgImageMediaId, "", v => IsEmptyOr(v, 36, g => Guid.TryParse(g, out _)), "rỗng hoặc GUID"),
        new(ContactEmail, "", v => IsEmptyOr(v, 254, EmailPattern().IsMatch), "rỗng hoặc email hợp lệ"),
        new(SocialFacebook, "", v => IsEmptyOr(v, 300, HttpsUrlPattern().IsMatch), "rỗng hoặc URL https://"),
        new(SocialYoutube, "", v => IsEmptyOr(v, 300, HttpsUrlPattern().IsMatch), "rỗng hoặc URL https://"),
        new(SocialTiktok, "", v => IsEmptyOr(v, 300, HttpsUrlPattern().IsMatch), "rỗng hoặc URL https://"),
        new(FooterText, "", v => v.Length <= 500, "≤ 500 ký tự"),
        new(HomeHeroMode, "static", v => v is "banners" or "static", "banners hoặc static")
    ];

    public static readonly IReadOnlyDictionary<string, KeyDefinition> ByKey =
        All.ToDictionary(d => d.Key);
}
