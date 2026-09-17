using Microsoft.AspNetCore.Authorization;

namespace AntFarm.Auth.Authorization;

/// <summary>Chấp thuận <see cref="PermissionRequirement"/> khi <see cref="IPermissionResolver"/> (đọc DB service) xác nhận người dùng có quyền.</summary>
public sealed class PermissionAuthorizationHandler(IPermissionResolver resolver)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var accountId = context.User.GetAccountId();
        if (accountId is null)
            return; // chưa xác thực — để middleware Authentication xử lý 401, ở đây chỉ không Succeed

        var permissions = await resolver.GetPermissionsAsync(accountId.Value, CancellationToken.None);
        if (permissions.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
