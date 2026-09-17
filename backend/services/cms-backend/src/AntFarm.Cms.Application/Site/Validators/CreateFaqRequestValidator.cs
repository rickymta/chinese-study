using AntFarm.Cms.Application.Site.Dtos;
using FluentValidation;

namespace AntFarm.Cms.Application.Site.Validators;

/// <summary>Kiểm HÌNH DẠNG <c>POST /api/admin/faqs</c> (400 VALIDATION, §5.2.3 W3b).</summary>
public sealed class CreateFaqRequestValidator : AbstractValidator<CreateFaqRequest>
{
    public CreateFaqRequestValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(300);
        RuleFor(x => x.AnswerMarkdown).NotEmpty().MaximumLength(10000);
        // Rỗng ⇒ chuẩn hoá thành "general" (luôn khớp pattern) — chỉ chặn khi giá trị NGƯỜI DÙNG gõ sai định dạng.
        RuleFor(x => x.GroupKey)
            .Must(g => FaqInputRules.GroupKeyPattern().IsMatch(FaqInputRules.Normalize(g)))
            .WithMessage("groupKey phải khớp ^[a-z0-9-]{1,32}$.");
    }
}
