using AntFarm.Chinese.Application.Admin.Content.Dtos;
using AntFarm.Chinese.Domain.Lessons;
using FluentValidation;

namespace AntFarm.Chinese.Application.Admin.Content.Validators;

/// <summary>Chỉ kiểm ĐỊNH DẠNG (400 VALIDATION) — §6.3 <c>GET /api/admin/lessons</c>.</summary>
public sealed class AdminLessonsQueryValidator : AbstractValidator<AdminLessonsQuery>
{
    public AdminLessonsQueryValidator()
    {
        RuleFor(x => x.Status).Must(LessonStatuses.All.Contains).When(x => x.Status is not null)
            .WithMessage($"status phải là một trong: {string.Join(", ", LessonStatuses.All)}.");
        RuleFor(x => x.Q).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
