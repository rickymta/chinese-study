namespace AntFarm.Chinese.Domain.Content;

/// <summary>Một chữ Hán (§5.1.1, migration F6_Vocabulary) — nạp từ <c>content/chinese/data/characters/characters.json</c> bởi <c>ContentImporter</c> (§5.2.4, R6-11). Khoá tự nhiên <c>Hanzi</c>.</summary>
public sealed class Character
{
    public Guid Id { get; private set; }
    public string Hanzi { get; private set; } = null!;
    public List<string> TraditionalVariants { get; private set; } = [];
    public List<string> PinyinReadings { get; private set; } = [];
    public List<string> HanViet { get; private set; } = [];
    public Dictionary<string, string>? HanVietByPinyin { get; private set; }
    public string HanVietStatus { get; private set; } = Content.HanVietStatus.Derived;
    public short? StrokeCount { get; private set; }
    public string? Radical { get; private set; }
    public short? RadicalNumber { get; private set; }
    public List<string> Sources { get; private set; } = [];

    /// <summary>F10 ghi — có giá trị ⇒ nhóm Hán Việt bị khoá, không cho tệp học liệu ghi đè (R6-11).</summary>
    public DateTime? EditedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core cần constructor không tham số.
    private Character()
    {
    }

    public bool IsHanVietLocked => HanVietStatus == Content.HanVietStatus.Reviewed || EditedAt is not null;

    public static Character CreateFromImport(CharacterImportData data, DateTime nowUtc)
    {
        var character = new Character
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };

        character.ApplyUnprotectedFields(data);
        character.HanViet = [.. data.HanViet];
        character.HanVietByPinyin = data.HanVietByPinyin is null ? null : new Dictionary<string, string>(data.HanVietByPinyin);
        character.HanVietStatus = data.HanVietStatus;

        return character;
    }

    /// <summary>Nạp lại một dòng đã tồn tại (R6-11) — KHÔNG ghi đè nhóm Hán Việt nếu đang bị khoá.</summary>
    public ImportOutcome ApplyImport(CharacterImportData data, DateTime nowUtc)
    {
        var hanVietLocked = IsHanVietLocked;

        var newHanVietByPinyin = data.HanVietByPinyin is null ? null : new Dictionary<string, string>(data.HanVietByPinyin);
        var hanVietWouldChange = !HanViet.SequenceEqual(data.HanViet, StringComparer.Ordinal)
            || !DictionaryEquals(HanVietByPinyin, newHanVietByPinyin)
            || HanVietStatus != data.HanVietStatus;

        var protectedSkipped = hanVietLocked && hanVietWouldChange;

        var before = Snapshot();

        ApplyUnprotectedFields(data);

        if (!hanVietLocked)
        {
            HanViet = [.. data.HanViet];
            HanVietByPinyin = newHanVietByPinyin;
            HanVietStatus = data.HanVietStatus;
        }

        var changed = before != Snapshot();
        if (changed)
            UpdatedAt = nowUtc;

        if (protectedSkipped)
            return ImportOutcome.UpdatedProtected;

        return changed ? ImportOutcome.Updated : ImportOutcome.Unchanged;
    }

    private void ApplyUnprotectedFields(CharacterImportData data)
    {
        Hanzi = data.Hanzi;
        TraditionalVariants = [.. data.TraditionalVariants];
        PinyinReadings = [.. data.PinyinReadings];
        StrokeCount = data.StrokeCount;
        Radical = data.Radical;
        RadicalNumber = data.RadicalNumber;
        Sources = [.. data.Sources];
    }

    private string Snapshot() => string.Join('',
        Hanzi, string.Join('', TraditionalVariants), string.Join('', PinyinReadings),
        string.Join('', HanViet), SerializeMap(HanVietByPinyin), HanVietStatus,
        StrokeCount, Radical, RadicalNumber, string.Join('', Sources));

    private static string SerializeMap(Dictionary<string, string>? map) =>
        map is null ? "" : string.Join('', map.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}{kv.Value}"));

    private static bool DictionaryEquals(Dictionary<string, string>? a, Dictionary<string, string>? b)
    {
        if (a is null || b is null)
            return a is null && b is null;
        if (a.Count != b.Count)
            return false;

        foreach (var (key, value) in a)
            if (!b.TryGetValue(key, out var otherValue) || otherValue != value)
                return false;

        return true;
    }
}
