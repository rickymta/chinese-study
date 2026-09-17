using AntFarm.Chinese.Domain.Srs;

namespace AntFarm.Chinese.Application.Srs;

/// <summary>POST /api/srs/cards/{cardId}/reviews — body (§6.2, R7-8/R7-9).</summary>
public sealed record ReviewCardCommand(Guid ClientReviewId, SrsRating Rating, int? DurationMs);

public sealed record ReviewCardResultDto(Guid ReviewId, bool Duplicate, SrsCardDto Card, SrsSummaryDto Summary);
