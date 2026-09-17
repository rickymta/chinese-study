using AntFarm.Chinese.Application.Admin.Content.Dtos;
using FluentValidation;

namespace AntFarm.Chinese.Application.Admin.Content.Validators;

/// <summary>Kiểm HÌNH DẠNG <c>PUT /api/admin/lessons/{id}</c> (§6.3) — biên số khớp CHECK constraint (migration F9_Lessons); <c>glossary</c> kiểm nội dung sâu hơn ở service qua <c>LessonContentValidator</c> (cần thông báo tiếng Việt theo đường dẫn từng mục).</summary>
public sealed class UpdateLessonMetaRequestValidator : AbstractValidator<UpdateLessonMetaRequest>
{
    public UpdateLessonMetaRequestValidator()
    {
        RuleFor(x => x.Slug)
            .Length(LessonSlugRules.MinLength, LessonSlugRules.MaxLength)
            .Matches(LessonSlugRules.Pattern())
            .WithMessage("slug phải 3–64 ký tự, chữ thường/số, nối bằng dấu gạch ngang.");

        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Topic).NotEmpty().MaximumLength(64);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Summary).MaximumLength(1000);
        RuleFor(x => x.EstimatedMinutes).InclusiveBetween((short)1, (short)120);

        RuleFor(x => x.Objectives).Must(o => o.Count <= 20).WithMessage("objectives tối đa 20 mục.");
        RuleForEach(x => x.Objectives).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Glossary).Must(g => g.Count <= 10).WithMessage("glossary tối đa 10 mục.");
    }
}
