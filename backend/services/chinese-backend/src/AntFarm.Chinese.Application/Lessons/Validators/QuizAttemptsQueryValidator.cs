using FluentValidation;

namespace AntFarm.Chinese.Application.Lessons.Validators;

/// <summary>Kiểm HÌNH DẠNG <c>GET /api/lessons/{id}/quiz-attempts?limit=</c> (§6.1) — 400 <c>VALIDATION</c>.</summary>
public sealed class QuizAttemptsQueryValidator : AbstractValidator<QuizAttemptsQuery>
{
    public QuizAttemptsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 20);
    }
}
