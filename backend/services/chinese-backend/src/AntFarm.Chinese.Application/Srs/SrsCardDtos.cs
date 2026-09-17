using AntFarm.Chinese.Domain.Srs;

namespace AntFarm.Chinese.Application.Srs;

/// <summary>Khối "card" dùng chung ở phản hồi chấm thẻ, thêm thẻ, tạm dừng (§6.2).</summary>
public sealed record SrsCardDto(
    Guid CardId, SrsState State, int? Step, DateTime DueAt,
    double? Stability, double? Difficulty, int Reps, int Lapses, DateTime? LastReviewAt, bool IsSuspended);

/// <summary>POST /api/srs/cards — body (§6.2): 1..100 phần tử, không trùng (kiểm ở <c>AddCardsCommandValidator</c>).</summary>
public sealed record AddCardsCommand(IReadOnlyList<Guid> WordIds);

public sealed record AddCardResultItem(Guid WordId, Guid CardId, bool Created);

public sealed record AddCardsResultDto(int Added, int Skipped, IReadOnlyList<AddCardResultItem> Cards);

/// <summary>PUT /api/srs/cards/{cardId}/suspension (§6.2).</summary>
public sealed record SetCardSuspensionCommand(bool Suspended);
