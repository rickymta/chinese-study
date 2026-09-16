namespace AntFarm.Chinese.Domain.Content;

/// <summary>Một dòng từ vựng ĐÃ kiểm hợp lệ, sẵn sàng nạp vào <see cref="Word"/> (§5.2.4) — <c>ContentImporter</c> dựng record này từ <c>hsk-words.json</c> sau khi qua mọi luật §5.4.5.</summary>
public sealed record WordImportData(
    string Simplified,
    string? Traditional,
    IReadOnlyList<string> Variants,
    string Pinyin,
    short? Hsk3Level,
    short? Hsk2Level,
    short? HskExam2026Level,
    short? OfficialIndex,
    int? PathOrder,
    int? FrequencyRank,
    IReadOnlyList<string> Pos,
    string? UsageNote,
    IReadOnlyList<string> MeaningsEn,
    IReadOnlyList<string> MeaningsVi,
    string MeaningViStatus,
    string MeaningViSource,
    string? HanViet,
    string? HanVietStatus,
    IReadOnlyList<string> Sources);
