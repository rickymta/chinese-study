namespace AntFarm.Chinese.Application.Pinyin.Dtos;

/// <summary>Một ví dụ pinyin+chữ Hán (§5.4.2) — dùng trong <see cref="PinyinInitialDto.Examples"/> và các block hướng dẫn.</summary>
public sealed record PinyinExampleDto(string Pinyin, string Hanzi);

/// <summary>Một thanh mẫu (§6.1 GET /api/pinyin/chart) — <c>Code</c> rỗng ("") nghĩa là "không thanh mẫu".</summary>
public sealed record PinyinInitialDto(
    string Code,
    string Group,
    string Display,
    string Ipa,
    bool Aspirated,
    string NoteVi,
    IReadOnlyList<PinyinExampleDto> Examples);

/// <summary>Một vận mẫu (§6.1).</summary>
public sealed record PinyinFinalDto(
    string Code,
    string Group,
    string Display,
    string? StandaloneSpelling,
    string NoteVi);

/// <summary>Chữ minh hoạ cho một (âm tiết, thanh) — R5-3.</summary>
public sealed record ToneExampleDto(string Hanzi, string MeaningVi);

/// <summary>Một hàng bảng âm tiết — <c>Tones</c> chỉ có khoá "1".."4", có thể rỗng (chưa có chữ minh hoạ).</summary>
public sealed record PinyinSyllableDto(
    string Syllable,
    string Initial,
    string Final,
    IReadOnlyDictionary<string, ToneExampleDto> Tones);

/// <summary>Shape của GET /api/pinyin/chart (§6.1) — <c>Version</c> dùng làm ETag.</summary>
public sealed record PinyinChartDto(
    string Version,
    IReadOnlyList<PinyinInitialDto> Initials,
    IReadOnlyList<PinyinFinalDto> Finals,
    IReadOnlyList<PinyinSyllableDto> Syllables);
