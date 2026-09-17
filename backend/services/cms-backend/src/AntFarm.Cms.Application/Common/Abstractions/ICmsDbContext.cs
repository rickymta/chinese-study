using AntFarm.Cms.Domain.Access;
using AntFarm.Cms.Domain.Site;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AntFarm.Cms.Application.Common.Abstractions;

/// <summary>
/// Application chỉ phụ thuộc interface này, không phụ thuộc thẳng EF Core DbContext của
/// Infrastructure (DDD 4 lớp). W1: DbSet của schema `access` (§5.1.1). W3a: DbSet schema `site`
/// phần nền (`settings`, `languages`, `audit_logs`, §5.1.2) + <see cref="SetOriginalVersion"/> cho
/// concurrency (xmin, khuôn chinese-backend F10) trên <c>Language</c>.
/// </summary>
public interface ICmsDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }

    DbSet<SiteSetting> SiteSettings { get; }
    DbSet<Language> Languages { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghi đè giá trị <c>xmin</c> ĐỌC ĐƯỢC (client gửi lại ở lần ghi kế tiếp) làm "giá trị gốc" mà
    /// EF dùng để sinh <c>UPDATE ... WHERE xmin = @original</c> — lệch với xmin THẬT của dòng trong
    /// DB ⇒ <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/> (R-CA3, 409
    /// <c>CONCURRENCY_CONFLICT</c>). Chép khuôn <c>IChineseDbContext.SetOriginalVersion</c>.
    /// </summary>
    void SetOriginalVersion(object entity, uint version);

    /// <summary>
    /// Gỡ TOÀN BỘ entity đang theo dõi khỏi ChangeTracker — dùng khi một lượt ghi thất bại do
    /// đụng độ (race, vd hai request cùng provision một user lần đầu) và code cần đọc lại trạng
    /// thái THẬT từ DB. Không gọi thì entity vừa <c>Add()</c> còn ở trạng thái <c>Added</c> trong
    /// DbContext (scoped/một request) ⇒ lượt <c>SaveChangesAsync</c> KẾ TIẾP trong CÙNG request sẽ
    /// cố chèn lại chúng ⇒ lỗi 500 (bài học review F3 chinese-backend).
    /// </summary>
    void ClearTracking();
}
