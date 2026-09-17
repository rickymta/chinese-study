using AntFarm.Chinese.Application.Lessons.Dtos;
using FluentValidation;

namespace AntFarm.Chinese.Application.Lessons.Validators;

/// <summary>
/// §5.2.1.2 — <c>startedAt</c> (nếu có) phải trong khoảng hợp lý so với "bây giờ" (chặn đồng hồ máy
/// khách lệch quá xa), không so với <c>TimeProvider</c> (FluentValidation không tiêm được) — dùng
/// <see cref="DateTimeOffset.UtcNow"/> vì đây chỉ là chặn thô, sai lệch vài giây lúc test không đáng
/// kể. So sánh trên <see cref="DateTimeOffset"/> TRỰC TIẾP (không đổi <c>.UtcDateTime</c>) — toán tử
/// so sánh của <see cref="DateTimeOffset"/> tự quy về mốc UTC nên offset gốc (+07:00, Z...) không
/// ảnh hưởng kết quả.
/// </summary>
public sealed class SubmitQuizRequestValidator : AbstractValidator<SubmitQuizRequest>
{
    public SubmitQuizRequestValidator()
    {
        RuleFor(x => x.ClientAttemptId).NotEqual(Guid.Empty);

        RuleFor(x => x.Answers)
            .NotNull()
            .Must(a => a.Count is >= 1 and <= 50).WithMessage("answers phải có 1–50 mục.")
            .Must(a => a.Select(x => x.QuestionId).Distinct().Count() == a.Count).WithMessage("answers không được trùng questionId.");

        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.OptionId).Matches("^[a-d]$").WithMessage("optionId phải là một trong a,b,c,d.");
        });

        RuleFor(x => x.StartedAt)
            .Must(startedAt => startedAt is null ||
                (startedAt.Value <= DateTimeOffset.UtcNow.AddMinutes(5) && startedAt.Value >= DateTimeOffset.UtcNow.AddHours(-24)))
            .WithMessage("startedAt phải trong khoảng hợp lý so với hiện tại.");
    }
}
