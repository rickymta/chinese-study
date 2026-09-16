using AntFarm.Chinese.Application.Access.Dtos;
using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Common.Options;
using AntFarm.Chinese.Domain.Access;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Chinese.Application.Access;

/// <summary>
/// Quản trị người dùng/vai trò cục bộ của chinese-backend (§5.2.3, §6.3, R4-6..R4-9). Danh sách
/// chỉ gồm người ĐÃ TỪNG vào service (bảng access.users do <see cref="UserProvisioningService"/>
/// tạo) — không liệt kê tài khoản identity chưa từng vào (R4-6).
/// </summary>
public sealed class UserAdminService(
    IChineseDbContext db,
    ChineseAdminOptions adminOptions,
    PermissionResolver permissionResolver,
    TimeProvider timeProvider,
    ILogger<UserAdminService> logger)
{
    public async Task<AdminUsersPageDto> ListAsync(AdminUsersQuery query, CancellationToken ct)
    {
        var usersQuery = db.Users.AsNoTracking().AsQueryable();

        var q = query.Q?.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            // R4-6: so khớp lower(email) LIKE (dùng cột sinh EmailLower — trùng biểu thức
            // ix_users_email_lower — quy mô MVP vài chục người dùng nên chấp nhận quét tuần tự;
            // chỉ mục btree bình thường KHÔNG tăng tốc mẫu "%q%" có wildcard ở đầu, chỉ mục ở đây
            // có ý nghĩa cho truy vấn khác dùng lower(email) = ... chứ không riêng cho tìm kiếm
            // này) HOẶC display_name khớp không phân biệt hoa/thường. Hạ cả hai vế về chữ thường
            // rồi dùng LIKE thường (không dùng ILIKE của Npgsql) để KHÔNG cần thêm gói
            // Npgsql.EntityFrameworkCore.PostgreSQL riêng cho Application (đã có sẵn
            // Microsoft.EntityFrameworkCore.Relational cho FromSqlInterpolated ở dưới). Escape
            // %/_/\\ để người dùng gõ ký tự đặc biệt không biến thành wildcard ngoài ý muốn.
            var likePatternLower = BuildLikePattern(q.ToLowerInvariant());
            usersQuery = usersQuery.Where(u =>
                EF.Functions.Like(EF.Property<string>(u, "EmailLower"), likePatternLower, "\\") ||
                EF.Functions.Like(u.DisplayName.ToLower(), likePatternLower, "\\"));
        }

        var totalCount = await usersQuery.CountAsync(ct);
        var pageUsers = await usersQuery
            .OrderByDescending(u => u.LastSeenAt).ThenBy(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var rolesByUser = await LoadRoleCodesAsync(pageUsers.Select(u => u.Id), ct);
        var items = pageUsers.Select(u => ToDto(u, rolesByUser)).ToList();

        return new AdminUsersPageDto(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<AdminUserDto> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Không tìm thấy người dùng.");

        var rolesByUser = await LoadRoleCodesAsync([userId], ct);
        return ToDto(user, rolesByUser);
    }

    /// <summary>
    /// R4-7/R4-8/R4-9: thay TOÀN BỘ tập vai trò của <paramref name="targetId"/>. Khoá dòng
    /// <c>access.roles WHERE code='admin'</c> trong transaction để TUẦN TỰ HOÁ mọi thao tác đổi
    /// vai trò — hai admin tự gỡ nhau gần như đồng thời không lọt cả hai (RK — đua LAST_ADMIN).
    /// </summary>
    public async Task<AdminUserDto> SetRolesAsync(Guid actorId, Guid targetId, IReadOnlyList<string>? rawRoles, CancellationToken ct)
    {
        var requested = NormalizeRoles(rawRoles);

        await using var transaction = await db.BeginTransactionAsync(ct);

        // Khoá dòng vai trò "admin" — mọi request PUT roles (kể cả không đụng tới admin) đều đi
        // qua đây nên tuần tự hoá lẫn nhau; bảng access.roles chỉ 2 dòng nên không đáng lo tranh chấp.
        // Dùng ToListAsync() rồi lấy phần tử ở PHÍA CLIENT (thay vì nối thẳng SingleOrDefaultAsync
        // vào FromSqlInterpolated) để giữ NGUYÊN VĂN câu SQL thô, tránh rủi ro EF/Npgsql bọc thêm
        // subquery/LIMIT khi cố dịch cardinality check — đã kiểm tra thực nghiệm race LAST_ADMIN
        // (Task.WhenAll hai request tự gỡ admin của chính mình) xanh ổn định nhiều lần với cách
        // viết này (xem UserRolesTests.HaiAdmin_DongThoiTuGoChinhMinh...).
        var lockedAdminRole = (await db.Roles
            .FromSqlInterpolated($"SELECT * FROM access.roles WHERE code = {RoleCodes.Admin} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync(ct))
            .SingleOrDefault();

        var allRoles = await db.Roles.AsNoTracking().ToListAsync(ct);
        var idByCode = allRoles.ToDictionary(r => r.Code, r => r.Id, StringComparer.OrdinalIgnoreCase);
        var codeById = allRoles.ToDictionary(r => r.Id, r => r.Code);

        var unknown = requested.Where(code => !idByCode.ContainsKey(code)).ToList();
        if (unknown.Count > 0)
            throw new BusinessRuleException(
                "UNKNOWN_ROLE",
                $"Vai trò không tồn tại: {string.Join(", ", unknown)}",
                new { roles = unknown });

        var targetExists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == targetId, ct);
        if (!targetExists)
            throw new NotFoundException("Không tìm thấy người dùng.");

        var currentUserRoles = await db.UserRoles.Where(ur => ur.UserId == targetId).ToListAsync(ct);
        var currentCodes = currentUserRoles.Select(ur => codeById[ur.RoleId]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var desiredCodes = requested.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!currentCodes.SetEquals(desiredCodes))
        {
            // R4-8: LAST_ADMIN — nếu thao tác làm target MẤT admin, phải còn admin KHÁC.
            if (lockedAdminRole is not null && currentCodes.Contains(RoleCodes.Admin) && !desiredCodes.Contains(RoleCodes.Admin))
            {
                var otherAdminCount = await db.UserRoles
                    .CountAsync(ur => ur.RoleId == lockedAdminRole.Id && ur.UserId != targetId, ct);
                if (otherAdminCount == 0)
                    throw new BusinessRuleException("LAST_ADMIN", "Không thể gỡ quyền của quản trị viên cuối cùng.");
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var toRemove = currentUserRoles.Where(ur => !desiredCodes.Contains(codeById[ur.RoleId])).ToList();
            if (toRemove.Count > 0)
                db.UserRoles.RemoveRange(toRemove);

            foreach (var code in desiredCodes.Except(currentCodes, StringComparer.OrdinalIgnoreCase))
                db.UserRoles.Add(UserRole.Create(targetId, idByCode[code], now));

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            permissionResolver.Invalidate(targetId); // R4-9: quyền có hiệu lực NGAY, không chờ cache 60 giây
            logger.LogInformation(
                "{ActorId} đổi vai trò {TargetId}: {Before} → {After}",
                actorId, targetId,
                string.Join(",", currentCodes.OrderBy(c => c)),
                string.Join(",", desiredCodes.OrderBy(c => c)));
        }
        else
        {
            // Tập mới = tập cũ ⇒ không ghi gì, nhưng vẫn phải commit để GIẢI PHÓNG khoá dòng admin.
            await transaction.CommitAsync(ct);
        }

        return await GetAsync(targetId, ct);
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesCatalogAsync(CancellationToken ct)
    {
        var roles = await db.Roles.AsNoTracking().ToListAsync(ct);
        var permissionsByRole = await db.RolePermissions.AsNoTracking()
            .GroupBy(rp => rp.RoleId)
            .Select(g => new { RoleId = g.Key, Codes = g.Select(rp => rp.PermissionCode).ToList() })
            .ToListAsync(ct);
        var permissionsByRoleId = permissionsByRole.ToDictionary(x => x.RoleId, x => (IReadOnlyList<string>)x.Codes.OrderBy(c => c).ToList());

        // §6.3: sắp theo thứ tự cố định admin, learner (RoleCodes.All) — không phải theo tên.
        return RoleCodes.All
            .Select(code => roles.FirstOrDefault(r => r.Code == code))
            .Where(r => r is not null)
            .Select(r => new RoleDto(
                r!.Code,
                r.Name,
                RoleCodes.Describe(r.Code),
                permissionsByRoleId.TryGetValue(r.Id, out var codes) ? codes : []))
            .ToList();
    }

    private async Task<Dictionary<Guid, List<string>>> LoadRoleCodesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.ToList();
        var pairs = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId)
            orderby r.Code
            select new { ur.UserId, r.Code }
        ).ToListAsync(ct);

        var result = ids.ToDictionary(id => id, _ => new List<string>());
        foreach (var pair in pairs)
            result[pair.UserId].Add(pair.Code);

        return result;
    }

    private AdminUserDto ToDto(User user, Dictionary<Guid, List<string>> rolesByUser) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        rolesByUser.TryGetValue(user.Id, out var roles) ? roles : [],
        DefaultRoleAssignmentPolicy.IsBootstrapAdmin(user.Email, adminOptions),
        user.FirstSeenAt,
        user.LastSeenAt);

    /// <summary>R4-7: trim, lower, bỏ trùng — mảng null/rỗng ⇒ tập rỗng (hợp lệ, gỡ hết vai trò).</summary>
    private static List<string> NormalizeRoles(IReadOnlyList<string>? rawRoles) =>
        (rawRoles ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

    private static string BuildLikePattern(string value) =>
        $"%{value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
}
