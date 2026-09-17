using System.Text.RegularExpressions;

namespace AntFarm.Chinese.Domain.Pinyin;

/// <summary>Một hàng của bảng âm tiết pinyin (R5-1) — <see cref="Key"/> là khoá âm tiết theo chính tả chuẩn, không thanh.</summary>
public sealed partial record PinyinSyllable(string Key, string Initial, string Final)
{
    // R5-1: chỉ 4 âm tiết được phép viết ü thành 'v' (chính tả chuẩn có hai chấm); sau j/q/x/y
    // chính tả chuẩn viết 'u' nên khoá vẫn là 'ju'/'qu'/'xue'/'yuan' (không chứa 'v').
    private static readonly HashSet<string> AllowedVKeys = ["nv", "lv", "nve", "lve"];

    /// <summary>Khớp <c>^[a-z]+$</c> và chỉ chứa 'v' nếu là một trong 4 khoá được phép (R5-1).</summary>
    public static bool IsValidKey(string key) =>
        !string.IsNullOrEmpty(key)
        && KeyPattern().IsMatch(key)
        && (!key.Contains('v') || AllowedVKeys.Contains(key));

    [GeneratedRegex("^[a-z]+$")]
    private static partial Regex KeyPattern();
}
