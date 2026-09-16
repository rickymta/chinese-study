using System.Reflection;
using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Pinyin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AntFarm.Chinese.Infrastructure.Persistence;

/// <summary>
/// F3: schema `access` (migration F3_Access, §5.1.2) — users, roles, permissions, user_roles,
/// role_permissions. F5: schema `learning` (migration F5_ToneDrill, §5.1.1) — study_events,
/// tone_drill_sessions, tone_drill_answers. KHÔNG gọi <c>HasDefaultSchema</c> — mỗi cấu hình tự
/// khai schema riêng vì service sẽ có thêm `content` ở các feature sau, mặc định ngầm dễ gây nhầm.
/// </summary>
public sealed class ChineseDbContext(DbContextOptions<ChineseDbContext> options)
    : DbContext(options), IChineseDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<StudyEvent> StudyEvents => Set<StudyEvent>();
    public DbSet<ToneDrillSession> ToneDrillSessions => Set<ToneDrillSession>();
    public DbSet<ToneDrillAnswer> ToneDrillAnswers => Set<ToneDrillAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public void ClearTracking() => ChangeTracker.Clear();

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await Database.BeginTransactionAsync(cancellationToken);
}
