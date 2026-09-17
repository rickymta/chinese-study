using System.Text.RegularExpressions;

namespace AntFarm.Chinese.Application.Admin.Content.Validators;

/// <summary>Luật <c>slug</c> dùng chung (R-CA8) — <c>3–64</c> ký tự, chữ thường/số, các cụm nối bằng một dấu gạch ngang.</summary>
public static partial class LessonSlugRules
{
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    public static partial Regex Pattern();

    public const int MinLength = 3;
    public const int MaxLength = 64;
}
