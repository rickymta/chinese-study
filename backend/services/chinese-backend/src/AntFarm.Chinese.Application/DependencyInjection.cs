using System.Reflection;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Access;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Chinese.Application;

public static class DependencyInjection
{
    /// <summary>Đăng ký các dịch vụ tầng Application (use case, validator...). F3: provisioning
    /// (R-P4/R-P5/R-P6), PermissionResolver — hiện thực cổng <see cref="IPermissionResolver"/> của
    /// AntFarm.Auth trên DB access.* của chính service (R-P1), MeService (§6.3).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<UserProvisioningService>();
        services.AddScoped<MeService>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        return services;
    }
}
