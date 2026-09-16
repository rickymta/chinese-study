namespace AntFarm.Chinese.Domain.Pinyin;

/// <summary>Một câu trả lời gửi lên — đầu vào THUẦN cho <see cref="ToneDrillSession.Create"/>, tách khỏi DTO tầng Application/Api (DDD: Domain không phụ thuộc tầng trên).</summary>
public sealed record ToneDrillPartInput(int PartIndex, string Syllable, string Hanzi, int ExpectedTone, int AnsweredTone);

/// <summary>Một câu (item) của bài luyện — 1 phần (<c>listen_tone</c>) hoặc 2 phần (<c>tone_pair</c>).</summary>
public sealed record ToneDrillItemInput(int ItemIndex, IReadOnlyList<ToneDrillPartInput> Parts, int? ResponseMs, int ReplayCount);

/// <summary>
/// Một phiên luyện nghe-chọn thanh đã nộp (§5.1.1). <see cref="Create"/> tự tính <see cref="Total"/>/
/// <see cref="Correct"/> từ danh sách câu — câu ĐÚNG khi MỌI phần đúng (R5-9).
/// </summary>
public sealed class ToneDrillSession
{
    private readonly List<ToneDrillAnswer> _answers = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>uuid client sinh lúc bắt đầu phiên — khoá chống nộp trùng (R5-10, idempotent theo (UserId, ClientSessionId)).</summary>
    public Guid ClientSessionId { get; private set; }

    public ToneDrillMode Mode { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime FinishedAt { get; private set; }

    /// <summary>Số CÂU (không phải số phần) — <c>smallint</c> theo §5.1.1 (tối đa 100 câu/phiên).</summary>
    public short Total { get; private set; }

    /// <summary>Số câu đúng HẾT MỌI PHẦN (R5-9) — <c>smallint</c> theo §5.1.1.</summary>
    public short Correct { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<ToneDrillAnswer> Answers => _answers;

    // EF Core cần constructor không tham số.
    private ToneDrillSession()
    {
    }

    public static ToneDrillSession Create(
        Guid userId, Guid clientSessionId, ToneDrillMode mode,
        DateTime startedAtUtc, DateTime finishedAtUtc,
        IReadOnlyList<ToneDrillItemInput> items, DateTime nowUtc)
    {
        var session = new ToneDrillSession
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ClientSessionId = clientSessionId,
            Mode = mode,
            StartedAt = startedAtUtc,
            FinishedAt = finishedAtUtc,
            Total = (short)items.Count,
            CreatedAt = nowUtc
        };

        var correctItems = 0;
        foreach (var item in items)
        {
            var itemCorrect = true;
            foreach (var part in item.Parts)
            {
                var answer = ToneDrillAnswer.Create(
                    session.Id, userId, item.ItemIndex, part.PartIndex, part.Syllable, part.Hanzi,
                    part.ExpectedTone, part.AnsweredTone, item.ResponseMs, item.ReplayCount, finishedAtUtc);

                session._answers.Add(answer);
                if (!answer.IsCorrect)
                    itemCorrect = false;
            }

            if (itemCorrect)
                correctItems++;
        }

        session.Correct = (short)correctItems;
        return session;
    }
}
