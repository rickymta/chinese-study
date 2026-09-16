using AntFarm.Chinese.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Infrastructure.Persistence;

/// <summary>
/// F0: DbContext RỖNG — chưa có DbSet nào (schema `access` do F3 tạo qua migration
/// F3_Access; `content`/`learning` ở các feature sau). Khối AutoMigrate ở Program.cs
/// chỉ gọi MigrateAsync() khi GetMigrations().Any() nên DbContext rỗng không làm khởi
/// động vỡ.
/// </summary>
public sealed class ChineseDbContext(DbContextOptions<ChineseDbContext> options)
    : DbContext(options), IChineseDbContext
{
    // F3 bổ sung: public DbSet<User> Users => Set<User>();
    //             public DbSet<Role> Roles => Set<Role>(); ...
}
