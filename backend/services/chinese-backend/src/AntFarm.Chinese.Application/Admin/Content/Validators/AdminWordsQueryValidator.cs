using AntFarm.Chinese.Application.Admin.Content.Dtos;
using AntFarm.Chinese.Domain.Content;
using FluentValidation;

namespace AntFarm.Chinese.Application.Admin.Content.Validators;

/// <summary>Chỉ kiểm ĐỊNH DẠNG (400 VALIDATION) — §6.3 <c>GET /api/admin/words</c>.</summary>
public sealed class AdminWordsQueryValidator : AbstractValidator<AdminWordsQuery>
{
    public AdminWordsQueryValidator()
    {
        RuleFor(x => x.MeaningViStatus).Must(s => s is MeaningViStatus.Machine or MeaningViStatus.Reviewed)
            .When(x => x.MeaningViStatus is not null)
            .WithMessage("meaningViStatus phải là machine hoặc reviewed.");
        RuleFor(x => x.HanVietStatus).Must(s => s is HanVietStatus.Derived or HanVietStatus.Reviewed)
            .When(x => x.HanVietStatus is not null)
            .WithMessage("hanVietStatus phải là derived hoặc reviewed.");
        RuleFor(x => x.Hsk).InclusiveBetween((short)1, (short)7).When(x => x.Hsk.HasValue);
        RuleFor(x => x.Q).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
