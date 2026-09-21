using System.Text.RegularExpressions;

namespace AntFarm.Cms.Application.Site.Validators;

/// <summary>Luật định dạng dùng CHUNG cho <c>CreateFaqRequestValidator</c>/<c>UpdateFaqRequestValidator</c> (§5.2.3 W3b).</summary>
public static partial class FaqInputRules
{
    public const string DefaultGroupKey = "general";

    [GeneratedRegex("^[a-z0-9-]{1,32}$")]
    public static partial Regex GroupKeyPattern();

    /// <summary>Rỗng/khoảng trắng ⇒ mặc định <see cref="DefaultGroupKey"/> (chuẩn hoá tại service, KHÔNG ở validator — validator chỉ kiểm hình dạng khi có giá trị).</summary>
    public static string Normalize(string? groupKey) =>
        string.IsNullOrWhiteSpace(groupKey) ? DefaultGroupKey : groupKey.Trim().ToLowerInvariant();
}
