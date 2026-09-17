using System.Reflection;
using AntFarm.Cms.Application.Common.Abstractions;
using AntFarm.Cms.Domain.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AntFarm.Cms.Infrastructure.Persistence;

/// <summary>
/// W1: schema `access` (migration W1_Access, §5.1.1) — users, roles, permissions, user_roles,
/// role_permissions. Feature sau (W3+) bổ sung schema `site` (nội dung website, ảnh, hộp thư,
/// nhật ký thao tác). KHÔNG gọi <c>HasDefaultSchema</c> — mỗi cấu hình tự khai schema riêng.
/// </summary>
public sealed class CmsDbContext(DbContextOptions<CmsDbContext> options)
    : DbContext(options), ICmsDbContext
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

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await Database.BeginTransactionAsync(cancellationToken);
}
