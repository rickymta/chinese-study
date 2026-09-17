using AntFarm.Chinese.Application.Common.Options;
using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Chinese.Infrastructure.Seeding;

/// <summary>
/// R-P3 (chèn bù danh mục vai trò/quyền, idempotent) + nửa "user đã tồn tại" của R-P6 (bootstrap
/// admin lúc KHỞI ĐỘNG cho user có email trong <c>ChineseAdmin:BootstrapEmails</c> mà thiếu admin
/// — không gỡ khi email bị xoá khỏi cấu hình). Gọi từ Program.cs SAU migration — BẮT MỌI exception,
/// log Error, KHÔNG ném (seed hỏng không được làm service chết, CLAUDE.md mục "Seed").
/// </summary>
public static class AccessSeeder
{
    private static readonly (string Code, string Name)[] Roles =
    [
        (RoleCodes.Admin, "Quản trị viên"),
        (RoleCodes.Learner, "Học viên")
    ];

    private static readonly (string Code, string Description)[] Permissions =
    [
        (PermissionCodes.StudyUse, "Dùng mọi chức năng học của service"),
        (PermissionCodes.ContentManage, "Soạn/sửa/xuất bản bài học, quiz, duyệt nghĩa từ vựng"),
        (PermissionCodes.UsersManage, "Xem người dùng của service, gán vai trò")
    ];

    // R-P2: bảng vai trò → quyền đã chốt trong hợp đồng.
    private static readonly Dictionary<string, string[]> RolePermissionMap = new()
    {
        [RoleCodes.Admin] = [PermissionCodes.StudyUse, PermissionCodes.ContentManage, PermissionCodes.UsersManage],
        [RoleCodes.Learner] = [PermissionCodes.StudyUse]
    };

    public static async Task SeedAsync(
        ChineseDbContext db, ChineseAdminOptions adminOptions, TimeProvider timeProvider, ILogger logger, CancellationToken ct)
    {
        try
        {
            await SeedRolesAndPermissionsAsync(db, ct);
            await BootstrapExistingAdminsAsync(db, adminOptions, timeProvider, logger, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seed access.* thất bại — service vẫn khởi động, danh mục quyền/vai trò có thể thiếu tới lần seed kế tiếp.");
        }
    }

    private static async Task SeedRolesAndPermissionsAsync(ChineseDbContext db, CancellationToken ct)
    {
        var existingRoleCodes = await db.Roles.Select(r => r.Code).ToListAsync(ct);
        foreach (var (code, name) in Roles)
            if (!existingRoleCodes.Contains(code))
                db.Roles.Add(Role.Create(code, name));

        var existingPermissionCodes = await db.Permissions.Select(p => p.Code).ToListAsync(ct);
        foreach (var (code, description) in Permissions)
            if (!existingPermissionCodes.Contains(code))
                db.Permissions.Add(Permission.Create(code, description));

        await db.SaveChangesAsync(ct); // cần Role.Id đã tồn tại trước khi chèn role_permissions bên dưới

        var roleByCode = await db.Roles.ToDictionaryAsync(r => r.Code, ct);
        var existingPairs = (await db.RolePermissions.ToListAsync(ct))
            .Select(rp => (rp.RoleId, rp.PermissionCode))
            .ToHashSet();

        foreach (var (roleCode, permissionCodes) in RolePermissionMap)
        {
            if (!roleByCode.TryGetValue(roleCode, out var role))
                continue; // không nên xảy ra (Roles vừa seed ở trên) — phòng hờ dữ liệu lạ

            foreach (var permissionCode in permissionCodes)
                if (!existingPairs.Contains((role.Id, permissionCode)))
                    db.RolePermissions.Add(RolePermission.Create(role.Id, permissionCode));
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task BootstrapExistingAdminsAsync(
        ChineseDbContext db, ChineseAdminOptions adminOptions, TimeProvider timeProvider, ILogger logger, CancellationToken ct)
    {
        var bootstrapEmails = adminOptions.BootstrapEmails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToLowerInvariant())
            .ToHashSet();

        if (bootstrapEmails.Count == 0)
            return;

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == RoleCodes.Admin, ct);
        if (adminRole is null)
            return; // seed vai trò ở trên lỡ thất bại — bỏ qua, lần khởi động sau thử lại

        var candidates = await db.Users
            .Where(u => bootstrapEmails.Contains(u.Email.ToLower()))
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var user in candidates)
        {
            var hasAdmin = await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id, ct);
            if (hasAdmin)
                continue;

            db.UserRoles.Add(UserRole.Create(user.Id, adminRole.Id, now));
            logger.LogInformation("Gán vai trò admin (bootstrap, R-P6) cho user {UserId} ({Email})", user.Id, user.Email);
        }

        await db.SaveChangesAsync(ct);
    }
}
