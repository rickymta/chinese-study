using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Pinyin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AntFarm.Chinese.Application.Common.Abstractions;

/// <summary>
/// Application chỉ phụ thuộc interface này, không phụ thuộc thẳng EF Core DbContext của
/// Infrastructure (DDD 4 lớp). F3 bổ sung DbSet của schema `access` (§5.1.2); F5 bổ sung schema
/// `learning` (§5.1.1) + <see cref="BeginTransactionAsync"/> (nộp bài luyện thanh ghi
/// session + answers + study_event trong CÙNG một transaction, R5-12).
/// </summary>
public interface IChineseDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }

    DbSet<StudyEvent> StudyEvents { get; }
    DbSet<ToneDrillSession> ToneDrillSessions { get; }
    DbSet<ToneDrillAnswer> ToneDrillAnswers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gỡ TOÀN BỘ entity đang theo dõi khỏi ChangeTracker — dùng khi một lượt ghi thất bại do
    /// đụng độ (race, vd hai request cùng provision một user lần đầu) và code cần đọc lại trạng
    /// thái THẬT từ DB. Không gọi thì entity vừa <c>Add()</c> còn ở trạng thái <c>Added</c> trong
    /// DbContext (scoped/một request) ⇒ lượt <c>SaveChangesAsync</c> KẾ TIẾP trong CÙNG request
    /// (vd controller khác) sẽ cố chèn lại chúng ⇒ lỗi 500 (review F3 17/09/2026).
    /// </summary>
    void ClearTracking();
}
