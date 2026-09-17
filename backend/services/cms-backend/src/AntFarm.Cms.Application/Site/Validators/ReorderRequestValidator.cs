using AntFarm.Cms.Application.Site.Dtos;
using FluentValidation;

namespace AntFarm.Cms.Application.Site.Validators;

/// <summary>
/// Kiểm HÌNH DẠNG <c>PUT /api/admin/languages/order</c> (400 VALIDATION) — mảng rỗng vẫn hợp lệ về
/// hình dạng (không có ngôn ngữ nào thì service đối chiếu ra khớp luôn); <c>ORDER_MISMATCH</c> (422,
/// ids không khớp đúng tập id hiện có) là quy tắc nghiệp vụ, thuộc <see cref="LanguageAdminService"/>.
/// </summary>
public sealed class ReorderRequestValidator : AbstractValidator<ReorderLanguagesRequest>
{
    public ReorderRequestValidator()
    {
        RuleFor(x => x.Ids).NotNull().WithMessage("Thiếu danh sách ids.");
    }
}
