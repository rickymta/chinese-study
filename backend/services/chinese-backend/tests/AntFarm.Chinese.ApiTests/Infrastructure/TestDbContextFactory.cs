using AntFarm.Chinese.Infrastructure.Persistence;
using AntFarm.Testing;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.ApiTests.Infrastructure;

/// <summary>Mở một <see cref="ChineseDbContext"/> RIÊNG (ngoài vòng đời request/DI của app) để test
/// thao tác DB trực tiếp — vd mô phỏng admin gỡ hết vai trò của một người dùng ngoài API.</summary>
internal static class TestDbContextFactory
{
    public static ChineseDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ChineseDbContext>()
            .UseNpgsql(TestDatabase.BuildConnectionString("af_chinese_test"), npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ChineseDbContext(options);
    }
}
