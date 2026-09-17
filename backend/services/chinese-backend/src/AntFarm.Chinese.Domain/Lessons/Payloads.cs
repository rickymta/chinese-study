namespace AntFarm.Chinese.Domain.Lessons;

// (De)serialize qua LessonJson (Application, System.Text.Json camelCase) — dùng chung cho
// content.lesson_blocks.payload, content.quiz_questions.options, content.lessons.glossary (jsonb,
// §5.1.1). Đặt ở Domain vì là hình dạng dữ liệu nghiệp vụ thuần, không phụ thuộc EF/JSON runtime.

public sealed record TextPayload(IReadOnlyList<string> Paragraphs);

public sealed record DialoguePayload(string? Title, IReadOnlyList<DialogueLine> Lines);

public sealed record DialogueLine(string Speaker, string Hanzi, string Pinyin, string Vi);

public sealed record GrammarPayload(string Title, string? Pattern, string Explanation, IReadOnlyList<GrammarExample> Examples);

public sealed record GrammarExample(string Hanzi, string Pinyin, string Vi, string? Note);

public sealed record TipPayload(string Text, string? Variant);

/// <summary>Một lựa chọn của câu quiz — <c>Id</c> ∈ a..d, <c>Lang</c> ∈ vi|zh|pinyin (R-CA4/§5.4.3).</summary>
public sealed record QuizOption(string Id, string Text, string Lang);

/// <summary>Mục từ bổ sung của bài (§5.1.1 <c>lessons.glossary</c>) — KHÔNG vào SRS.</summary>
public sealed record GlossaryItem(string Hanzi, string Pinyin, string Vi);
