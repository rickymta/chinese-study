namespace AntFarm.Chinese.Domain.Pinyin;

/// <summary>
/// MỘT DÒNG MỖI PHẦN của bài luyện thanh (§5.1.1) — <c>listen_tone</c> có 1 phần/câu,
/// <c>tone_pair</c> có 2 phần/câu (mỗi âm tiết một phần). Phi chuẩn hoá <see cref="UserId"/> để
/// <c>ToneStatsService</c> đếm/lọc theo cửa sổ 200 phần/thanh không cần join qua session.
/// </summary>
public sealed class ToneDrillAnswer
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Thứ tự câu trong phiên (0..19) — <c>smallint</c> theo §5.1.1.</summary>
    public short ItemIndex { get; private set; }

    /// <summary>Vị trí âm tiết trong câu: 0 (luôn có) hoặc 1 (chỉ <c>tone_pair</c>) — <c>smallint</c> theo §5.1.1.</summary>
    public short PartIndex { get; private set; }

    /// <summary>Khoá âm tiết (R5-1), KHÔNG thanh — vd "ma", "nv", "zhuang".</summary>
    public string Syllable { get; private set; } = null!;

    /// <summary>Chữ minh hoạ đã phát — lưu lại để xem lại câu sai.</summary>
    public string Hanzi { get; private set; } = null!;

    /// <summary><c>smallint</c> theo §5.1.1 (giá trị 1..4).</summary>
    public short ExpectedTone { get; private set; }

    /// <summary><c>smallint</c> theo §5.1.1 (giá trị 1..4).</summary>
    public short AnsweredTone { get; private set; }

    public bool IsCorrect { get; private set; }

    /// <summary>Thời gian trả lời của CẢ CÂU (ms) — chép cho mọi phần của câu. <c>integer</c> (không phải smallint — có thể tới 600000, §5.1.1).</summary>
    public int? ResponseMs { get; private set; }

    /// <summary>Số lần nghe lại của CẢ CÂU — chép cho mọi phần của câu. <c>smallint</c> theo §5.1.1.</summary>
    public short ReplayCount { get; private set; }

    /// <summary>= <c>session.FinishedAt</c> — dùng để sắp cửa sổ thống kê 200 phần gần nhất mỗi thanh.</summary>
    public DateTime AnsweredAt { get; private set; }

    // EF Core cần constructor không tham số.
    private ToneDrillAnswer()
    {
    }

    /// <summary>Nhận tham số <c>int</c> (khớp DTO/validator ở tầng trên, đã kiểm biên trước khi tới đây) — thu hẹp về <c>short</c> khi lưu (§5.1.1).</summary>
    public static ToneDrillAnswer Create(
        Guid sessionId, Guid userId, int itemIndex, int partIndex, string syllable, string hanzi,
        int expectedTone, int answeredTone, int? responseMs, int replayCount, DateTime answeredAtUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        SessionId = sessionId,
        UserId = userId,
        ItemIndex = (short)itemIndex,
        PartIndex = (short)partIndex,
        Syllable = syllable,
        Hanzi = hanzi,
        ExpectedTone = (short)expectedTone,
        AnsweredTone = (short)answeredTone,
        IsCorrect = expectedTone == answeredTone,
        ResponseMs = responseMs,
        ReplayCount = (short)replayCount,
        AnsweredAt = answeredAtUtc
    };
}
