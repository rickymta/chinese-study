namespace AntFarm.Chinese.Infrastructure.Content;

/// <summary>Ánh xạ <c>content/chinese/data/lessons/NN-slug.json</c> (§5.4.2) — <c>System.Text.Json</c>, camelCase.</summary>
public sealed record LessonFile(
    int SchemaVersion,
    string Slug,
    string Title,
    string Topic,
    string Level,
    int OrderIndex,
    string Status,
    string Summary,
    List<string>? Objectives,
    int EstimatedMinutes,
    List<string>? Sources,
    List<LessonFileWord>? Words,
    List<LessonFileGlossaryItem>? Glossary,
    List<LessonFileBlock>? Blocks,
    List<LessonFileQuestion>? Quiz);

public sealed record LessonFileWord(string Simplified, string Pinyin);

public sealed record LessonFileGlossaryItem(string Hanzi, string Pinyin, string Vi);

/// <summary><c>Payload</c> giữ nguyên <see cref="System.Text.Json.JsonElement"/> — hình dạng tuỳ <c>Type</c> (§5.4.2), <c>LessonImporter</c> tự kiểm bằng <c>LessonContentValidator</c> rồi ghi thẳng xuống cột jsonb (không cần deserialize sang record cụ thể).</summary>
public sealed record LessonFileBlock(string Type, System.Text.Json.JsonElement Payload);

public sealed record LessonFileQuestion(
    string Key, string Type, string Prompt, string PromptLang, string? PromptPinyin, string? AudioText,
    List<LessonFileOption>? Options, string CorrectOptionId, string? Explanation);

public sealed record LessonFileOption(string Id, string Text, string Lang);
