using FluentValidation;

namespace AntFarm.Identity.Application.Accounts;

/// <summary>
/// Chỉ kiểm ĐỊNH DẠNG (400 VALIDATION, §6.0) — quy tắc nghiệp vụ (múi giờ có thật hay không,
/// đăng ký có đang mở hay không, email đã tồn tại chưa...) thuộc AuthService (422/403/409).
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.")
            .MaximumLength(254).WithMessage("Email tối đa 254 ký tự.");

        RuleFor(x => x.Password).NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .Length(8, 128).WithMessage("Mật khẩu phải từ 8 đến 128 ký tự.");

        RuleFor(x => x.DisplayName).NotEmpty().WithMessage("Tên hiển thị không được để trống.")
            .MaximumLength(100).WithMessage("Tên hiển thị tối đa 100 ký tự.");

        RuleFor(x => x.TimeZone).NotEmpty().WithMessage("Múi giờ không được để trống.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.");

        RuleFor(x => x.Password).NotEmpty().WithMessage("Mật khẩu không được để trống.");
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().WithMessage("Tên hiển thị không được để trống.")
            .MaximumLength(100).WithMessage("Tên hiển thị tối đa 100 ký tự.");

        RuleFor(x => x.TimeZone).NotEmpty().WithMessage("Múi giờ không được để trống.");
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống.");

        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .Length(8, 128).WithMessage("Mật khẩu mới phải từ 8 đến 128 ký tự.");
    }
}
