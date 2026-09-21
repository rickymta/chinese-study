using AntFarm.Cms.Application.Site.Dtos;
using FluentValidation;

namespace AntFarm.Cms.Application.Site.Validators;

/// <summary>
/// Kiểm HÌNH DẠNG <c>POST /api/admin/languages</c> (400 VALIDATION, §5.2.3) — <c>CODE_TAKEN</c>
/// (409), <c>APP_URL_REQUIRED</c> (422 — cần đọc <c>status</c> đã parse) là quy tắc nghiệp vụ,
/// thuộc <see cref="LanguageAdminService"/>.
/// </summary>
public sealed class CreateLanguageRequestValidator : AbstractValidator<CreateLanguageRequest>
{
    public CreateLanguageRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Matches(LanguageInputRules.CodePattern())
            .WithMessage("code phải khớp ^[a-z][a-z0-9-]{1,31}$.");
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
    }
}
