using AntFarm.Chinese.Application.Common.Options;
using AntFarm.Chinese.Infrastructure.Persistence;
using AntFarm.Chinese.Infrastructure.Seeding;
using AntFarm.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Infrastructure;

/// <summary>
/// EnsureDeleted + Migrate + seed danh mục access.roles/access.permissions MỘT LẦN cho toàn bộ
/// collection (§9.2, giống IdentityDbFixture) — tự Skip nếu thiếu AF_TEST_PG. Seed KHÔNG kèm
/// BootstrapEmails ở đây (chưa có user nào tồn tại lúc fixture chạy) — bootstrap admin cho user
/// MỚI provision do chính app xử lý lúc chạy test (xem ChineseDbApiFactory.BootstrapAdminEmail).
/// </summary>
public sealed class ChineseDbFixture : IAsyncLifetime
{
    public bool HasDatabase { get; } = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AF_TEST_PG"));

    public async Task InitializeAsync()
    {
        if (!HasDatabase)
            return;

        var connectionString = TestDatabase.BuildConnectionString("af_chinese_test");
        var options = new DbContextOptionsBuilder<ChineseDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new ChineseDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        await AccessSeeder.SeedAsync(db, new ChineseAdminOptions(), TimeProvider.System, NullLogger.Instance, CancellationToken.None);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name)]
public sealed class ChineseApiCollection : ICollectionFixture<ChineseDbFixture>
{
    public const string Name = "ChineseApi";
}
