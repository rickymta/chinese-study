using AntFarm.Chinese.Application.Access.Dtos;
using FluentValidation;

namespace AntFarm.Chinese.Application.Access;

/// <summary>
/// Chỉ kiểm ĐỊNH DẠNG (400 VALIDATION, §6.3) — mảng rỗng HỢP LỆ (R4-7: gỡ hết quyền một người là
/// thao tác hợp pháp). Mã vai trò có tồn tại hay không (422 UNKNOWN_ROLE) là quy tắc nghiệp vụ,
/// thuộc <see cref="UserAdminService"/>.
/// </summary>
public sealed class SetUserRolesRequestValidator : AbstractValidator<SetUserRolesRequest>
{
    public SetUserRolesRequestValidator()
    {
        RuleFor(x => x.Roles).NotNull().WithMessage("Thiếu danh sách vai trò (roles).");
        RuleForEach(x => x.Roles)
            .NotEmpty().WithMessage("Mã vai trò không được để trống.")
            .When(x => x.Roles is not null);
    }
}
