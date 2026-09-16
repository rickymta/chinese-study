namespace AntFarm.Chinese.Application.Dictionary;

/// <summary>Một dòng kết quả tìm/liệt kê (§6.1) — dùng cả cho <c>GET /api/dictionary/search</c> lẫn danh sách <c>words[]</c> của <c>GET /api/dictionary/characters/{hanzi}</c> (cùng hình dạng).</summary>
public sealed record DictionarySearchItemDto(
    Guid Id, string Simplified, string? Traditional, string Pinyin,
    short? Hsk3Level, short? Hsk2Level, string? HanViet,
    IReadOnlyList<string> MeaningsVi, string MeaningViStatus, string MatchKind);

public sealed record DictionarySearchResultDto(IReadOnlyList<DictionarySearchItemDto> Items, int Page, int PageSize, int TotalCount);

public sealed record WordCharacterSummaryDto(string Hanzi, IReadOnlyList<string> PinyinReadings, IReadOnlyList<string> HanViet, short? StrokeCount);

/// <summary>Khối SRS của người đang gọi trong chi tiết từ (F7 thêm) — F6 LUÔN <c>null</c> (§6.1).</summary>
public sealed record WordSrsSummaryDto(Guid CardId, string State, DateTime DueAt, bool IsSuspended);

public sealed record WordDetailDto(
    Guid Id, string Simplified, string? Traditional, IReadOnlyList<string> Variants, string Pinyin,
    short? Hsk3Level, short? Hsk2Level, short? HskExam2026Level, short? OfficialIndex, int? PathOrder, int? FrequencyRank,
    IReadOnlyList<string> Pos, string? UsageNote, IReadOnlyList<string> MeaningsEn, IReadOnlyList<string> MeaningsVi,
    string MeaningViStatus, string MeaningViSource, string? HanViet, string? HanVietStatus,
    IReadOnlyList<string> Sources, IReadOnlyList<WordCharacterSummaryDto> Characters, WordSrsSummaryDto? Srs);

public sealed record CharacterDetailDto(
    string Hanzi, IReadOnlyList<string> TraditionalVariants, IReadOnlyList<string> PinyinReadings,
    IReadOnlyList<string> HanViet, IReadOnlyDictionary<string, string>? HanVietByPinyin, string HanVietStatus,
    short? StrokeCount, string? Radical, short? RadicalNumber, IReadOnlyList<DictionarySearchItemDto> Words);
