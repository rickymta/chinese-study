using AntFarm.Chinese.Application.Writing.Dtos;
using AntFarm.Chinese.Domain.Learning;
using FluentValidation;

namespace AntFarm.Chinese.Application.Writing.Validators;

/// <summary>Kiểm HÌNH DẠNG <c>POST /api/writing/attempts</c> (§5.2.2) — biên số khớp CHECK constraint của migration F8_Writing (đã kiểm ở đây tránh vòng round-trip DB chỉ để nhận lỗi biên).</summary>
public sealed class RecordWritingAttemptValidator : AbstractValidator<RecordWritingAttemptRequest>
{
    public RecordWritingAttemptValidator()
    {
        RuleFor(x => x.ClientAttemptId).NotEqual(Guid.Empty);

        RuleFor(x => x.Hanzi)
            .Must(CjkCharacterValidation.IsSingleCjkCharacter)
            .WithMessage("hanzi phải là đúng một chữ Hán.");

        RuleFor(x => x.Mode).Must(WritingModes.IsKnown).WithMessage("mode phải là guided hoặc recall.");

        RuleFor(x => x.TotalStrokes).InclusiveBetween(1, 64);
        RuleFor(x => x.TotalMistakes).InclusiveBetween(0, 500);
        RuleFor(x => x.HintsUsed).InclusiveBetween(0, 200);
        RuleFor(x => x.DurationMs).InclusiveBetween(0, 3_600_000).When(x => x.DurationMs.HasValue);
    }
}
