using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Identity.Application;

public static class DependencyInjection
{
    /// <summary>Đăng ký các dịch vụ tầng Application (use case, validator...). F0 chưa có gì cụ thể
    /// ngoài quét validator theo assembly — F2 bổ sung AuthService, AccountService.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
