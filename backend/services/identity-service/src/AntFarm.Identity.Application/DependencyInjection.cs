using System.Reflection;
using AntFarm.Identity.Application.Accounts;
using AntFarm.Identity.Application.Admin;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Identity.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký dịch vụ tầng Application. Không đăng ký <c>IPasswordHasherService</c>,
    /// <c>ITokenIssuer</c>, <c>ISigningKeyStore</c>, <c>AuthOptions</c>, <c>JwtOptions</c>,
    /// <c>InternalOptions</c> ở đây — những thứ đó cần dựng SỚM trong Program.cs (khoá ký đọc
    /// trước cả khi validate cấu hình JWT bearer) nên đăng ký trực tiếp ở đó.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<AuthService>();
        services.AddScoped<AccountService>();

        // W10 (§5.2.9): API nội bộ quản trị tài khoản + cài đặt đăng ký runtime.
        services.AddScoped<AccountAdminService>();
        services.AddScoped<RegistrationStatsService>();
        services.AddScoped<PlatformSettingsService>();
        services.AddSingleton<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();
        // Singleton (không Scoped): sống suốt vòng đời ứng dụng để cache 30s có tác dụng qua
        // nhiều request/nhiều scope khác nhau — tự mở IServiceScopeFactory khi cần đọc DB.
        services.AddSingleton<IRegistrationGate, RegistrationGate>();

        return services;
    }
}
