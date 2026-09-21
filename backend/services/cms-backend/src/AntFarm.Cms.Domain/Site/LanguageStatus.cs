namespace AntFarm.Cms.Domain.Site;

/// <summary>Trạng thái hiển thị của một ngôn ngữ trên website (§5.1.2 W3a) — lưu DB dạng chuỗi qua <see cref="LanguageStatuses"/>.</summary>
public enum LanguageStatus
{
    Open,
    ComingSoon,
    Hidden
}

/// <summary>Ánh xạ <see cref="LanguageStatus"/> ⇄ chuỗi lưu DB/JSON (<c>open|coming_soon|hidden</c>) — dùng CHUNG cho EF <c>HasConversion</c> (Infrastructure) và DTO (Application).</summary>
public static class LanguageStatuses
{
    public const string Open = "open";
    public const string ComingSoon = "coming_soon";
    public const string Hidden = "hidden";

    public static readonly IReadOnlyList<string> All = [Open, ComingSoon, Hidden];

    private static readonly IReadOnlyDictionary<LanguageStatus, string> ToDbMap = new Dictionary<LanguageStatus, string>
    {
        [LanguageStatus.Open] = Open,
        [LanguageStatus.ComingSoon] = ComingSoon,
        [LanguageStatus.Hidden] = Hidden
    };

    private static readonly IReadOnlyDictionary<string, LanguageStatus> FromDbMap =
        ToDbMap.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static string ToCode(LanguageStatus status) => ToDbMap[status];

    public static bool TryParse(string code, out LanguageStatus status) => FromDbMap.TryGetValue(code, out status);

    public static LanguageStatus Parse(string code) =>
        FromDbMap.TryGetValue(code, out var status)
            ? status
            : throw new ArgumentOutOfRangeException(nameof(code), code, "Trạng thái ngôn ngữ không hợp lệ.");
}
