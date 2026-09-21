using AntFarm.Cms.Application.Site.Dtos;
using FluentValidation;

namespace AntFarm.Cms.Application.Site.Validators;

/// <summary>Kiểm HÌNH DẠNG <c>PUT /api/admin/languages/{id}</c> (400 VALIDATION, §5.2.3) — không có <c>code</c> (bất biến).</summary>
public sealed class UpdateLanguageRequestValidator : AbstractValidator<UpdateLanguageRequest>
{
    public UpdateLanguageRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
        RuleFor(x => x.NativeName).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Tagline).MaximumLength(160);
        RuleFor(x => x.DescriptionMarkdown).MaximumLength(4000);
        RuleFor(x => x.Status).Must(LanguageInputRules.ValidStatuses.Contains)
            .WithMessage("status phải là open, coming_soon hoặc hidden.");
        RuleFor(x => x.AppUrl)
            .MaximumLength(300)
            .Must(LanguageInputRules.IsValidAppUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.AppUrl))
            .WithMessage("appUrl phải bắt đầu bằng https:// hoặc http://localhost[:port]/http://127.0.0.1[:port].");
        RuleFor(x => x.AccentColor)
            .Matches(LanguageInputRules.AccentColorPattern())
            .When(x => !string.IsNullOrWhiteSpace(x.AccentColor))
            .WithMessage("accentColor phải theo định dạng #RRGGBB hoặc #RRGGBBAA.");
        RuleFor(x => x.Version).NotEmpty().WithMessage("Thiếu version.");
    }
}
