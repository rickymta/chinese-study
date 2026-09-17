using System.Text.RegularExpressions;
using AntFarm.Chinese.Application.Admin.Content.Dtos;
using AntFarm.Chinese.Domain.Content;
using FluentValidation;

namespace AntFarm.Chinese.Application.Admin.Content.Validators;

/// <summary>Kiểm HÌNH DẠNG <c>PUT /api/admin/words/{id}</c> (§6.3, R-CA9) — chuẩn hoá (trim/bỏ trùng)/so sánh với DB để quyết <c>meaning_vi_source</c> làm ở service.</summary>
public sealed partial class UpdateWordRequestValidator : AbstractValidator<UpdateWordRequest>
{
    // Chữ thường tiếng Việt có dấu, âm tiết cách nhau ĐÚNG một dấu cách (R-CA9) — \p{Ll} bao trùm cả
    // ký tự có dấu đã tổ hợp NFC (đ/ă/â/ê/ô/ơ/ư và các dấu thanh vẫn thuộc phạm trù "chữ thường").
    [GeneratedRegex(@"^\p{Ll}+(\s\p{Ll}+)*$")]
    private static partial Regex HanVietPattern();

    public UpdateWordRequestValidator()
    {
        RuleFor(x => x.MeaningsVi).Must(m => m.Count is >= 1 and <= 10)
            .WithMessage("meaningsVi phải có 1–10 mục.");
        RuleForEach(x => x.MeaningsVi)
            .Must(m => !string.IsNullOrWhiteSpace(m) && m.Trim().Length <= 200)
            .WithMessage("mỗi nghĩa phải 1–200 ký tự (sau khi bỏ khoảng trắng đầu/cuối).");

        RuleFor(x => x.MeaningViStatus).Must(s => s is MeaningViStatus.Machine or MeaningViStatus.Reviewed)
            .WithMessage("meaningViStatus phải là machine hoặc reviewed.");

        RuleFor(x => x.HanViet).MaximumLength(64).Matches(HanVietPattern())
            .When(x => !string.IsNullOrWhiteSpace(x.HanViet))
            .WithMessage("hanViet phải là chữ thường tiếng Việt có dấu, các âm tiết cách nhau một dấu cách.");

        RuleFor(x => x.HanVietStatus).Must(s => s is HanVietStatus.Derived or HanVietStatus.Reviewed)
            .WithMessage("hanVietStatus phải là derived hoặc reviewed.");
    }
}
