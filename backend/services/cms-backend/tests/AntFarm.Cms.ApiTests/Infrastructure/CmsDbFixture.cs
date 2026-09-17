using AntFarm.Cms.Application.Common.Options;
using AntFarm.Cms.Infrastructure.Persistence;
using AntFarm.Cms.Infrastructure.Seeding;
using AntFarm.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AntFarm.Cms.ApiTests.Infrastructure;

/// <summary>
/// EnsureDeleted + Migrate + seed danh mục access.roles/access.permissions/access.role_permissions
/// + site.settings/site.languages (W3a) MỘT LẦN cho toàn bộ collection — tự Skip nếu thiếu
/// AF_TEST_PG. Seed KHÔNG kèm BootstrapEmails ở đây (chưa có user nào tồn tại lúc fixture chạy) —
/// bootstrap admin cho user MỚI provision do chính app xử lý lúc chạy test (xem
/// CmsDbApiFactory.BootstrapAdminEmail).
/// </summary>
public sealed class CmsDbFixture : IAsyncLifetime
{
    public bool HasDatabase { get; } = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AF_TEST_PG"));

    public async Task InitializeAsync()
    {
        if (!HasDatabase)
            return;

        var connectionString = TestDatabase.BuildConnectionString("af_cms_test");
        var options = new DbContextOptionsBuilder<CmsDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new CmsDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        await AccessSeeder.SeedAsync(db, new CmsAdminOptions(), TimeProvider.System, NullLogger.Instance, CancellationToken.None);
        await SiteSeeder.SeedAsync(db, new CmsSeedOptions(), TimeProvider.System, NullLogger.Instance, CancellationToken.None);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name)]
public sealed class CmsApiCollection : ICollectionFixture<CmsDbFixture>
{
    public const string Name = "CmsApi";
}
