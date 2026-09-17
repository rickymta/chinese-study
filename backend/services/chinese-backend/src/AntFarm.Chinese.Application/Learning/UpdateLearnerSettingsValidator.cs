using FluentValidation;

namespace AntFarm.Chinese.Application.Learning;

/// <summary>Kiểm HÌNH DẠNG (§6.2) — lỗi ở đây trả 400 <c>VALIDATION</c> với <c>details</c> theo tên trường.</summary>
public sealed class UpdateLearnerSettingsValidator : AbstractValidator<UpdateLearnerSettingsCommand>
{
    public UpdateLearnerSettingsValidator()
    {
        RuleFor(x => x.DailyNewCards).InclusiveBetween((short)0, (short)50);
        RuleFor(x => x.DailyReviewLimit).InclusiveBetween((short)10, (short)1000);

        RuleFor(x => x.DesiredRetention)
            .InclusiveBetween(0.80m, 0.97m)
            .Must(HasAtMostTwoDecimals).WithMessage("desiredRetention tối đa 2 chữ số thập phân.");

        RuleFor(x => x.TtsRate)
            .InclusiveBetween(0.50m, 1.20m)
            .Must(HasAtMostTwoDecimals).WithMessage("ttsRate tối đa 2 chữ số thập phân.");
    }

    private static bool HasAtMostTwoDecimals(decimal value) => decimal.Round(value, 2) == value;
}
