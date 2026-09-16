using System.Text;
using System.Text.RegularExpressions;

namespace AntFarm.Chinese.Domain.Pinyin;

/// <summary>
/// Tiện ích thuần thao tác chuỗi pinyin (§5.2.1) — nguồn sự thật lưu trữ là pinyin SỐ THANH
/// (<c>ni3 hao3</c>), dấu chỉ dùng khi HIỂN THỊ. Dùng ở F5 để kiểm dữ liệu học liệu lúc nạp
/// (<c>PinyinCatalogLoader</c>) và F6 để tra từ. Không I/O, không phụ thuộc gì ngoài BCL — test
/// trực tiếp không cần DB/HTTP.
/// </summary>
public static partial class PinyinText
{
    [GeneratedRegex(@"^[A-Za-z]+[1-5]$")]
    private static partial Regex NumberedTokenPattern();

    [GeneratedRegex(@"^([A-Za-z]+)([1-5])$")]
    private static partial Regex SyllableTokenPattern();

    // (nguyên âm mang dấu, [nguyên âm gốc, thanh 1..4]) — cả chữ thường lẫn chữ hoa.
    private static readonly Dictionary<char, (char Base, int Tone)> MarkedVowels = BuildMarkedVowels();

    // Bảng đặt dấu theo §5.3.C — khoá là ký tự SAU KHI đã đổi v→ü (ToMarkedToken đổi trước khi tìm
    // nguyên âm để đặt dấu), nên khoá ở đây là 'ü' chứ không phải 'v'.
    private static readonly Dictionary<char, string[]> ToneTable = new()
    {
        ['a'] = ["ā", "á", "ǎ", "à"],
        ['e'] = ["ē", "é", "ě", "è"],
        ['i'] = ["ī", "í", "ǐ", "ì"],
        ['o'] = ["ō", "ó", "ǒ", "ò"],
        ['u'] = ["ū", "ú", "ǔ", "ù"],
        ['ü'] = ["ǖ", "ǘ", "ǚ", "ǜ"]
    };

    /// <summary>
    /// Chuẩn hoá pinyin số (có thể nhiều âm tiết cách nhau bởi khoảng trắng): gộp khoảng trắng
    /// thừa, quy <c>ü</c>/<c>u:</c>/<c>U:</c>/<c>Ü</c> về <c>v</c> thường (GIỮ nguyên hoa/thường
    /// các ký tự còn lại), rồi kiểm mỗi token khớp <c>^[A-Za-z]+[1-5]$</c>. Trả <c>null</c> nếu
    /// rỗng hoặc bất kỳ token nào sai định dạng — KHÔNG ném (dữ liệu người dùng/học liệu không
    /// đáng tin, gọi nơi khác tự quyết định báo lỗi thế nào).
    /// </summary>
    public static string? NormalizeNumbered(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var collapsed = CollapseWhitespace(input.Trim());
        var tokens = collapsed.Split(' ');
        var normalized = new string[tokens.Length];

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = ReplaceUmlautWithV(tokens[i]);
            if (!NumberedTokenPattern().IsMatch(token))
                return null;

            normalized[i] = token;
        }

        return string.Join(' ', normalized);
    }

    /// <summary>
    /// Chuyển pinyin dấu (có thể tách nhiều âm tiết bằng khoảng trắng hoặc dấu nháy đơn kiểu
    /// <c>Xī'ān</c>) sang pinyin số. Âm tiết không có dấu ⇒ thanh nhẹ (5). Trả <c>null</c> nếu
    /// rỗng.
    /// </summary>
    public static string? FromToneMarks(string marked)
    {
        if (string.IsNullOrWhiteSpace(marked))
            return null;

        var tokens = marked.Trim().Split([' ', '\''], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return null;

        var results = new string[tokens.Length];
        for (var i = 0; i < tokens.Length; i++)
        {
            var converted = ConvertMarkedToken(tokens[i]);
            if (converted is null)
                return null;

            results[i] = converted;
        }

        return string.Join(' ', results);
    }

    /// <summary>Bỏ số thanh + khoảng trắng, chuyển chữ thường — dùng làm khoá tra tìm (vd tìm từ vựng bỏ qua thanh, F6).</summary>
    public static string ToSearchKey(string numbered)
    {
        var noDigits = DigitPattern().Replace(numbered, "");
        var noSpaces = WhitespacePattern().Replace(noDigits, "");
        return noSpaces.ToLowerInvariant();
    }

    [GeneratedRegex("[0-9]")]
    private static partial Regex DigitPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    /// <summary>Tách MỘT token pinyin số (vd <c>lv4</c>) thành âm tiết (chữ thường) + thanh. <c>false</c> nếu không khớp <c>^[A-Za-z]+[1-5]$</c>.</summary>
    public static bool TryParseSyllable(string token, out string syllable, out int tone)
    {
        var match = SyllableTokenPattern().Match(token);
        if (!match.Success)
        {
            syllable = "";
            tone = 0;
            return false;
        }

        syllable = match.Groups[1].Value.ToLowerInvariant();
        tone = int.Parse(match.Groups[2].Value);
        return true;
    }

    /// <summary>
    /// Đặt dấu cho pinyin số (một hoặc nhiều âm tiết cách nhau bằng khoảng trắng) theo §5.3.C.
    /// Token không hợp lệ (không parse được) ⇒ GIỮ NGUYÊN token đó, không ném.
    /// </summary>
    public static string ToMarked(string numbered)
    {
        if (string.IsNullOrWhiteSpace(numbered))
            return numbered;

        var tokens = numbered.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', tokens.Select(ToMarkedToken));
    }

    private static string ToMarkedToken(string token)
    {
        var match = SyllableTokenPattern().Match(token);
        if (!match.Success)
            return token; // token sai giữ nguyên

        var letters = match.Groups[1].Value; // GIỮ hoa/thường gốc
        var tone = int.Parse(match.Groups[2].Value);

        // v/V luôn hiển thị ü/Ü — làm TRƯỚC khi tìm nguyên âm để đặt dấu, vì ü/ü cũng có thể là
        // nguyên âm được chọn (vd "nv3" → "nǚ").
        var chars = letters.Select(c => c switch
        {
            'v' => 'ü',
            'V' => 'Ü',
            _ => c
        }).ToArray();

        if (tone == 5)
            return new string(chars); // thanh nhẹ: không dấu, nhưng vẫn đổi v→ü

        var index = FindMarkIndex(chars);
        if (index < 0)
            return new string(chars); // m/n/ng/r... không có nguyên âm — không ném

        var isUpper = char.IsUpper(chars[index]);
        var baseLower = char.ToLowerInvariant(chars[index]);
        var markedChar = ToneTable.TryGetValue(baseLower, out var forms) ? forms[tone - 1][0] : baseLower;
        chars[index] = isUpper ? char.ToUpperInvariant(markedChar) : markedChar;

        return new string(chars);
    }

    /// <summary>Quy tắc chọn nguyên âm mang dấu (§5.3.C): có 'a' ⇒ a; không 'a' mà có 'e' ⇒ e; có "ou" ⇒ o; còn lại ⇒ nguyên âm cuối.</summary>
    private static int FindMarkIndex(char[] chars)
    {
        var lower = new string(chars).ToLowerInvariant();

        var aIndex = lower.IndexOf('a');
        if (aIndex >= 0)
            return aIndex;

        var eIndex = lower.IndexOf('e');
        if (eIndex >= 0)
            return eIndex;

        var ouIndex = lower.IndexOf("ou", StringComparison.Ordinal);
        if (ouIndex >= 0)
            return ouIndex; // đặt dấu lên 'o' của "ou"

        for (var i = lower.Length - 1; i >= 0; i--)
            if ("iouü".IndexOf(lower[i]) >= 0)
                return i;

        return -1;
    }

    private static string CollapseWhitespace(string input) => WhitespacePattern().Replace(input, " ");

    private static string ReplaceUmlautWithV(string token) =>
        token
            .Replace("u:", "v", StringComparison.Ordinal)
            .Replace("U:", "v", StringComparison.Ordinal)
            .Replace('ü', 'v')
            .Replace('Ü', 'v');

    private static string? ConvertMarkedToken(string token)
    {
        var tone = 5;
        var sb = new StringBuilder(token.Length);

        foreach (var ch in token)
        {
            if (MarkedVowels.TryGetValue(ch, out var info))
            {
                tone = info.Tone;
                sb.Append(info.Base);
            }
            else if (ch == 'ü')
                sb.Append('v');
            else if (ch == 'Ü')
                sb.Append('V');
            else
                sb.Append(ch);
        }

        return sb.Length == 0 ? null : sb.ToString() + tone;
    }

    private static Dictionary<char, (char Base, int Tone)> BuildMarkedVowels()
    {
        // (thường, hoa) cho mỗi thanh 1..4 của a/e/i/o/u/ü — ü mang dấu quy về 'v' (nguồn sự thật số thanh).
        (char Lower, char Upper)[] rows =
        [
            ('ā', 'Ā'), ('á', 'Á'), ('ǎ', 'Ǎ'), ('à', 'À'),
            ('ē', 'Ē'), ('é', 'É'), ('ě', 'Ě'), ('è', 'È'),
            ('ī', 'Ī'), ('í', 'Í'), ('ǐ', 'Ǐ'), ('ì', 'Ì'),
            ('ō', 'Ō'), ('ó', 'Ó'), ('ǒ', 'Ǒ'), ('ò', 'Ò'),
            ('ū', 'Ū'), ('ú', 'Ú'), ('ǔ', 'Ǔ'), ('ù', 'Ù'),
            ('ǖ', 'Ǖ'), ('ǘ', 'Ǘ'), ('ǚ', 'Ǚ'), ('ǜ', 'Ǜ')
        ];
        char[] bases = ['a', 'a', 'a', 'a', 'e', 'e', 'e', 'e', 'i', 'i', 'i', 'i', 'o', 'o', 'o', 'o', 'u', 'u', 'u', 'u', 'v', 'v', 'v', 'v'];
        int[] tones = [1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4];

        var map = new Dictionary<char, (char, int)>();
        for (var i = 0; i < rows.Length; i++)
        {
            map[rows[i].Lower] = (bases[i], tones[i]);
            map[rows[i].Upper] = (char.ToUpperInvariant(bases[i]), tones[i]);
        }

        return map;
    }
}
