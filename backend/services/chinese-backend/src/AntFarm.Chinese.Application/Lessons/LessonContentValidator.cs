using System.Text.Json;
using System.Text.RegularExpressions;
using AntFarm.Chinese.Domain.Lessons;

namespace AntFarm.Chinese.Application.Lessons;

/// <summary>Một lỗi hợp lệ hoá — <see cref="Path"/> chỉ mang tính tham khảo (vd "blocks[2].payload.lines[0].pinyin"), không phải khoá máy đọc.</summary>
public sealed record ValidationProblem(string Path, string Message);

/// <summary>Một lựa chọn đã kiểm sẵn cho <see cref="LessonContentValidator.ValidateQuestion"/> (Application, tách khỏi entity <c>QuizQuestion</c> để không phải parse lại jsonb).</summary>
public sealed record QuestionValidationInput(
    string Type, string Prompt, string PromptLang, string? PromptPinyin, string? AudioText,
    IReadOnlyList<QuizOption> Options, string CorrectOptionId, string Explanation);

/// <summary>
/// Kiểm nội dung bài học (§5.2.1.2, §5.4.3) — dùng chung cho <c>LessonImporter</c> (F9, bài nạp từ
/// tệp) và quản trị nội dung (F10, chưa triển khai). CHỈ kiểm CẤU TRÚC/CÚ PHÁP (không đối chiếu bảng
/// âm tiết pinyin đầy đủ hay <c>hsk-words.json</c> — những luật đó đã có ở
/// <c>content/chinese/scripts/validate.mjs</c> cho học liệu seed; <c>LessonImporter</c> tự đối chiếu
/// <c>content.words</c> khi tra từ, R-LS15). Thuần, không I/O — test trực tiếp.
/// </summary>
public static partial class LessonContentValidator
{
    private static readonly string[] AllowedTipVariants = ["pronunciation", "culture", "memory", "grammar"];
    private static readonly string[] AllowedOptionLangs = ["vi", "zh", "pinyin"];
    private static readonly string[] AllowedPromptLangs = ["vi", "zh"];

    [GeneratedRegex("[A-Za-z]")]
    private static partial Regex LatinPattern();

    [GeneratedRegex(@"(\p{IsCJKUnifiedIdeographs}|\p{IsCJKUnifiedIdeographsExtensionA})")]
    private static partial Regex HanCharPattern();

    [GeneratedRegex(@"^(\p{IsCJKUnifiedIdeographs}|\p{IsCJKUnifiedIdeographsExtensionA})+$")]
    private static partial Regex HanziOnlyPattern();

    // Dấu câu Trung + ASCII tương ứng cho phép trong hanzi/audioText (§5.4.3, khớp validate.mjs PUNCT_CLASS).
    [GeneratedRegex("^[\\s\\p{IsCJKUnifiedIdeographs}\\p{IsCJKUnifiedIdeographsExtensionA}，。！？、：；“”‘’…,.!?:;'\"\\-]+$")]
    private static partial Regex HanziLinePattern();

    [GeneratedRegex("[，。！？、：；“”‘’…,.!?:;'\"\\-]")]
    private static partial Regex PunctuationPattern();

    [GeneratedRegex(@"^([A-Za-z]+)([1-5])$")]
    private static partial Regex PinyinTokenPattern();

    [GeneratedRegex(@"\[\[([^\[\]|]*)\|([^\[\]]*)\]\]")]
    private static partial Regex InlineTokenPattern();

    // ---- cặp hanzi/pinyin (dòng hội thoại, ví dụ ngữ pháp, promptPinyin) ----

    /// <summary>Kiểm một cặp (hanzi, pinyin) — dòng hội thoại/ví dụ ngữ pháp (§5.4.3): hanzi chỉ chữ Hán + dấu câu, số âm tiết pinyin (bỏ dấu câu) khớp số chữ Hán, mỗi âm tiết đúng cú pháp số thanh.</summary>
    public static IReadOnlyList<ValidationProblem> ValidateHanziPinyinPair(string path, string hanzi, string pinyin)
    {
        var problems = new List<ValidationProblem>();

        if (string.IsNullOrEmpty(hanzi))
        {
            problems.Add(new ValidationProblem(path, "hanzi không được rỗng."));
            return problems;
        }

        var hanziWithoutPunct = PunctuationPattern().Replace(hanzi, "");
        if (LatinPattern().IsMatch(hanziWithoutPunct))
        {
            problems.Add(new ValidationProblem(path, $"hanzi '{hanzi}' chứa chữ Latin (chỉ được chữ Hán + dấu câu)."));
            return problems;
        }

        if (!HanziLinePattern().IsMatch(hanzi))
        {
            problems.Add(new ValidationProblem(path, $"hanzi '{hanzi}' chứa ký tự không hợp lệ (chỉ được chữ Hán + dấu câu)."));
            return problems;
        }

        var hanziCount = HanCharPattern().Matches(hanzi).Count;
        var tokens = PunctuationPattern().Replace(pinyin ?? "", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != hanziCount)
        {
            problems.Add(new ValidationProblem(path, $"số âm tiết pinyin ({tokens.Length}) khác số chữ Hán ({hanziCount}) trong '{hanzi}'/'{pinyin}'."));
            return problems;
        }

        foreach (var token in tokens)
            if (!PinyinTokenPattern().IsMatch(token))
                problems.Add(new ValidationProblem(path, $"âm tiết '{token}' (trong '{pinyin}') không đúng cú pháp (chữ cái + số thanh 1-5)."));

        return problems;
    }

    /// <summary>Kiểm cú pháp chữ Hán nội dòng <c>[[hanzi|pinyin]]</c> trong một đoạn văn bản tự do (§5.4.3) — vd trong <c>paragraphs</c>, <c>explanation</c>, <c>pattern</c>, <c>tip.text</c>.</summary>
    public static IReadOnlyList<ValidationProblem> ValidateInlineTokens(string path, string? text)
    {
        var problems = new List<ValidationProblem>();
        if (string.IsNullOrEmpty(text))
            return problems;

        var stripped = text;
        foreach (Match match in InlineTokenPattern().Matches(text))
        {
            stripped = stripped.Replace(match.Value, "", StringComparison.Ordinal);

            var hanziPart = match.Groups[1].Value;
            var pinyinPart = match.Groups[2].Value;
            var hanziRuneCount = hanziPart.EnumerateRunes().Count();

            if (hanziRuneCount is 0 or > 10 || !HanziOnlyPattern().IsMatch(hanziPart))
            {
                problems.Add(new ValidationProblem(path, $"token nội dòng '{match.Value}' có phần chữ Hán không hợp lệ (phải là 1–10 chữ Hán)."));
                continue;
            }

            var tokens = pinyinPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != hanziRuneCount)
            {
                problems.Add(new ValidationProblem(path, $"token nội dòng '{match.Value}' — số âm tiết ({tokens.Length}) khác số chữ Hán ({hanziRuneCount})."));
                continue;
            }

            foreach (var token in tokens)
                if (!PinyinTokenPattern().IsMatch(token))
                    problems.Add(new ValidationProblem(path, $"token nội dòng '{match.Value}' — âm tiết '{token}' không hợp lệ."));
        }

        // '[[' hoặc ']]' còn sót lại sau khi đã bóc hết token khớp mẫu ⇒ cú pháp hỏng (thiếu '|' hoặc ']]').
        if (stripped.Contains("[[", StringComparison.Ordinal) || stripped.Contains("]]", StringComparison.Ordinal))
            problems.Add(new ValidationProblem(path, $"cú pháp nội dòng [[...|...]] hỏng (thiếu '|' hoặc ']]') trong: '{text}'."));

        return problems;
    }

    // ---- khối nội dung ----

    /// <summary>Kiểm một khối bài học theo <see cref="LessonBlockTypes"/> (§5.4.3).</summary>
    public static IReadOnlyList<ValidationProblem> ValidateBlock(string type, JsonElement payload, string path = "payload")
    {
        return type switch
        {
            LessonBlockTypes.Text => ValidateTextPayload(payload, path),
            LessonBlockTypes.Dialogue => ValidateDialoguePayload(payload, path),
            LessonBlockTypes.Grammar => ValidateGrammarPayload(payload, path),
            LessonBlockTypes.Tip => ValidateTipPayload(payload, path),
            _ => [new ValidationProblem(path, $"Loại khối '{type}' không hợp lệ.")]
        };
    }

    private static IReadOnlyList<ValidationProblem> ValidateTextPayload(JsonElement payload, string path)
    {
        var problems = new List<ValidationProblem>();
        var paragraphs = GetStringArray(payload, "paragraphs");
        if (paragraphs.Count is 0 or > 10)
            problems.Add(new ValidationProblem(path, "text.paragraphs phải có 1–10 mục."));

        for (var i = 0; i < paragraphs.Count; i++)
        {
            var paragraph = paragraphs[i];
            if (string.IsNullOrEmpty(paragraph) || paragraph.Length > 2000)
                problems.Add(new ValidationProblem($"{path}.paragraphs[{i}]", "mỗi đoạn văn phải 1–2000 ký tự."));

            problems.AddRange(ValidateInlineTokens($"{path}.paragraphs[{i}]", paragraph));
        }

        return problems;
    }

    private static IReadOnlyList<ValidationProblem> ValidateDialoguePayload(JsonElement payload, string path)
    {
        var problems = new List<ValidationProblem>();

        var title = GetString(payload, "title");
        if (title is { Length: > 100 })
            problems.Add(new ValidationProblem($"{path}.title", "dialogue.title tối đa 100 ký tự."));

        if (!payload.TryGetProperty("lines", out var linesElement) || linesElement.ValueKind != JsonValueKind.Array)
        {
            problems.Add(new ValidationProblem($"{path}.lines", "dialogue.lines bắt buộc là mảng."));
            return problems;
        }

        var lines = linesElement.EnumerateArray().ToList();
        if (lines.Count is < 2 or > 20)
            problems.Add(new ValidationProblem($"{path}.lines", "dialogue.lines phải có 2–20 dòng."));

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var linePath = $"{path}.lines[{i}]";
            var speaker = GetString(line, "speaker") ?? "";
            var hanzi = GetString(line, "hanzi") ?? "";
            var pinyin = GetString(line, "pinyin") ?? "";
            var vi = GetString(line, "vi") ?? "";

            if (speaker.Length is 0 or > 20)
                problems.Add(new ValidationProblem($"{linePath}.speaker", "speaker phải 1–20 ký tự."));
            if (hanzi.Length is 0 or > 100)
                problems.Add(new ValidationProblem($"{linePath}.hanzi", "hanzi phải 1–100 ký tự."));
            if (vi.Length is 0 or > 300)
                problems.Add(new ValidationProblem($"{linePath}.vi", "vi phải 1–300 ký tự."));

            problems.AddRange(ValidateHanziPinyinPair($"{linePath}.pinyin", hanzi, pinyin));
        }

        return problems;
    }

    private static IReadOnlyList<ValidationProblem> ValidateGrammarPayload(JsonElement payload, string path)
    {
        var problems = new List<ValidationProblem>();

        var title = GetString(payload, "title") ?? "";
        if (title.Length is 0 or > 100)
            problems.Add(new ValidationProblem($"{path}.title", "grammar.title phải 1–100 ký tự."));

        var pattern = GetString(payload, "pattern");
        if (pattern is { Length: > 200 })
            problems.Add(new ValidationProblem($"{path}.pattern", "grammar.pattern tối đa 200 ký tự."));
        problems.AddRange(ValidateInlineTokens($"{path}.pattern", pattern));

        var explanation = GetString(payload, "explanation") ?? "";
        if (explanation.Length is 0 or > 2000)
            problems.Add(new ValidationProblem($"{path}.explanation", "grammar.explanation phải 1–2000 ký tự."));
        problems.AddRange(ValidateInlineTokens($"{path}.explanation", explanation));

        if (!payload.TryGetProperty("examples", out var examplesElement) || examplesElement.ValueKind != JsonValueKind.Array)
        {
            problems.Add(new ValidationProblem($"{path}.examples", "grammar.examples bắt buộc là mảng."));
            return problems;
        }

        var examples = examplesElement.EnumerateArray().ToList();
        if (examples.Count is 0 or > 6)
            problems.Add(new ValidationProblem($"{path}.examples", "grammar.examples phải có 1–6 mục."));

        for (var i = 0; i < examples.Count; i++)
        {
            var example = examples[i];
            var examplePath = $"{path}.examples[{i}]";
            var hanzi = GetString(example, "hanzi") ?? "";
            var pinyin = GetString(example, "pinyin") ?? "";
            var vi = GetString(example, "vi") ?? "";
            var note = GetString(example, "note");

            if (hanzi.Length is 0 or > 100)
                problems.Add(new ValidationProblem($"{examplePath}.hanzi", "hanzi phải 1–100 ký tự."));
            if (vi.Length is 0 or > 300)
                problems.Add(new ValidationProblem($"{examplePath}.vi", "vi phải 1–300 ký tự."));
            if (note is { Length: > 200 })
                problems.Add(new ValidationProblem($"{examplePath}.note", "note tối đa 200 ký tự."));

            problems.AddRange(ValidateHanziPinyinPair($"{examplePath}.pinyin", hanzi, pinyin));
        }

        return problems;
    }

    private static IReadOnlyList<ValidationProblem> ValidateTipPayload(JsonElement payload, string path)
    {
        var problems = new List<ValidationProblem>();

        var text = GetString(payload, "text") ?? "";
        if (text.Length is 0 or > 1000)
            problems.Add(new ValidationProblem($"{path}.text", "tip.text phải 1–1000 ký tự."));
        problems.AddRange(ValidateInlineTokens($"{path}.text", text));

        var variant = GetString(payload, "variant");
        if (variant is not null && !AllowedTipVariants.Contains(variant, StringComparer.Ordinal))
            problems.Add(new ValidationProblem($"{path}.variant", $"tip.variant '{variant}' không hợp lệ (pronunciation|culture|memory|grammar)."));

        return problems;
    }

    // ---- câu hỏi quiz ----

    /// <summary>Kiểm một câu quiz (§5.4.3, R-CA4) — KHÔNG kiểm số lượng câu/tỉ lệ nghe của CẢ BÀI (đó là việc của <c>LessonPublishRules</c>, cần nhìn toàn bộ danh sách câu).</summary>
    /// <param name="requireExplanation">
    /// <c>true</c> (mặc định — file seed, luật §5.4.3): <c>explanation</c> bắt buộc 1–500. <c>false</c> (API admin F10,
    /// §6.3 <c>explanation?</c>): được để rỗng — kết quả quiz chỉ không hiện lời giải; có thì vẫn ≤ 500 + kiểm cú pháp nội dòng.
    /// </param>
    public static IReadOnlyList<ValidationProblem> ValidateQuestion(QuestionValidationInput input, string path = "question", bool requireExplanation = true)
    {
        var problems = new List<ValidationProblem>();

        if (!QuizQuestionTypes.IsKnown(input.Type))
            problems.Add(new ValidationProblem($"{path}.type", $"type '{input.Type}' không hợp lệ."));

        if (string.IsNullOrEmpty(input.Prompt) || input.Prompt.Length > 300)
            problems.Add(new ValidationProblem($"{path}.prompt", "prompt phải 1–300 ký tự."));

        if (!AllowedPromptLangs.Contains(input.PromptLang, StringComparer.Ordinal))
            problems.Add(new ValidationProblem($"{path}.promptLang", $"promptLang '{input.PromptLang}' không hợp lệ (vi|zh)."));

        if (input.PromptPinyin is not null)
        {
            if (input.PromptLang != "zh")
                problems.Add(new ValidationProblem($"{path}.promptPinyin", "promptPinyin chỉ hợp lệ khi promptLang='zh'."));
            else
                problems.AddRange(ValidateHanziPinyinPair($"{path}.promptPinyin", input.Prompt, input.PromptPinyin));
        }

        if (input.Type == QuizQuestionTypes.ListenChoice)
        {
            if (string.IsNullOrEmpty(input.AudioText))
                problems.Add(new ValidationProblem($"{path}.audioText", "listen_choice bắt buộc phải có audioText."));
            else if (input.AudioText.Length > 100 || LatinPattern().IsMatch(PunctuationPattern().Replace(input.AudioText, "")) || !HanziLinePattern().IsMatch(input.AudioText))
                problems.Add(new ValidationProblem($"{path}.audioText", $"audioText '{input.AudioText}' phải 1–100 ký tự, toàn chữ Hán (+ dấu câu)."));
        }
        else if (!string.IsNullOrEmpty(input.AudioText))
        {
            problems.Add(new ValidationProblem($"{path}.audioText", "single_choice không được có audioText."));
        }

        if (input.Options.Count is < 2 or > 4)
        {
            problems.Add(new ValidationProblem($"{path}.options", "options phải có 2–4 lựa chọn."));
        }
        else
        {
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            var seenTexts = new HashSet<string>(StringComparer.Ordinal);
            foreach (var option in input.Options)
            {
                var optionPath = $"{path}.options[{option.Id}]";
                if (!seenIds.Add(option.Id))
                    problems.Add(new ValidationProblem(optionPath, $"id lựa chọn '{option.Id}' trùng trong cùng câu."));
                if (string.IsNullOrEmpty(option.Text) || option.Text.Length > 200)
                    problems.Add(new ValidationProblem(optionPath, "text lựa chọn phải 1–200 ký tự."));
                if (!seenTexts.Add(option.Text))
                    problems.Add(new ValidationProblem(optionPath, $"text lựa chọn '{option.Text}' trùng với lựa chọn khác."));

                if (!AllowedOptionLangs.Contains(option.Lang, StringComparer.Ordinal))
                {
                    problems.Add(new ValidationProblem(optionPath, $"lang '{option.Lang}' không hợp lệ (vi|zh|pinyin)."));
                }
                else if (option.Lang == "pinyin")
                {
                    foreach (var token in option.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        if (!PinyinTokenPattern().IsMatch(token))
                            problems.Add(new ValidationProblem(optionPath, $"lang='pinyin' nhưng text '{option.Text}' không đúng cú pháp pinyin số thanh."));
                }
            }

            if (!input.Options.Any(o => o.Id == input.CorrectOptionId))
                problems.Add(new ValidationProblem($"{path}.correctOptionId", $"correctOptionId '{input.CorrectOptionId}' không thuộc danh sách lựa chọn."));
        }

        if (string.IsNullOrEmpty(input.Explanation))
        {
            if (requireExplanation)
                problems.Add(new ValidationProblem($"{path}.explanation", "explanation phải 1–500 ký tự."));
        }
        else if (input.Explanation.Length > 500)
        {
            problems.Add(new ValidationProblem($"{path}.explanation", requireExplanation ? "explanation phải 1–500 ký tự." : "explanation tối đa 500 ký tự."));
        }
        else
        {
            problems.AddRange(ValidateInlineTokens($"{path}.explanation", input.Explanation));
        }

        return problems;
    }

    // ---- glossary ----

    public static IReadOnlyList<ValidationProblem> ValidateGlossary(IReadOnlyList<GlossaryItem> items, string path = "glossary")
    {
        var problems = new List<ValidationProblem>();
        if (items.Count > 10)
            problems.Add(new ValidationProblem(path, "glossary tối đa 10 mục."));

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var itemPath = $"{path}[{i}]";

            if (string.IsNullOrEmpty(item.Hanzi) || item.Hanzi.Length > 10 || !HanziOnlyPattern().IsMatch(item.Hanzi))
                problems.Add(new ValidationProblem($"{itemPath}.hanzi", "hanzi phải 1–10 chữ Hán."));
            else
                problems.AddRange(ValidateHanziPinyinPair($"{itemPath}.pinyin", item.Hanzi, item.Pinyin));

            if (string.IsNullOrEmpty(item.Vi) || item.Vi.Length > 100)
                problems.Add(new ValidationProblem($"{itemPath}.vi", "vi phải 1–100 ký tự."));
        }

        return problems;
    }

    // ---- tiện ích JsonElement ----

    private static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static List<string> GetStringArray(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
            return [];

        return [.. array.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : "")];
    }
}
