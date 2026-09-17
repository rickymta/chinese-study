using FluentValidation;

namespace AntFarm.Chinese.Application.Writing.Validators;

/// <summary>Kiểm HÌNH DẠNG <c>GET /api/writing/characters?set=&amp;page=&amp;pageSize=</c> (§6.2, R-W6) — 400 <c>VALIDATION</c> khi <c>set</c> không khớp bất kỳ giá trị nào (bao gồm <c>lesson:</c> sai định dạng slug).</summary>
public sealed class WritingCharactersQueryValidator : AbstractValidator<WritingCharactersQuery>
{
    public WritingCharactersQueryValidator()
    {
        RuleFor(x => x.Set)
            .Matches("^(hsk1|weak|practiced|lesson:[a-z0-9]+(-[a-z0-9]+)*)$")
            .WithMessage("set phải là hsk1, weak, practiced hoặc lesson:<slug>.");

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
