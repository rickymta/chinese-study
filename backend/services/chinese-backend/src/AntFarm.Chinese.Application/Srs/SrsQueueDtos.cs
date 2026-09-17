namespace AntFarm.Chinese.Application.Srs;

/// <summary>GET /api/srs/queue?limit= — query string (§6.2, R7-7); <c>limit</c> 1..50, mặc định 20.</summary>
public sealed class SrsQueueQuery
{
    public int Limit { get; init; } = 20;
}

/// <summary>Khối "word" rút gọn trong một thẻ hàng đợi (§6.2) — KHÁC <c>DictionarySearchItemDto</c> (không có hsk/matchKind).</summary>
public sealed record SrsQueueWordDto(
    Guid Id, string Simplified, string? Traditional, string Pinyin, string? HanViet,
    IReadOnlyList<string> MeaningsVi, string MeaningViStatus);

/// <summary><c>queue</c>: nhóm hàng đợi thẻ này rơi vào (R7-7) — <c>learning</c> (nhóm 1) · <c>review</c> (nhóm 2) · <c>new</c> (nhóm 3) · <c>ahead</c> (nhóm 4, học trước).</summary>
public sealed record SrsQueueCardDto(
    Guid CardId, Domain.Srs.SrsState State, string Queue, DateTime DueAt,
    SrsQueueWordDto Word, IReadOnlyDictionary<string, string> Intervals);

public sealed record SrsQueueDto(DateTime GeneratedAt, IReadOnlyList<SrsQueueCardDto> Cards, SrsSummaryDto Summary);
