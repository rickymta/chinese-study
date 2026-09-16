namespace AntFarm.Chinese.Infrastructure.Content.Files;

/// <summary>Ánh xạ <c>content/chinese/data/characters/characters.json</c> (§5.4.5) — <c>System.Text.Json</c>, camelCase.</summary>
public sealed record CharactersFile(
    string Dataset,
    string Version,
    string? License,
    List<CharacterEntry>? Characters);

public sealed record CharacterEntry(
    string Hanzi,
    List<string>? TraditionalVariants,
    List<string>? PinyinReadings,
    List<string>? HanViet,
    Dictionary<string, string>? HanVietByPinyin,
    string HanVietStatus,
    int? StrokeCount,
    string? Radical,
    int? RadicalNumber,
    List<string>? Sources);
