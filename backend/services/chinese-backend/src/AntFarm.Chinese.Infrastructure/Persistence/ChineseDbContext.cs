using System.Reflection;
using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Domain.Access;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Infrastructure.Persistence;

/// <summary>
/// F3: schema `access` (migration F3_Access, §5.1.2) — users, roles, permissions, user_roles,
/// role_permissions. KHÔNG gọi <c>HasDefaultSchema</c> — mỗi cấu hình tự khai schema riêng vì
/// service sẽ có thêm `content`/`learning` ở các feature sau (F5+), mặc định ngầm dễ gây nhầm.
/// </summary>
public sealed class ChineseDbContext(DbContextOptions<ChineseDbContext> options)
    : DbContext(options), IChineseDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public void ClearTracking() => ChangeTracker.Clear();
}
