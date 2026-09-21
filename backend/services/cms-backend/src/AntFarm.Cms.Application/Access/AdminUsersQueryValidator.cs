using AntFarm.Cms.Application.Access.Dtos;
using FluentValidation;

namespace AntFarm.Cms.Application.Access;

/// <summary>Chỉ kiểm ĐỊNH DẠNG (400 VALIDATION) — §6.1: <c>q</c> tối đa 100 ký tự, <c>page</c> ≥ 1, <c>pageSize</c> trong 1..100.</summary>
public sealed class AdminUsersQueryValidator : AbstractValidator<AdminUsersQuery>
{
    public AdminUsersQueryValidator()
    {
        RuleFor(x => x.Q).MaximumLength(100).WithMessage("Từ khoá tìm kiếm tối đa 100 ký tự.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("page phải lớn hơn hoặc bằng 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("pageSize phải trong khoảng 1 đến 100.");
    }
}
