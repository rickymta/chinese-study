using System.Text;
using System.Text.RegularExpressions;
using AntFarm.Chinese.Application.Pinyin;
using AntFarm.Chinese.Domain.Pinyin;
using AntFarm.Chinese.Domain.Text;

namespace AntFarm.Chinese.Application.Dictionary;

public enum DictionaryQueryKind { Empty, Hanzi, Latin }

/// <summary>Kết quả phân tích một truy vấn tra từ (§5.2.3).</summary>
public sealed record ParsedDictionaryQuery(
    DictionaryQueryKind Kind,
    string Raw,
    string? HanziText,
    string? PinyinCompact,
    string? PinyinToneless,
    string? ViText,
    string? ViPlain,
    bool ViHasDiacritics);

/// <summary>
/// Phân tích truy vấn tra từ (§5.2.3) — Hán tự, pinyin (số/dấu/không thanh), tiếng Việt có/không
/// dấu, Hán Việt. Nhận <see cref="IPinyinCatalog"/> để tách ranh giới âm tiết bằng bảng âm tiết
/// THẬT (<c>ContainsSyllable</c>) thay vì chỉ <see cref="PinyinSyllable.IsValidKey"/> (định dạng) —
/// xem giải thích đầy đủ ở <see cref="PinyinSyllableTable"/> (Domain). Catalog chưa sẵn sàng
/// (<see cref="IPinyinCatalog.IsAvailable"/> = false) ⇒ lùi về <see cref="PinyinSyllableTable"/>
/// nhúng cứng — suy giảm nhẹ độ chính xác tách âm tiết liền (§5.2.3 "nǐhǎo") nhưng KHÔNG chặn tra
/// từ theo chữ Hán/pinyin số/tiếng Việt.
/// </summary>
public sealed partial class DictionaryQueryParser(IPinyinCatalog catalog)
{
    [GeneratedRegex(@"[\p{IsCJKUnifiedIdeographs}\p{IsCJKUnifiedIdeographsExtensionA}〇]")]
    private static partial Regex HanziCharPattern();

    [GeneratedRegex(@"^[a-züv'\s]+$")]
    private static partial Regex TonelessLetterPattern();

    private const string MarkedVowelChars = "āáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜ";

    [GeneratedRegex(@"^[a-züv'\sāáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜ]+$")]
    private static partial Regex ToneMarkedLetterPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    /// <summary>NFC → trim → gộp khoảng trắng (R6-21) — dùng cả để validate độ dài lẫn để phân tích.</summary>
    public static string Normalize(string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return "";

        var nfc = q.Normalize(NormalizationForm.FormC).Trim();
        return WhitespacePattern().Replace(nfc, " ");
    }

    public ParsedDictionaryQuery Parse(string? q)
    {
        var normalized = Normalize(q);
        if (normalized.Length == 0)
            return new ParsedDictionaryQuery(DictionaryQueryKind.Empty, "", null, null, null, null, null, false);

        if (HanziCharPattern().IsMatch(normalized))
        {
            var hanziText = WhitespacePattern().Replace(normalized, "");
            return new ParsedDictionaryQuery(DictionaryQueryKind.Hanzi, normalized, hanziText, null, null, null, null, false);
        }

        var l = normalized.ToLowerInvariant();
        var hasDigit = l.Any(char.IsDigit);
        var hasMark = l.Any(c => MarkedVowelChars.Contains(c));

        string? pinyinCompact = null;
        if (hasDigit && PinyinQuery.TryParseNumbered(l, out var numberedCompact, IsKnownSyllable))
        {
            pinyinCompact = numberedCompact;
        }
        else if (hasMark && ToneMarkedLetterPattern().IsMatch(l) && PinyinQuery.TryParseToneMarked(l, out var markedCompact, IsKnownSyllable))
        {
            pinyinCompact = markedCompact;
        }

        string? pinyinToneless = null;
        if (!hasDigit && !hasMark && TonelessLetterPattern().IsMatch(l))
            pinyinToneless = PinyinQuery.Toneless(l);

        var viText = VietnameseText.NormalizeForSearch(normalized);
        var viPlain = VietnameseText.RemoveDiacritics(viText);
        var viHasDiacritics = viText != viPlain;

        return new ParsedDictionaryQuery(DictionaryQueryKind.Latin, normalized, null, pinyinCompact, pinyinToneless, viText, viPlain, viHasDiacritics);
    }

    private bool IsKnownSyllable(string key) =>
        PinyinSyllable.IsValidKey(key)
        && (key == "r" || (catalog.IsAvailable ? catalog.ContainsSyllable(key) : PinyinSyllableTable.IsKnown(key)));
}
