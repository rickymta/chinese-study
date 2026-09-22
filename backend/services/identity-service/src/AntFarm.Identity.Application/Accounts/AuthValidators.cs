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

/// <summary>M1: chuỗi hex thường 64 ký tự (32 byte, cùng thuật toán sinh token của <c>AuthService</c>) — dùng lại ở refresh/logout/đổi mật khẩu mobile.</summary>
internal static class MobileRefreshTokenRule
{
    public const string Pattern = "^[0-9a-f]{64}$";
    public const string Message = "Refresh token không đúng định dạng.";
}

public sealed class MobileRegisterRequestValidator : AbstractValidator<MobileRegisterRequest>
{
    public MobileRegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.")
            .MaximumLength(254).WithMessage("Email tối đa 254 ký tự.");

        RuleFor(x => x.Password).NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .Length(8, 128).WithMessage("Mật khẩu phải từ 8 đến 128 ký tự.");

        RuleFor(x => x.DisplayName).NotEmpty().WithMessage("Tên hiển thị không được để trống.")
            .MaximumLength(100).WithMessage("Tên hiển thị tối đa 100 ký tự.");

        RuleFor(x => x.TimeZone).NotEmpty().WithMessage("Múi giờ không được để trống.");

        RuleFor(x => x.DeviceName).MaximumLength(100).WithMessage("Tên thiết bị tối đa 100 ký tự.");
    }
}

public sealed class MobileLoginRequestValidator : AbstractValidator<MobileLoginRequest>
{
    public MobileLoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.");

        RuleFor(x => x.Password).NotEmpty().WithMessage("Mật khẩu không được để trống.");

        RuleFor(x => x.DeviceName).MaximumLength(100).WithMessage("Tên thiết bị tối đa 100 ký tự.");
    }
}

public sealed class MobileRefreshRequestValidator : AbstractValidator<MobileRefreshRequest>
{
    public MobileRefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Thiếu refresh token.")
            .Matches(MobileRefreshTokenRule.Pattern).WithMessage(MobileRefreshTokenRule.Message);
    }
}

public sealed class MobileLogoutRequestValidator : AbstractValidator<MobileLogoutRequest>
{
    public MobileLogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Thiếu refresh token.")
            .Matches(MobileRefreshTokenRule.Pattern).WithMessage(MobileRefreshTokenRule.Message);
    }
}

public sealed class MobileChangePasswordRequestValidator : AbstractValidator<MobileChangePasswordRequest>
{
    public MobileChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống.");

        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .Length(8, 128).WithMessage("Mật khẩu mới phải từ 8 đến 128 ký tự.");

        // RM-A8: RefreshToken tuỳ chọn — CHỈ kiểm định dạng khi có gửi lên.
        RuleFor(x => x.RefreshToken).Matches(MobileRefreshTokenRule.Pattern).WithMessage(MobileRefreshTokenRule.Message)
            .When(x => !string.IsNullOrEmpty(x.RefreshToken));
    }
}
