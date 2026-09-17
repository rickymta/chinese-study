using FluentValidation;

namespace AntFarm.Chinese.Application.Srs;

/// <summary>Kiểm HÌNH DẠNG <c>GET /api/srs/queue?limit=</c> (§6.2, R7-7) — 400 <c>VALIDATION</c>.</summary>
public sealed class SrsQueueQueryValidator : AbstractValidator<SrsQueueQuery>
{
    public SrsQueueQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);
    }
}
