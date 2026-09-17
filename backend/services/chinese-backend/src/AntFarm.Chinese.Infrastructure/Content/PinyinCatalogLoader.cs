using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AntFarm.Chinese.Application.Pinyin.Dtos;
using AntFarm.Chinese.Domain.Pinyin;
using Microsoft.Extensions.Logging;

namespace AntFarm.Chinese.Infrastructure.Content;

/// <summary>
/// Nạp <c>content/chinese/data/pinyin/{initials,finals,syllables,guide}.json</c> lúc khởi động
/// (§5.2.1). MỌI lỗi (thiếu file, JSON hỏng, vi phạm quy tắc tối thiểu) ⇒ log Error + trả catalog
/// <see cref="PinyinCatalog.Unavailable"/> — KHÔNG BAO GIỜ ném ra ngoài (CLAUDE.md mục "Seed": học
/// liệu hỏng không được làm service sập, chỉ endpoint pinyin trả 503).
/// </summary>
public static class PinyinCatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static PinyinCatalog Load(string rootPath, ILogger logger)
    {
        try
        {
            var dataDir = Path.Combine(rootPath, "data", "pinyin");

            var initials = ReadWrapper<RawInitial>(dataDir, "initials.json", "pinyin-initials");
            var finals = ReadWrapper<RawFinal>(dataDir, "finals.json", "pinyin-finals");
            var syllables = ReadWrapper<RawSyllable>(dataDir, "syllables.json", "pinyin-syllables");
            var guide = ReadWrapper<RawGuideTopic>(dataDir, "guide.json", "pinyin-guide");

            var initialCodes = new HashSet<string>(initials.Items!.Select(i => i.Code));
            var finalCodes = new HashSet<string>(finals.Items!.Select(f => f.Code));

            var syllableEntries = new Dictionary<string, PinyinCatalog.SyllableEntry>();
            var syllableDtos = new List<PinyinSyllableDto>();

            foreach (var raw in syllables.Items!)
            {
                if (!PinyinSyllable.IsValidKey(raw.Syllable))
                    throw new InvalidDataException($"syllables.json › khoá '{raw.Syllable}' không hợp lệ (R5-1).");
                if (syllableEntries.ContainsKey(raw.Syllable))
                    throw new InvalidDataException($"syllables.json › khoá '{raw.Syllable}' bị trùng.");
                if (!initialCodes.Contains(raw.Initial))
                    throw new InvalidDataException($"syllables.json › {raw.Syllable} › initial '{raw.Initial}' không có trong initials.json.");
                if (!finalCodes.Contains(raw.Final))
                    throw new InvalidDataException($"syllables.json › {raw.Syllable} › final '{raw.Final}' không có trong finals.json.");

                var tones = new Dictionary<int, ToneExampleDto>();
                var toneDtos = new Dictionary<string, ToneExampleDto>();
                foreach (var (toneKey, toneValue) in raw.Tones ?? [])
                {
                    if (toneKey is not ("1" or "2" or "3" or "4"))
                        throw new InvalidDataException($"syllables.json › {raw.Syllable} › tones › khoá '{toneKey}' không hợp lệ (chỉ 1..4).");

                    var dto = new ToneExampleDto(toneValue.Hanzi, toneValue.MeaningVi);
                    tones[int.Parse(toneKey)] = dto;
                    toneDtos[toneKey] = dto;
                }

                syllableEntries[raw.Syllable] = new PinyinCatalog.SyllableEntry(raw.Initial, raw.Final, tones);
                syllableDtos.Add(new PinyinSyllableDto(raw.Syllable, raw.Initial, raw.Final, toneDtos));
            }

            var initialDtos = initials.Items!
                .Select(i => new PinyinInitialDto(
                    i.Code, i.Group, i.Display, i.Ipa, i.Aspirated, i.NoteVi,
                    (i.Examples ?? []).Select(e => new PinyinExampleDto(e.Pinyin, e.Hanzi)).ToList()))
                .ToList();

            var finalDtos = finals.Items!
                .Select(f => new PinyinFinalDto(f.Code, f.Group, f.Display, f.StandaloneSpelling, f.NoteVi))
                .ToList();

            var guideTopics = guide.Items!
                .Select(g => new GuideTopicDto(g.Id, g.Title, g.Order, g.Blocks))
                .OrderBy(t => t.Order)
                .ToList();

            var version = ComputeVersion(dataDir);
            var chart = new PinyinChartDto(version, initialDtos, finalDtos, syllableDtos);
            var guideDto = new PinyinGuideDto(version, guideTopics);

            var toneExampleCount = syllableEntries.Values.Sum(s => s.Tones.Count);
            logger.LogInformation(
                "Nạp học liệu pinyin thành công: {InitialCount} thanh mẫu, {FinalCount} vận mẫu, {SyllableCount} âm tiết, {ToneExampleCount} cặp (âm tiết, thanh) có chữ minh hoạ.",
                initialDtos.Count, finalDtos.Count, syllableDtos.Count, toneExampleCount);

            return PinyinCatalog.Available(version, chart, guideDto, syllableEntries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Nạp học liệu pinyin từ '{RootPath}' thất bại — GET/POST /api/pinyin/* sẽ trả 503 CONTENT_UNAVAILABLE tới khi khởi động lại với học liệu hợp lệ.",
                rootPath);
            return PinyinCatalog.Unavailable();
        }
    }

    private static RawWrapper<T> ReadWrapper<T>(string dataDir, string fileName, string expectedDataset)
    {
        var path = Path.Combine(dataDir, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Thiếu file học liệu '{fileName}'.", path);

        var json = File.ReadAllText(path);
        var wrapper = JsonSerializer.Deserialize<RawWrapper<T>>(json, JsonOptions)
            ?? throw new InvalidDataException($"{fileName}: nội dung rỗng hoặc không parse được.");

        if (wrapper.Dataset != expectedDataset)
            throw new InvalidDataException($"{fileName}: dataset phải là '{expectedDataset}', nhận '{wrapper.Dataset}'.");
        if (wrapper.Items is null || wrapper.Items.Count == 0)
            throw new InvalidDataException($"{fileName}: items rỗng.");

        return wrapper;
    }

    private static string ComputeVersion(string dataDir)
    {
        var combined = new StringBuilder();
        foreach (var fileName in new[] { "initials.json", "finals.json", "syllables.json", "guide.json" })
            combined.Append(File.ReadAllText(Path.Combine(dataDir, fileName)));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined.ToString()));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private sealed record RawWrapper<T>(string Dataset, string Version, List<T>? Items);
    private sealed record RawExample(string Pinyin, string Hanzi);
    private sealed record RawInitial(string Code, string Group, string Display, string Ipa, bool Aspirated, string NoteVi, List<RawExample>? Examples);
    private sealed record RawFinal(string Code, string Group, string Display, string? StandaloneSpelling, string NoteVi);
    private sealed record RawToneExample(string Hanzi, string MeaningVi);
    private sealed record RawSyllable(string Syllable, string Initial, string Final, Dictionary<string, RawToneExample>? Tones);
    private sealed record RawGuideTopic(string Id, string Title, int Order, JsonElement Blocks);
}
