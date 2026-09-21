using AntFarm.Cms.Application.Site.Dtos;
using FluentValidation;

namespace AntFarm.Cms.Application.Site.Validators;

/// <summary>
/// Kiểm HÌNH DẠNG <c>PUT /api/admin/faqs/order</c> (400 VALIDATION) — <c>ORDER_MISMATCH</c> (422,
/// ids không khớp đúng tập FAQ của nhóm) là quy tắc nghiệp vụ, thuộc <see cref="FaqAdminService"/>.
/// </summary>
public sealed class ReorderFaqsRequestValidator : AbstractValidator<ReorderFaqsRequest>
{
    public ReorderFaqsRequestValidator()
    {
        RuleFor(x => x.GroupKey)
            .Must(g => FaqInputRules.GroupKeyPattern().IsMatch(FaqInputRules.Normalize(g)))
            .WithMessage("groupKey phải khớp ^[a-z0-9-]{1,32}$.");
        RuleFor(x => x.Ids).NotNull().WithMessage("Thiếu danh sách ids.");
    }
}
