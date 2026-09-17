using AntFarm.Chinese.Application.Admin.Content.Dtos;
using FluentValidation;

namespace AntFarm.Chinese.Application.Admin.Content.Validators;

/// <summary>Kiểm HÌNH DẠNG <c>POST /api/admin/words/review</c> (§6.3) — 1–100 mục.</summary>
public sealed class BulkReviewWordsRequestValidator : AbstractValidator<BulkReviewWordsRequest>
{
    public BulkReviewWordsRequestValidator()
    {
        RuleFor(x => x.Items).Must(i => i.Count is >= 1 and <= 100)
            .WithMessage("items phải có 1–100 mục.");
        RuleForEach(x => x.Items).ChildRules(item => item.RuleFor(i => i.Id).NotEqual(Guid.Empty));
    }
}
