using AntFarm.Identity.Infrastructure.Persistence;
using AntFarm.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Identity.ApiTests.Infrastructure;

/// <summary>EnsureDeleted + Migrate MỘT LẦN cho toàn bộ collection (§9.2) — tự Skip nếu thiếu AF_TEST_PG (không dùng [DbFact] ở đây vì đây là fixture, không phải test; test bên trong collection tự [DbFact]).</summary>
public sealed class IdentityDbFixture : IAsyncLifetime
{
    public bool HasDatabase { get; } = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AF_TEST_PG"));

    public async Task InitializeAsync()
    {
        if (!HasDatabase)
            return;

        var connectionString = TestDatabase.BuildConnectionString("af_identity_test");
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new IdentityDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name)]
public sealed class IdentityApiCollection : ICollectionFixture<IdentityDbFixture>
{
    public const string Name = "IdentityApi";
}
