using AntFarm.Cms.Application.Access;
using AntFarm.Cms.Application.Common.Options;
using AntFarm.Cms.Domain.Access;
using AntFarm.Cms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Cms.Infrastructure.Seeding;

/// <summary>
/// R-W9 (chèn bù danh mục vai trò/quyền theo <see cref="RoleCatalog"/>, idempotent) + nửa
/// "user đã tồn tại" của R-W10 (bootstrap admin lúc KHỞI ĐỘNG cho user có email trong
/// <c>CmsAdmin:BootstrapEmails</c> mà thiếu admin — không gỡ khi email bị xoá khỏi cấu hình). Gọi
/// từ Program.cs SAU migration — BẮT MỌI exception, log Error, KHÔNG ném (seed hỏng không được
/// làm service chết, CLAUDE.md mục "Seed"). Chép khuôn
/// <c>AntFarm.Chinese.Infrastructure.Seeding.AccessSeeder</c>.
/// </summary>
public static class AccessSeeder
{
    public static async Task SeedAsync(
        CmsDbContext db, CmsAdminOptions adminOptions, TimeProvider timeProvider, ILogger logger, CancellationToken ct)
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

    private static async Task SeedRolesAndPermissionsAsync(CmsDbContext db, CancellationToken ct)
    {
        var existingRoleCodes = await db.Roles.Select(r => r.Code).ToListAsync(ct);
        foreach (var role in RoleCatalog.Roles)
            if (!existingRoleCodes.Contains(role.Code))
                db.Roles.Add(Role.Create(role.Code, role.Name));

        var existingPermissionCodes = await db.Permissions.Select(p => p.Code).ToListAsync(ct);
        foreach (var permission in RoleCatalog.Permissions)
            if (!existingPermissionCodes.Contains(permission.Code))
                db.Permissions.Add(Permission.Create(permission.Code, permission.Description));

        await db.SaveChangesAsync(ct); // cần Role.Id đã tồn tại trước khi chèn role_permissions bên dưới

        var roleByCode = await db.Roles.ToDictionaryAsync(r => r.Code, ct);
        var existingPairs = (await db.RolePermissions.ToListAsync(ct))
            .Select(rp => (rp.RoleId, rp.PermissionCode))
            .ToHashSet();

        foreach (var roleDefinition in RoleCatalog.Roles)
        {
            if (!roleByCode.TryGetValue(roleDefinition.Code, out var role))
                continue; // không nên xảy ra (Roles vừa seed ở trên) — phòng hờ dữ liệu lạ

            foreach (var permissionCode in roleDefinition.Permissions)
                if (!existingPairs.Contains((role.Id, permissionCode)))
                    db.RolePermissions.Add(RolePermission.Create(role.Id, permissionCode));
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task BootstrapExistingAdminsAsync(
        CmsDbContext db, CmsAdminOptions adminOptions, TimeProvider timeProvider, ILogger logger, CancellationToken ct)
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
            logger.LogInformation("Gán vai trò admin (bootstrap, R-W10) cho user {UserId} ({Email})", user.Id, user.Email);
        }

        await db.SaveChangesAsync(ct);
    }
}
