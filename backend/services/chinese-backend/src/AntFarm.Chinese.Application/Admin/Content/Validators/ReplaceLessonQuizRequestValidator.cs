using AntFarm.Chinese.Application.Admin.Content.Dtos;
using FluentValidation;

namespace AntFarm.Chinese.Application.Admin.Content.Validators;

/// <summary>Kiểm HÌNH DẠNG NGOÀI <c>PUT /api/admin/lessons/{id}/quiz</c> (§6.3) — nội dung câu (prompt/pinyin/lang/audioText...) kiểm sâu ở service bằng <c>LessonContentValidator</c> sau khi gán id lựa chọn theo vị trí.</summary>
public sealed class ReplaceLessonQuizRequestValidator : AbstractValidator<ReplaceLessonQuizRequest>
{
    public ReplaceLessonQuizRequestValidator()
    {
        RuleFor(x => x.Questions).Must(q => q.Count <= 30).WithMessage("questions tối đa 30 câu.");
        RuleForEach(x => x.Questions).ChildRules(question =>
        {
            question.RuleFor(q => q.Options).Must(o => o.Count is >= 2 and <= 4)
                .WithMessage("options phải có 2–4 lựa chọn.");
            question.RuleFor(q => q.CorrectIndex).GreaterThanOrEqualTo(0)
                .WithMessage("correctIndex phải ≥ 0.");
        });
    }
}
