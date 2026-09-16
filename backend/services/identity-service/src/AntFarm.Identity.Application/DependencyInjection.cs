using System.Reflection;
using AntFarm.Identity.Application.Accounts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Identity.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký dịch vụ tầng Application. Không đăng ký <c>IPasswordHasherService</c>,
    /// <c>ITokenIssuer</c>, <c>ISigningKeyStore</c>, <c>AuthOptions</c>, <c>JwtOptions</c> ở đây —
    /// những thứ đó cần dựng SỚM trong Program.cs (khoá ký đọc trước cả khi validate cấu hình
    /// JWT bearer) nên đăng ký trực tiếp ở đó.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<AuthService>();
        services.AddScoped<AccountService>();
        return services;
    }
}
