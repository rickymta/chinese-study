using AntFarm.Cms.Application.Site.Dtos;
using FluentValidation;

namespace AntFarm.Cms.Application.Site.Validators;

/// <summary>Kiểm HÌNH DẠNG <c>PUT /api/admin/faqs/{id}</c> (400 VALIDATION, §5.2.3 W3b).</summary>
public sealed class UpdateFaqRequestValidator : AbstractValidator<UpdateFaqRequest>
{
    public UpdateFaqRequestValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(300);
        RuleFor(x => x.AnswerMarkdown).NotEmpty().MaximumLength(10000);
        RuleFor(x => x.GroupKey)
            .Must(g => FaqInputRules.GroupKeyPattern().IsMatch(FaqInputRules.Normalize(g)))
            .WithMessage("groupKey phải khớp ^[a-z0-9-]{1,32}$.");
        RuleFor(x => x.Version).NotEmpty().WithMessage("Thiếu version.");
    }
}
