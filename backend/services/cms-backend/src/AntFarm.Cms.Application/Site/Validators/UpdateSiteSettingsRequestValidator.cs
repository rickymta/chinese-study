using AntFarm.Cms.Application.Site.Dtos;
using AntFarm.Cms.Domain.Site;
using FluentValidation;

namespace AntFarm.Cms.Application.Site.Validators;

/// <summary>
/// Kiểm HÌNH DẠNG <c>PUT /api/admin/site-settings</c> (400 VALIDATION, §5.2.3/§6.2) — phải gửi ĐỦ
/// 11 khoá của <see cref="SiteSettingKeys"/>, không thiếu không lạ, mỗi giá trị (sau <c>Trim</c>)
/// đúng luật riêng của khoá đó. Mỗi lỗi gắn <c>propertyName</c> = tên khoá để FE hiện đúng ô.
/// </summary>
public sealed class UpdateSiteSettingsRequestValidator : AbstractValidator<UpdateSiteSettingsRequest>
{
    public UpdateSiteSettingsRequestValidator()
    {
        RuleFor(x => x.Values).NotNull().WithMessage("Thiếu values.");

        RuleFor(x => x.Values).Custom((values, context) =>
        {
            if (values is null)
                return;

            var expectedKeys = SiteSettingKeys.All.Select(d => d.Key).ToHashSet();
            var actualKeys = values.Keys.ToHashSet();

            foreach (var missingKey in expectedKeys.Except(actualKeys))
                context.AddFailure(missingKey, $"Thiếu khoá bắt buộc '{missingKey}'.");

            foreach (var extraKey in actualKeys.Except(expectedKeys))
                context.AddFailure(extraKey, $"Khoá '{extraKey}' không nằm trong danh mục cấu hình.");

            foreach (var key in actualKeys.Intersect(expectedKeys))
            {
                var definition = SiteSettingKeys.ByKey[key];
                var value = (values[key] ?? string.Empty).Trim();
                if (!definition.IsValid(value))
                    context.AddFailure(key, $"Giá trị khoá '{key}' không hợp lệ ({definition.RuleDescription}).");
            }
        });
    }
}
