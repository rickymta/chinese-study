using System.Reflection;
using AntFarm.Cms.Application.Common.Abstractions;
using AntFarm.Cms.Domain.Access;
using AntFarm.Cms.Domain.Site;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AntFarm.Cms.Infrastructure.Persistence;

/// <summary>
/// W1: schema `access` (migration W1_Access, §5.1.1) — users, roles, permissions, user_roles,
/// role_permissions. W3a: schema `site` phần nền (migration W3a_SiteBasics, §5.1.2) — settings,
/// languages, audit_logs. Feature sau (W3b+) bổ sung faqs, ảnh, hộp thư. KHÔNG gọi
/// <c>HasDefaultSchema</c> — mỗi cấu hình tự khai schema riêng.
/// </summary>
public sealed class CmsDbContext(DbContextOptions<CmsDbContext> options)
    : DbContext(options), ICmsDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public void ClearTracking() => ChangeTracker.Clear();

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await Database.BeginTransactionAsync(cancellationToken);

    public void SetOriginalVersion(object entity, uint version) =>
        Entry(entity).Property("Version").OriginalValue = version;
}
