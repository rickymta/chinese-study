using System.Collections.Concurrent;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Access;

/// <summary>
/// Nguồn sự thật DUY NHẤT của quyền hiệu lực (R-P1) — join users → user_roles → role_permissions
/// trên DB `af_chinese`, KHÔNG suy quyền từ claim JWT. Cache 60 giây theo userId để
/// <c>PermissionAuthorizationHandler</c> (chạy trên MỖI request có [RequirePermission]) không dội
/// DB liên tục. Cache là <c>static</c> dù lớp đăng ký Scoped — phải sống QUA nhiều request/scope,
/// không phải static thì mỗi request tạo cache rỗng mới, vô nghĩa.
/// </summary>
public sealed class PermissionResolver(IChineseDbContext db, TimeProvider timeProvider) : IPermissionResolver
{
    private static readonly ConcurrentDictionary<Guid, CacheEntry> Cache = new();
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (Cache.TryGetValue(userId, out var entry) && entry.ExpiresAtUtc > now)
            return entry.Permissions;

        var permissionCodes = await (
            from ur in db.UserRoles.AsNoTracking()
            join rp in db.RolePermissions.AsNoTracking() on ur.RoleId equals rp.RoleId
            where ur.UserId == userId
            select rp.PermissionCode
        ).Distinct().ToListAsync(cancellationToken);

        IReadOnlySet<string> permissions = permissionCodes.ToHashSet();
        Cache[userId] = new CacheEntry(permissions, now.Add(Ttl));
        return permissions;
    }

    /// <summary>Bỏ cache của một user — dùng khi vai trò vừa đổi (F4 <c>UserAdminService.SetRolesAsync</c>) để quyền có hiệu lực NGAY thay vì chờ tối đa 60 giây.</summary>
    public void Invalidate(Guid userId) => Cache.TryRemove(userId, out _);

    private readonly record struct CacheEntry(IReadOnlySet<string> Permissions, DateTime ExpiresAtUtc);
}
