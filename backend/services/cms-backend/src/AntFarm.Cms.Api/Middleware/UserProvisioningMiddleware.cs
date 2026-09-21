using AntFarm.Cms.Application.Access;

namespace AntFarm.Cms.Api.Middleware;

/// <summary>
/// Provision/đồng bộ người dùng (R-W3) NGAY SAU xác thực (UseAuthentication) và TRƯỚC phân quyền
/// (UseAuthorization) — bảo đảm access.users luôn có dòng trước khi
/// PermissionAuthorizationHandler đọc quyền. Bỏ qua khi request không kèm token hợp lệ (endpoint
/// AllowAnonymous như /health, /api/system/info không cần provision).
/// </summary>
public sealed class UserProvisioningMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, UserProvisioningService provisioning)
    {
        if (context.User.Identity?.IsAuthenticated == true)
            await provisioning.EnsureAsync(context.User, context.RequestAborted);

        await next(context);
    }
}
