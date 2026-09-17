using FluentValidation;

namespace AntFarm.Identity.Application.Admin;

/// <summary>Chỉ kiểm ĐỊNH DẠNG (400 VALIDATION) — khuôn <c>AdminUsersQueryValidator</c> của chinese-backend.</summary>
public sealed class AdminAccountsQueryValidator : AbstractValidator<AdminAccountsQuery>
{
    public AdminAccountsQueryValidator()
    {
        RuleFor(x => x.Q).MaximumLength(100).WithMessage("Từ khoá tìm kiếm tối đa 100 ký tự.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("page phải lớn hơn hoặc bằng 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("pageSize phải trong khoảng 1 đến 100.");
    }
}

/// <summary>R-A2: dùng lại luật độ dài mật khẩu của đăng ký (8–128 ký tự) — chỉ áp khi admin TỰ NHẬP <c>newPassword</c> (rỗng/thiếu ⇒ sinh mật khẩu tạm, không qua luật này).</summary>
public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .Length(8, 128).WithMessage("Mật khẩu mới phải từ 8 đến 128 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.NewPassword));
    }
}
