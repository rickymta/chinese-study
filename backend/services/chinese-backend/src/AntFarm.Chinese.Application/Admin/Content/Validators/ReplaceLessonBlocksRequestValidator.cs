using AntFarm.Chinese.Application.Admin.Content.Dtos;
using AntFarm.Chinese.Domain.Lessons;
using FluentValidation;

namespace AntFarm.Chinese.Application.Admin.Content.Validators;

/// <summary>Kiểm HÌNH DẠNG NGOÀI <c>PUT /api/admin/lessons/{id}/blocks</c> (§6.3) — nội dung <c>payload</c> theo từng loại khối kiểm sâu ở service bằng <c>LessonContentValidator</c> (§5.4.3, cần path chi tiết).</summary>
public sealed class ReplaceLessonBlocksRequestValidator : AbstractValidator<ReplaceLessonBlocksRequest>
{
    public ReplaceLessonBlocksRequestValidator()
    {
        RuleFor(x => x.Blocks).Must(b => b.Count <= 30).WithMessage("blocks tối đa 30 khối.");
        RuleForEach(x => x.Blocks).ChildRules(block =>
            block.RuleFor(b => b.Type).Must(LessonBlockTypes.IsKnown)
                .WithMessage($"type phải là một trong: {string.Join(", ", LessonBlockTypes.All)}."));
    }
}
