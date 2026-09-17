using System.Reflection;
using AntFarm.Auth.Authorization;
using AntFarm.Cms.Application.Access;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Cms.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký các dịch vụ tầng Application (use case, validator...). W1: provisioning (R-W3),
    /// PermissionResolver — hiện thực cổng <see cref="IPermissionResolver"/> của AntFarm.Auth trên
    /// DB access.* của chính service (R-W1), MeService (§6.1), UserAdminService (§5.2.1, §6.1).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<UserProvisioningService>();
        services.AddScoped<MeService>();
        // Đăng ký CỤ THỂ (không chỉ qua interface) — UserAdminService cần gọi PermissionResolver.Invalidate,
        // phương thức KHÔNG có trong IPermissionResolver của AntFarm.Auth (chỉ có GetPermissionsAsync).
        services.AddScoped<PermissionResolver>();
        services.AddScoped<IPermissionResolver>(sp => sp.GetRequiredService<PermissionResolver>());
        services.AddScoped<UserAdminService>();

        return services;
    }
}
