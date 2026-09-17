using FluentValidation;

namespace AntFarm.Chinese.Application.Dictionary;

/// <summary>Query string <c>GET /api/dictionary/search?q=&amp;hsk=&amp;page=&amp;pageSize=</c> (§6.1) — <c>class</c> + <c>init</c> có giá trị mặc định (không phải positional record) để tham số thiếu vẫn bind đúng mặc định (cùng quy ước <c>AdminUsersQuery</c>, F4).</summary>
public sealed class DictionaryQuery
{
    public string? Q { get; init; }
    public short? Hsk { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>Kiểm HÌNH DẠNG (400 <c>VALIDATION</c>) — R6-20..R6-21.</summary>
public sealed class DictionaryQueryValidator : AbstractValidator<DictionaryQuery>
{
    public DictionaryQueryValidator()
    {
        RuleFor(x => x.Q)
            .Must(q => DictionaryQueryParser.Normalize(q).Length <= 64)
            .WithMessage("q tối đa 64 ký tự sau chuẩn hoá.");

        RuleFor(x => x.Hsk).InclusiveBetween((short)1, (short)7).When(x => x.Hsk.HasValue);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
