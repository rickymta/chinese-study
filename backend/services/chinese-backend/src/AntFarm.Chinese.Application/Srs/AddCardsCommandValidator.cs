using FluentValidation;

namespace AntFarm.Chinese.Application.Srs;

/// <summary>Kiểm HÌNH DẠNG (§6.2) — kiểm từ có TỒN TẠI hay không (422 <c>UNKNOWN_WORD</c>) thuộc <see cref="SrsCardService"/> vì cần đọc DB.</summary>
public sealed class AddCardsCommandValidator : AbstractValidator<AddCardsCommand>
{
    public AddCardsCommandValidator()
    {
        RuleFor(x => x.WordIds)
            .NotEmpty().WithMessage("wordIds không được rỗng.")
            .Must(ids => ids.Count <= 100).WithMessage("wordIds tối đa 100 phần tử.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("wordIds không được trùng.");
    }
}
