using AntFarm.Cms.Application.Common.Abstractions;
using AntFarm.Cms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AntFarm.Cms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "Thiếu ConnectionStrings:Default — copy appsettings.Development.json.example thành appsettings.Development.json và điền mật khẩu.");

        services.AddDbContext<CmsDbContext>(o => o
            .UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<ICmsDbContext>(sp => sp.GetRequiredService<CmsDbContext>());

        services.AddHealthChecks().AddDbContextCheck<CmsDbContext>("postgres", tags: ["ready"]);

        return services;
    }
}
