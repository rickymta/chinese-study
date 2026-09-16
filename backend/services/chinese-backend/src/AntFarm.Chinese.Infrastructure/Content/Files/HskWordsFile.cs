namespace AntFarm.Chinese.Infrastructure.Content.Files;

/// <summary>Ánh xạ <c>content/chinese/data/vocabulary/hsk-words.json</c> (§5.4.5) — <c>System.Text.Json</c>, camelCase.</summary>
public sealed record HskWordsFile(
    string Dataset,
    string Version,
    string? Standard,
    string? License,
    HskWordsCounts? Counts,
    List<HskWordEntry>? Words);

public sealed record HskWordsCounts(int Words, Dictionary<string, int>? ByHsk3Level, Dictionary<string, int>? MeaningViSource);

public sealed record HskWordEntry(
    int? OfficialIndex,
    string Simplified,
    string? Traditional,
    List<string>? Variants,
    string Pinyin,
    int? Hsk3Level,
    int? Hsk2Level,
    int? HskExam2026Level,
    int? PathOrder,
    int? FrequencyRank,
    List<string>? Pos,
    string? UsageNote,
    List<string>? MeaningsEn,
    List<string>? MeaningsVi,
    string MeaningViStatus,
    string MeaningViSource,
    string? HanViet,
    string? HanVietStatus,
    List<string>? Sources);
