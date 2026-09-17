using System.Text;
using System.Text.RegularExpressions;

namespace AntFarm.Chinese.Domain.Pinyin;

/// <summary>
/// Phân tích pinyin nhập từ hộp tìm kiếm (F6, §5.2.3) — thuần, dùng <see cref="PinyinText"/> +
/// <see cref="PinyinSyllable"/>. Mọi hàm nhận tham số <c>isValidSyllable</c> TUỲ CHỌN (mặc định
/// <see cref="DefaultIsValidSyllable"/> — kết hợp <see cref="PinyinSyllable.IsValidKey"/> với
/// <see cref="PinyinSyllableTable"/>, xem giải thích ĐIỂM LỆCH ở đó) để Application
/// (<c>DictionaryQueryParser</c>) có thể truyền vào <c>IPinyinCatalog.ContainsSyllable</c> thật khi
/// có sẵn — không bắt buộc, chỉ để dễ kiểm thử/thay thế nguồn âm tiết mà không đổi thuật toán.
/// </summary>
public static partial class PinyinQuery
{
    private const int MaxSyllableLength = 6; // âm tiết pinyin dài nhất (zhuang/shuang/chuang) — chặn DP xét chuỗi vô nghĩa

    private static readonly HashSet<char> MarkedVowelChars = new("āáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜ");
    private static readonly Dictionary<char, (char Base, int Tone)> MarkedVowelInfo = BuildMarkedVowelInfo();

    [GeneratedRegex(@"^([a-zü:v]+[0-5]['\s]*)+$")]
    private static partial Regex NumberedFullPattern();

    [GeneratedRegex(@"[a-zü:v]+[0-5]")]
    private static partial Regex NumberedTokenPattern();

    /// <summary>Kết hợp <see cref="PinyinSyllable.IsValidKey"/> (định dạng ký tự, R5-1) với <see cref="PinyinSyllableTable"/> (âm tiết có thật).</summary>
    public static bool DefaultIsValidSyllable(string key) =>
        PinyinSyllable.IsValidKey(key) && PinyinSyllableTable.IsKnown(key);

    /// <summary>
    /// Pinyin số thanh, một hoặc nhiều âm tiết (vd <c>ni3hao3</c>, <c>ni3 hao3</c>) — <paramref name="l"/>
    /// PHẢI đã <c>ToLowerInvariant()</c>. Trả <see cref="PinyinSyllableTable"/>-compact KHÔNG khoảng
    /// trắng (vd <c>ni3hao3</c>). Có chữ số nhưng không khớp định dạng hoặc âm tiết không có thật ⇒
    /// <c>false</c> (§5.2.3).
    /// </summary>
    public static bool TryParseNumbered(string l, out string? compact, Func<string, bool>? isValidSyllable = null)
    {
        isValidSyllable ??= DefaultIsValidSyllable;
        compact = null;

        if (string.IsNullOrEmpty(l) || !NumberedFullPattern().IsMatch(l))
            return false;

        var sb = new StringBuilder();
        foreach (Match m in NumberedTokenPattern().Matches(l))
        {
            var token = m.Value;
            var digitChar = token[^1];
            var lettersRaw = token[..^1];
            var letters = lettersRaw.Replace("u:", "v", StringComparison.Ordinal).Replace('ü', 'v');

            if (!isValidSyllable(letters))
                return false;

            var digit = digitChar == '0' ? '5' : digitChar;
            sb.Append(letters).Append(digit);
        }

        compact = sb.ToString();
        return true;
    }

    /// <summary>
    /// Pinyin dấu, một hoặc nhiều âm tiết cách nhau bởi khoảng trắng/nháy đơn, HOẶC liền không tách
    /// (vd <c>nǐhǎo</c>, <c>xīān</c>) — <paramref name="l"/> PHẢI đã <c>ToLowerInvariant()</c> và có
    /// ít nhất một nguyên âm mang dấu. Mỗi đoạn (token) liền được <see cref="Segment"/> tách ranh
    /// giới âm tiết CÓ RÀNG BUỘC "mỗi âm tiết tối đa một dấu thanh" (xem giải thích tại
    /// <see cref="PinyinSyllableTable"/> — ràng buộc này BẮT BUỘC để phân biệt "xi"+"an" (2 âm tiết,
    /// mỗi âm tiết một dấu) với "xian" (1 âm tiết — nếu chỉ có một dấu trên 'a'), KHÔNG PHẢI kiểm
    /// sau khi tách xong; nếu chỉ đối chiếu sau, quy hoạch động "ít âm tiết nhất" sẽ luôn chọn
    /// nhầm "xian" làm MỘT âm tiết vì nó vốn có thật trong bảng).
    /// </summary>
    public static bool TryParseToneMarked(string l, out string? compact, Func<string, bool>? isValidSyllable = null)
    {
        isValidSyllable ??= DefaultIsValidSyllable;
        compact = null;

        if (string.IsNullOrEmpty(l))
            return false;

        var hasMark = false;
        foreach (var c in l)
            if (MarkedVowelChars.Contains(c)) { hasMark = true; break; }
        if (!hasMark)
            return false;

        var tokens = l.Split([' ', '\''], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return false;

        var sb = new StringBuilder();
        foreach (var token in tokens)
        {
            if (!TryConvertToneMarkedToken(token, isValidSyllable, out var tokenCompact))
                return false;
            sb.Append(tokenCompact);
        }

        compact = sb.ToString();
        return true;
    }

    /// <summary>Bỏ số/cách/nháy đơn, <c>ü</c> → <c>v</c> — KHÔNG kiểm âm tiết có thật hay không (dùng làm khoá <c>pinyin_search</c>, so khớp chuỗi trực tiếp). <paramref name="l"/> PHẢI đã <c>ToLowerInvariant()</c>.</summary>
    public static string Toneless(string l)
    {
        var sb = new StringBuilder(l.Length);
        foreach (var c in l)
        {
            if (c is ' ' or '\'' || char.IsDigit(c))
                continue;
            sb.Append(c == 'ü' ? 'v' : c);
        }

        return sb.ToString();
    }

    /// <summary>Tách MỘT chuỗi âm tiết liền (không thanh) thành danh sách âm tiết — quy hoạch động, ưu tiên ít âm tiết nhất, hoà thì âm tiết đầu dài nhất (§5.2.3). <c>false</c> nếu không tách được.</summary>
    public static bool Segment(string toneless, out IReadOnlyList<string> syllables, Func<string, bool>? isValidSyllable = null)
    {
        isValidSyllable ??= DefaultIsValidSyllable;

        if (!TrySegmentWithMarks(toneless, [], isValidSyllable, out var withTones))
        {
            syllables = [];
            return false;
        }

        syllables = withTones.Select(x => x.Syllable).ToList();
        return true;
    }

    private static bool TryConvertToneMarkedToken(string token, Func<string, bool> isValidSyllable, out string tokenCompact)
    {
        tokenCompact = "";

        var toneless = new StringBuilder(token.Length);
        var marks = new List<(int Position, int Tone)>();

        foreach (var ch in token)
        {
            if (MarkedVowelInfo.TryGetValue(ch, out var info))
            {
                marks.Add((toneless.Length, info.Tone));
                toneless.Append(info.Base);
            }
            else if (ch == 'ü')
            {
                toneless.Append('v');
            }
            else
            {
                toneless.Append(ch);
            }
        }

        if (!TrySegmentWithMarks(toneless.ToString(), marks, isValidSyllable, out var syllables))
            return false;

        var sb = new StringBuilder();
        foreach (var (syllable, tone) in syllables)
            sb.Append(syllable).Append(tone);

        tokenCompact = sb.ToString();
        return true;
    }

    /// <summary>
    /// Lõi tách âm tiết: quy hoạch động từ CUỐI chuỗi lên đầu, <c>minCount[i]</c> = số âm tiết ít
    /// nhất để tách <c>toneless[i..n)</c>; ứng viên bị loại nếu KHÔNG phải âm tiết có thật
    /// (<paramref name="isValidSyllable"/>) hoặc chứa NHIỀU HƠN MỘT dấu thanh trong <paramref name="marks"/>
    /// (ràng buộc CỨNG — không phải lọc sau). Hoà số âm tiết ⇒ chọn âm tiết DÀI HƠN tại vị trí đó
    /// (áp dụng lặp lại ở MỌI vị trí, không chỉ âm tiết đầu — khái quát hoá tự nhiên của "âm tiết
    /// đầu dài nhất" khi hoà, §5.2.3).
    /// </summary>
    private static bool TrySegmentWithMarks(
        string toneless, IReadOnlyList<(int Position, int Tone)> marks, Func<string, bool> isValidSyllable,
        out List<(string Syllable, int Tone)> result)
    {
        result = [];
        var n = toneless.Length;
        if (n == 0)
            return false;

        var minCount = new int[n + 1];
        var chosenLen = new int[n + 1];
        Array.Fill(minCount, int.MaxValue);
        minCount[n] = 0;

        for (var i = n - 1; i >= 0; i--)
        {
            for (var len = 1; len <= Math.Min(MaxSyllableLength, n - i); len++)
            {
                if (minCount[i + len] == int.MaxValue)
                    continue;

                var marksInSegment = 0;
                foreach (var (position, _) in marks)
                    if (position >= i && position < i + len)
                        marksInSegment++;
                if (marksInSegment > 1)
                    continue; // >1 dấu trong một âm tiết ⇒ không hợp lệ (§5.2.3)

                var sub = toneless.Substring(i, len);
                if (!isValidSyllable(sub))
                    continue;

                var candidateCount = 1 + minCount[i + len];
                if (candidateCount < minCount[i] || (candidateCount == minCount[i] && len > chosenLen[i]))
                {
                    minCount[i] = candidateCount;
                    chosenLen[i] = len;
                }
            }
        }

        if (minCount[0] == int.MaxValue)
            return false;

        var position2 = 0;
        while (position2 < n)
        {
            var len = chosenLen[position2];
            var sub = toneless.Substring(position2, len);
            var tone = 5;
            foreach (var (markPosition, markTone) in marks)
            {
                if (markPosition >= position2 && markPosition < position2 + len)
                {
                    tone = markTone;
                    break;
                }
            }

            result.Add((sub, tone));
            position2 += len;
        }

        return true;
    }

    private static Dictionary<char, (char Base, int Tone)> BuildMarkedVowelInfo()
    {
        (char Ch, char Base, int Tone)[] rows =
        [
            ('ā', 'a', 1), ('á', 'a', 2), ('ǎ', 'a', 3), ('à', 'a', 4),
            ('ē', 'e', 1), ('é', 'e', 2), ('ě', 'e', 3), ('è', 'e', 4),
            ('ī', 'i', 1), ('í', 'i', 2), ('ǐ', 'i', 3), ('ì', 'i', 4),
            ('ō', 'o', 1), ('ó', 'o', 2), ('ǒ', 'o', 3), ('ò', 'o', 4),
            ('ū', 'u', 1), ('ú', 'u', 2), ('ǔ', 'u', 3), ('ù', 'u', 4),
            ('ǖ', 'v', 1), ('ǘ', 'v', 2), ('ǚ', 'v', 3), ('ǜ', 'v', 4)
        ];

        var map = new Dictionary<char, (char, int)>();
        foreach (var (ch, baseChar, tone) in rows)
            map[ch] = (baseChar, tone);

        return map;
    }
}
