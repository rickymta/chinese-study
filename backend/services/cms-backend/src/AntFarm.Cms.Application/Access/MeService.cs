using AntFarm.Cms.Application.Access.Dtos;
using AntFarm.Cms.Application.Common.Abstractions;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Cms.Application.Access;

/// <summary>
/// GET /api/me (§6.1). Đọc THẲNG từ DB (không qua <see cref="PermissionResolver"/> — cache 60
/// giây của resolver chỉ dành cho kiểm tra quyền [RequirePermission], còn /api/me phải phản ánh
/// TRẠNG THÁI HIỆN TẠI ngay lập tức, vd sau khi admin gỡ hết vai trò một người dùng).
/// </summary>
public sealed class MeService(ICmsDbContext db)
{
    public async Task<MeDto> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng — UserProvisioningMiddleware lẽ ra đã tạo trước khi tới đây.");

        var roles = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == userId
            orderby r.Code
            select r.Code
        ).ToListAsync(ct);

        var permissions = await (
            from ur in db.UserRoles.AsNoTracking()
            join rp in db.RolePermissions.AsNoTracking() on ur.RoleId equals rp.RoleId
            where ur.UserId == userId
            select rp.PermissionCode
        ).Distinct().OrderBy(p => p).ToListAsync(ct);

        return new MeDto(user.Id, user.Email, user.DisplayName, user.TimeZone, roles, permissions, user.FirstSeenAt);
    }
}
