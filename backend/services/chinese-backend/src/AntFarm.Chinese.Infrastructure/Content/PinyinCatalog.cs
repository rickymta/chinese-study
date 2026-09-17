using AntFarm.Chinese.Application.Pinyin;
using AntFarm.Chinese.Application.Pinyin.Dtos;
using AntFarm.Core.Errors;

namespace AntFarm.Chinese.Infrastructure.Content;

/// <inheritdoc cref="IPinyinCatalog"/>
public sealed class PinyinCatalog : IPinyinCatalog
{
    private readonly PinyinChartDto? _chart;
    private readonly PinyinGuideDto? _guide;
    private readonly IReadOnlyDictionary<string, SyllableEntry> _syllables;

    private PinyinCatalog(bool isAvailable, string version, PinyinChartDto? chart, PinyinGuideDto? guide, IReadOnlyDictionary<string, SyllableEntry> syllables)
    {
        IsAvailable = isAvailable;
        Version = version;
        _chart = chart;
        _guide = guide;
        _syllables = syllables;
    }

    public bool IsAvailable { get; }
    public string Version { get; }

    public PinyinChartDto Chart => _chart ?? throw NotAvailable();
    public PinyinGuideDto Guide => _guide ?? throw NotAvailable();

    public bool TryGetToneExample(string syllable, int tone, out ToneExampleDto? example)
    {
        example = null;
        return IsAvailable
            && _syllables.TryGetValue(syllable, out var entry)
            && entry.Tones.TryGetValue(tone, out example);
    }

    public bool ContainsSyllable(string syllable) => IsAvailable && _syllables.ContainsKey(syllable);

    public static PinyinCatalog Available(string version, PinyinChartDto chart, PinyinGuideDto guide, IReadOnlyDictionary<string, SyllableEntry> syllables) =>
        new(true, version, chart, guide, syllables);

    public static PinyinCatalog Unavailable() =>
        new(false, "", null, null, new Dictionary<string, SyllableEntry>());

    private static ServiceUnavailableException NotAvailable() =>
        new("CONTENT_UNAVAILABLE", "Học liệu pinyin chưa sẵn sàng — báo quản trị viên.");

    /// <summary>Dữ liệu tra cứu nội bộ cho một âm tiết — tách khỏi <see cref="PinyinSyllableDto"/> (DTO trả API) để không lộ cấu trúc lưu trữ.</summary>
    public sealed record SyllableEntry(string Initial, string Final, IReadOnlyDictionary<int, ToneExampleDto> Tones);
}
