using AntFarm.Identity.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Identity.Infrastructure.Persistence;

/// <summary>
/// F0: DbContext RỖNG — chưa có DbSet nào (schema `identity` do F2 tạo qua migration
/// F2_Accounts). Khối AutoMigrate ở Program.cs chỉ gọi MigrateAsync() khi
/// GetMigrations().Any() nên DbContext rỗng không làm khởi động vỡ.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options), IIdentityDbContext
{
    // F2 bổ sung: public DbSet<Account> Accounts => Set<Account>();
    //             public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
}
