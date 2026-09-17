using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AntFarm.Chinese.Domain.Text;

/// <summary>
/// Tiện ích thuần thao tác chuỗi tiếng Việt dùng để dựng khoá tìm kiếm (F6, §5.2.2) — không I/O,
/// test trực tiếp không cần DB. Dùng ở cả lúc GHI (Word.RecomputeSearchKeys) lẫn lúc TRUY VẤN
/// (DictionaryQueryParser) — CÙNG một hàm để đảm bảo so khớp nhất quán (§5.1.1: không dùng hàm
/// <c>unaccent()</c> của Postgres vì không <c>IMMUTABLE</c>, không đánh chỉ mục trực tiếp được).
/// </summary>
public static partial class VietnameseText
{
    /// <summary>
    /// Bỏ dấu tiếng Việt: <c>đ/Đ</c> KHÔNG phân rã được bằng NFD (là một chữ cái độc lập trong
    /// Unicode, không phải tổ hợp chữ cái + dấu) nên phải thay tay TRƯỚC khi NFD; sau đó NFD rồi bỏ
    /// mọi <see cref="UnicodeCategory.NonSpacingMark"/> (dấu thanh/dấu mũ tách rời), cuối cùng NFC
    /// lại cho gọn.
    /// </summary>
    public static string RemoveDiacritics(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        var withoutDStroke = s.Replace('đ', 'd').Replace('Đ', 'D');
        var decomposed = withoutDStroke.Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Chuẩn hoá một nghĩa/cụm tiếng Việt để làm khoá tìm kiếm (§5.2.2): NFC, lower bất biến văn
    /// hoá (không dùng <c>vi-VN</c> culture — tránh khác nhau giữa máy chủ/hệ điều hành), BỎ nội
    /// dung trong ngoặc tròn (chú thích từ loại, vd "(trợ từ)"), thay ký tự không phải
    /// chữ/số bằng khoảng trắng, gộp khoảng trắng thừa, trim.
    /// </summary>
    public static string NormalizeForSearch(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        var nfc = s.Normalize(NormalizationForm.FormC);
        var lower = nfc.ToLowerInvariant();
        var withoutParens = ParenthesesPattern().Replace(lower, "");

        var sb = new StringBuilder(withoutParens.Length);
        foreach (var c in withoutParens)
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');

        var collapsed = WhitespacePattern().Replace(sb.ToString(), " ");
        return collapsed.Trim();
    }

    /// <summary>So sánh <paramref name="s"/> (ĐÃ chuẩn hoá — vd qua <see cref="NormalizeForSearch"/>) với bản bỏ dấu của chính nó.</summary>
    public static bool HasDiacritics(string s) => !string.Equals(RemoveDiacritics(s), s, StringComparison.Ordinal);

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex ParenthesesPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
