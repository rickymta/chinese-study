using AntFarm.Cms.Domain.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AntFarm.Cms.Application.Common.Abstractions;

/// <summary>
/// Application chỉ phụ thuộc interface này, không phụ thuộc thẳng EF Core DbContext của
/// Infrastructure (DDD 4 lớp). W1: DbSet của schema `access` (§5.1.1). Feature sau (W3+) sẽ bổ
/// sung DbSet schema `site` + <see cref="ExecuteSqlAsync"/>/<see cref="SetOriginalVersion"/> cho
/// concurrency (xmin) khi có entity nội dung — chưa cần ở W1.
/// </summary>
public interface ICmsDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gỡ TOÀN BỘ entity đang theo dõi khỏi ChangeTracker — dùng khi một lượt ghi thất bại do
    /// đụng độ (race, vd hai request cùng provision một user lần đầu) và code cần đọc lại trạng
    /// thái THẬT từ DB. Không gọi thì entity vừa <c>Add()</c> còn ở trạng thái <c>Added</c> trong
    /// DbContext (scoped/một request) ⇒ lượt <c>SaveChangesAsync</c> KẾ TIẾP trong CÙNG request sẽ
    /// cố chèn lại chúng ⇒ lỗi 500 (bài học review F3 chinese-backend).
    /// </summary>
    void ClearTracking();
}
