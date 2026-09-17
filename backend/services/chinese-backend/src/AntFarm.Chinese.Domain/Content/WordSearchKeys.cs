using System.Text;
using AntFarm.Chinese.Domain.Text;

namespace AntFarm.Chinese.Domain.Content;

/// <summary>Hàm thuần dựng khoá tìm kiếm của <see cref="Word"/> (§5.2.2) — dùng lúc GHI (Word.RecomputeSearchKeys) và TRA CỨU (DictionaryQueryParser dùng cùng công thức cho phía truy vấn).</summary>
public static class WordSearchKeys
{
    /// <summary>'Bei3 jing1' → 'bei3jing1' (lower, bỏ cách, ü/u: → v, thanh 0 → 5, GIỮ số).</summary>
    public static string PinyinCompact(string pinyin) => NormalizeCompact(pinyin, keepDigits: true);

    /// <summary>'Bei3 jing1' → 'beijing' (như trên rồi BỎ số).</summary>
    public static string PinyinToneless(string pinyin) => NormalizeCompact(pinyin, keepDigits: false);

    /// <summary>Dựng <c>search_vi</c>/<c>search_vi_plain</c> (§5.2.2) — tách nghĩa theo <c>; , /</c>, chuẩn hoá, kèm Hán Việt, bọc <c>| ... |</c> để LIKE khớp trọn một cụm.</summary>
    public static (string WithDiacritics, string Plain) BuildSearchVi(IReadOnlyList<string> meaningsVi, string? hanViet)
    {
        var terms = new List<string>();

        foreach (var meaning in meaningsVi)
            foreach (var part in meaning.Split([';', ',', '/']))
            {
                var normalized = VietnameseText.NormalizeForSearch(part);
                if (normalized.Length > 0)
                    terms.Add(normalized);
            }

        var hanVietNormalized = VietnameseText.NormalizeForSearch(hanViet ?? "");
        if (hanVietNormalized.Length > 0)
            terms.Add(hanVietNormalized);

        var distinct = terms.Distinct().ToList();
        var withDiacritics = "| " + string.Join(" | ", distinct) + " |";
        var plain = VietnameseText.RemoveDiacritics(withDiacritics);
        return (withDiacritics, plain);
    }

    private static string NormalizeCompact(string pinyin, bool keepDigits)
    {
        var lower = pinyin.ToLowerInvariant().Replace("u:", "v", StringComparison.Ordinal);
        var sb = new StringBuilder(lower.Length);

        foreach (var c in lower)
        {
            if (c is ' ' or ':')
                continue;
            if (c == 'ü')
            {
                sb.Append('v');
                continue;
            }
            if (char.IsDigit(c))
            {
                if (!keepDigits)
                    continue;
                sb.Append(c == '0' ? '5' : c);
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
