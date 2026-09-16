using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Pinyin;
using AntFarm.Chinese.Infrastructure.Content;
using AntFarm.Chinese.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AntFarm.Chinese.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "Thiếu ConnectionStrings:Default — copy appsettings.Development.json.example thành appsettings.Development.json và điền mật khẩu.");

        services.AddDbContext<ChineseDbContext>(o => o
            .UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IChineseDbContext>(sp => sp.GetRequiredService<ChineseDbContext>());

        services.AddHealthChecks().AddDbContextCheck<ChineseDbContext>("postgres", tags: ["ready"]);

        // F5: học liệu pinyin — Singleton vì bất biến sau khi nạp; AddSingleton chỉ tạo lúc lần
        // RESOLVE ĐẦU TIÊN (§5.2.1 "nạp lười") — Program.cs chủ động resolve ngay sau Build() để
        // học liệu hỏng lộ ra ngay lúc khởi động (log Error) thay vì lúc request đầu tiên tới.
        services.Configure<ContentOptions>(configuration.GetSection(ContentOptions.SectionName));
        services.AddSingleton<IPinyinCatalog>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ContentOptions>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("PinyinCatalogLoader");
            return PinyinCatalogLoader.Load(options.ResolveRootPath(), logger);
        });

        // F6: nạp từ vựng/chữ Hán — Scoped vì dùng ChineseDbContext (ContentImportRunner tự tạo scope riêng ở Program.cs).
        services.AddScoped<ContentImporter>();

        return services;
    }
}
