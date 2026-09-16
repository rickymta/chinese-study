using AntFarm.Chinese.Domain.Text;

namespace AntFarm.Chinese.Domain.Content;

/// <summary>
/// Một mục từ vựng HSK (§5.1.1, migration F6_Vocabulary) — nạp từ <c>content/chinese/data/vocabulary/hsk-words.json</c>
/// bởi <c>ContentImporter</c> (§5.2.4, R6-11). Khoá tự nhiên <c>(Simplified, Pinyin)</c> — chữ trùng
/// mặt chữ khác cách đọc là HAI dòng khác nhau (R6-3, vd 地 <c>de5</c>/<c>di4</c>).
/// </summary>
public sealed class Word
{
    public Guid Id { get; private set; }
    public string Simplified { get; private set; } = null!;
    public string? Traditional { get; private set; }
    public List<string> Variants { get; private set; } = [];
    public string Pinyin { get; private set; } = null!;
    public string PinyinCompact { get; private set; } = null!;
    public string PinyinSearch { get; private set; } = null!;
    public short? Hsk3Level { get; private set; }
    public short? Hsk2Level { get; private set; }
    public short? HskExam2026Level { get; private set; }
    public short? OfficialIndex { get; private set; }
    public int? PathOrder { get; private set; }
    public int? FrequencyRank { get; private set; }
    public List<string> Pos { get; private set; } = [];
    public string? UsageNote { get; private set; }
    public List<string> MeaningsEn { get; private set; } = [];
    public List<string> MeaningsVi { get; private set; } = [];
    public string MeaningViStatus { get; private set; } = Content.MeaningViStatus.Machine;
    public string MeaningViSource { get; private set; } = Content.MeaningViSource.Machine;
    public string? HanViet { get; private set; }
    public string? HanVietPlain { get; private set; }
    public string? HanVietStatus { get; private set; }
    public string SearchVi { get; private set; } = null!;
    public string SearchViPlain { get; private set; } = null!;
    public List<string> Sources { get; private set; } = [];

    /// <summary>F10 ghi — có giá trị ⇒ MỌI trường "được duyệt" (nghĩa + Hán Việt) bị khoá, không cho tệp học liệu ghi đè (R6-11).</summary>
    public DateTime? EditedAt { get; private set; }
    public Guid? EditedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core cần constructor không tham số.
    private Word()
    {
    }

    /// <summary>Nhóm nghĩa tiếng Việt bị khoá — đã duyệt (F10) hoặc đã sửa tay (R6-11).</summary>
    public bool IsReviewLocked => MeaningViStatus == Content.MeaningViStatus.Reviewed || EditedAt is not null;

    /// <summary>Nhóm Hán Việt bị khoá — đã duyệt (F10) hoặc đã sửa tay (R6-11).</summary>
    public bool IsHanVietLocked => HanVietStatus == Content.HanVietStatus.Reviewed || EditedAt is not null;

    public static Word CreateFromImport(WordImportData data, DateTime nowUtc)
    {
        var word = new Word
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };

        word.ApplyUnprotectedFields(data);
        word.MeaningsVi = [.. data.MeaningsVi];
        word.MeaningViStatus = data.MeaningViStatus;
        word.MeaningViSource = data.MeaningViSource;
        word.HanViet = data.HanViet;
        word.HanVietStatus = data.HanVietStatus;
        word.RecomputeSearchKeys();

        return word;
    }

    /// <summary>Nạp lại một dòng đã tồn tại (R6-11) — KHÔNG ghi đè nhóm nghĩa/Hán Việt nếu đang bị khoá. Luôn <see cref="RecomputeSearchKeys"/> dù có đổi hay không (pinyin/hanViet có thể vừa đổi ở phần KHÔNG bị khoá).</summary>
    public ImportOutcome ApplyImport(WordImportData data, DateTime nowUtc)
    {
        var reviewLocked = IsReviewLocked;
        var hanVietLocked = IsHanVietLocked;

        var meaningWouldChange = !SequenceEqualsOrdinal(MeaningsVi, data.MeaningsVi)
            || MeaningViStatus != data.MeaningViStatus
            || MeaningViSource != data.MeaningViSource;
        var hanVietWouldChange = HanViet != data.HanViet || HanVietStatus != data.HanVietStatus;

        var protectedSkipped = (reviewLocked && meaningWouldChange) || (hanVietLocked && hanVietWouldChange);

        var before = Snapshot();

        ApplyUnprotectedFields(data);

        if (!reviewLocked)
        {
            MeaningsVi = [.. data.MeaningsVi];
            MeaningViStatus = data.MeaningViStatus;
            MeaningViSource = data.MeaningViSource;
        }

        if (!hanVietLocked)
        {
            HanViet = data.HanViet;
            HanVietStatus = data.HanVietStatus;
        }

        RecomputeSearchKeys();

        var changed = before != Snapshot();
        if (changed)
            UpdatedAt = nowUtc;

        if (protectedSkipped)
            return ImportOutcome.UpdatedProtected;

        return changed ? ImportOutcome.Updated : ImportOutcome.Unchanged;
    }

    private void ApplyUnprotectedFields(WordImportData data)
    {
        Simplified = data.Simplified;
        Traditional = data.Traditional;
        Variants = [.. data.Variants];
        Pinyin = data.Pinyin;
        Hsk3Level = data.Hsk3Level;
        Hsk2Level = data.Hsk2Level;
        HskExam2026Level = data.HskExam2026Level;
        OfficialIndex = data.OfficialIndex;
        PathOrder = data.PathOrder;
        FrequencyRank = data.FrequencyRank;
        Pos = [.. data.Pos];
        UsageNote = data.UsageNote;
        MeaningsEn = [.. data.MeaningsEn];
        Sources = [.. data.Sources];
    }

    private void RecomputeSearchKeys()
    {
        PinyinCompact = WordSearchKeys.PinyinCompact(Pinyin);
        PinyinSearch = WordSearchKeys.PinyinToneless(Pinyin);
        HanVietPlain = HanViet is null ? null : VietnameseText.RemoveDiacritics(VietnameseText.NormalizeForSearch(HanViet));

        var (withDiacritics, plain) = WordSearchKeys.BuildSearchVi(MeaningsVi, HanViet);
        SearchVi = withDiacritics;
        SearchViPlain = plain;
    }

    /// <summary>Ảnh chụp MỌI trường ngoại trừ <see cref="CreatedAt"/>/<see cref="UpdatedAt"/> — so sánh trước/sau để biết có thay đổi thật hay không (mảng so THEO THỨ TỰ phần tử, R6-11).</summary>
    private string Snapshot() => string.Join('',
        Simplified, Traditional, string.Join('', Variants), Pinyin, PinyinCompact, PinyinSearch,
        Hsk3Level, Hsk2Level, HskExam2026Level, OfficialIndex, PathOrder, FrequencyRank,
        string.Join('', Pos), UsageNote, string.Join('', MeaningsEn), string.Join('', MeaningsVi),
        MeaningViStatus, MeaningViSource, HanViet, HanVietPlain, HanVietStatus, SearchVi, SearchViPlain,
        string.Join('', Sources));

    private static bool SequenceEqualsOrdinal(IReadOnlyList<string> a, IReadOnlyList<string> b) =>
        a.Count == b.Count && a.SequenceEqual(b, StringComparer.Ordinal);
}
