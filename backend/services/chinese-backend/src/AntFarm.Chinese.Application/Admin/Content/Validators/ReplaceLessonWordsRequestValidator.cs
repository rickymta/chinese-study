using AntFarm.Chinese.Application.Admin.Content.Dtos;
using FluentValidation;

namespace AntFarm.Chinese.Application.Admin.Content.Validators;

/// <summary>Kiểm HÌNH DẠNG <c>PUT /api/admin/lessons/{id}/words</c> (§6.3) — id có tồn tại/thuộc bài kiểm ở service (422 <c>UNKNOWN_WORD</c>, cần đọc DB).</summary>
public sealed class ReplaceLessonWordsRequestValidator : AbstractValidator<ReplaceLessonWordsRequest>
{
    public ReplaceLessonWordsRequestValidator()
    {
        RuleFor(x => x.WordIds).Must(ids => ids.Count <= 30).WithMessage("wordIds tối đa 30 mục.");
        RuleFor(x => x.WordIds).Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("wordIds không được trùng.");
        RuleForEach(x => x.WordIds).NotEqual(Guid.Empty);
    }
}
