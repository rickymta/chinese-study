using System.Text.RegularExpressions;

namespace AntFarm.Cms.Application.Site.Validators;

/// <summary>Luật định dạng dùng CHUNG cho <c>CreateLanguageRequestValidator</c>/<c>UpdateLanguageRequestValidator</c> (§5.2.3 W3a).</summary>
public static partial class LanguageInputRules
{
    /// <summary>Bất biến sau khi tạo — 2–32 ký tự, chữ thường bắt đầu, số/gạch ngang theo sau.</summary>
    [GeneratedRegex("^[a-z][a-z0-9-]{1,31}$")]
    public static partial Regex CodePattern();

    [GeneratedRegex(@"^https://\S+$")]
    public static partial Regex HttpsUrlPattern();

    /// <summary>Cho phép dev local trỏ <c>appUrl</c> vào app ngôn ngữ đang chạy trên máy (§5.2.3).</summary>
    [GeneratedRegex(@"^http://(localhost|127\.0\.0\.1)(:\d+)?(/\S*)?$")]
    public static partial Regex LocalHttpUrlPattern();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$")]
    public static partial Regex AccentColorPattern();

    public static readonly IReadOnlyList<string> ValidStatuses = ["open", "coming_soon", "hidden"];

    public static bool IsValidAppUrl(string url) => HttpsUrlPattern().IsMatch(url) || LocalHttpUrlPattern().IsMatch(url);
}
