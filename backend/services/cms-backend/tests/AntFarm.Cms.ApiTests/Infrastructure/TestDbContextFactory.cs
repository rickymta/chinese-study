using AntFarm.Cms.Infrastructure.Persistence;
using AntFarm.Testing;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Cms.ApiTests.Infrastructure;

/// <summary>Mở một <see cref="CmsDbContext"/> RIÊNG (ngoài vòng đời request/DI của app) để test
/// thao tác DB trực tiếp — vd cô lập admin dư thừa giữa các lớp test LAST_ADMIN.</summary>
internal static class TestDbContextFactory
{
    public static CmsDbContext Create()
    {
        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseNpgsql(TestDatabase.BuildConnectionString("af_cms_test"), npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CmsDbContext(options);
    }
}
