using AntFarm.Chinese.Application.Admin.Content.Dtos;
using FluentValidation;

namespace AntFarm.Chinese.Application.Admin.Content.Validators;

/// <summary>Kiểm HÌNH DẠNG <c>POST /api/admin/lessons</c> (§6.3, R-CA8) — trùng slug/không đủ điều kiện xuất bản kiểm ở service (cần đọc DB).</summary>
public sealed class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(x => x.Slug)
            .Length(LessonSlugRules.MinLength, LessonSlugRules.MaxLength)
            .Matches(LessonSlugRules.Pattern())
            .WithMessage("slug phải 3–64 ký tự, chữ thường/số, nối bằng dấu gạch ngang.");

        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Topic).NotEmpty().MaximumLength(64);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0).When(x => x.OrderIndex.HasValue);
        RuleFor(x => x.Summary).MaximumLength(1000).When(x => x.Summary is not null);
    }
}
