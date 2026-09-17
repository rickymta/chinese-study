namespace AntFarm.Chinese.Domain.Content;

/// <summary>Một dòng chữ Hán ĐÃ kiểm hợp lệ, sẵn sàng nạp vào <see cref="Character"/> (§5.2.4) — <c>ContentImporter</c> dựng record này từ <c>characters.json</c> sau khi qua mọi luật §5.4.5.</summary>
public sealed record CharacterImportData(
    string Hanzi,
    IReadOnlyList<string> TraditionalVariants,
    IReadOnlyList<string> PinyinReadings,
    IReadOnlyList<string> HanViet,
    IReadOnlyDictionary<string, string>? HanVietByPinyin,
    string HanVietStatus,
    short? StrokeCount,
    string? Radical,
    short? RadicalNumber,
    IReadOnlyList<string> Sources);
