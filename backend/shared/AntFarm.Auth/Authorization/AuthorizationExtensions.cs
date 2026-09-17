using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Auth.Authorization;

/// <summary>
/// Nối dây <see cref="PermissionPolicyProvider"/> + <see cref="PermissionAuthorizationHandler"/>.
/// Service ngôn ngữ (từ F3) tự đăng ký <see cref="IPermissionResolver"/> hiện thực trên DB riêng
/// TRƯỚC khi gọi extension này. identity-service không dùng — không có khái niệm quyền cục bộ (R-N1).
/// </summary>
public static class AuthorizationExtensions
{
    public static IServiceCollection AddAfPermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }
}
