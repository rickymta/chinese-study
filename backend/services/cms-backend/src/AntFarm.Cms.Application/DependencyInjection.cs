using System.Reflection;
using AntFarm.Auth.Authorization;
using AntFarm.Cms.Application.Access;
using AntFarm.Cms.Application.Common.Audit;
using AntFarm.Cms.Application.Site;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Cms.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký các dịch vụ tầng Application (use case, validator...). W1: provisioning (R-W3),
    /// PermissionResolver — hiện thực cổng <see cref="IPermissionResolver"/> của AntFarm.Auth trên
    /// DB access.* của chính service (R-W1), MeService (§6.1), UserAdminService (§5.2.1, §6.1).
    /// W3a: cấu hình site/SEO + ngôn ngữ + trang public + nhật ký (§5.2.3) — <see cref="IRevalidationNotifier"/>
    /// đăng ký ở <c>AddInfrastructure</c> (chỉ Infrastructure mới có hiện thực cụ thể). W3b: FAQ +
    /// truy vấn nhật ký.
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

        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<SiteSettingsService>();
        services.AddScoped<LanguageAdminService>();
        services.AddScoped<PublicSiteService>();
        services.AddScoped<FaqAdminService>();
        services.AddScoped<AuditLogQueryService>();

        return services;
    }
}
