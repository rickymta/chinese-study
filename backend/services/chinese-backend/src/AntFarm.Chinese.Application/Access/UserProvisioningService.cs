using System.Collections.Concurrent;
using System.Security.Claims;
using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Common.Options;
using AntFarm.Chinese.Domain.Access;
using AntFarm.Chinese.Domain.Common;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Chinese.Application.Access;

/// <summary>
/// Provision danh tính lần đầu request có token hợp lệ (R-P4) + gán vai trò mặc định/bootstrap
/// admin lúc TẠO MỚI (R-P5/R-P6 nửa "tạo mới" — nửa "user đã tồn tại lúc khởi động" do
/// <c>AccessSeeder</c> xử lý). Nhận thẳng <see cref="ClaimsPrincipal"/> (kiểu BCL thuần
/// System.Security.Claims, KHÔNG cần tham chiếu AntFarm.Auth/ASP.NET Core để đọc claim bằng
/// <see cref="ClaimsPrincipal.FindFirst(string)"/>) — khớp chữ ký trong hợp đồng §5.2.3.
///
/// Cache 5 phút theo "sub" (R-P4: "tối đa 1 lần/5 phút mỗi user") — <c>static</c> dù lớp đăng ký
/// Scoped, lý do giống <see cref="PermissionResolver"/>: phải sống qua nhiều request.
/// </summary>
public sealed class UserProvisioningService(
    IChineseDbContext db,
    ChineseAccessOptions accessOptions,
    ChineseAdminOptions adminOptions,
    TimeProvider timeProvider,
    ILogger<UserProvisioningService> logger)
{
    private static readonly ConcurrentDictionary<Guid, DateTime> LastCheckedUtc = new();
    private static readonly TimeSpan CacheWindow = TimeSpan.FromMinutes(5);

    /// <summary>Trả về id người dùng (= "sub"). Bảo đảm có dòng trong access.users trước khi Authorization chạy.</summary>
    public async Task<Guid> EnsureAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var accountId = ParseAccountId(principal);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // R-P4: đã kiểm trong 5 phút gần đây ⇒ bỏ qua (tránh đọc/ghi DB mỗi request).
        if (LastCheckedUtc.TryGetValue(accountId, out var lastChecked) && now - lastChecked < CacheWindow)
            return accountId;

        var email = RequireClaim(principal, "email");
        var displayName = RequireClaim(principal, "name");
        var timeZone = TimeZoneCatalog.Normalize(principal.FindFirst("zoneinfo")?.Value);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == accountId, ct);
        if (user is null)
        {
            user = await CreateUserAsync(accountId, email, displayName, timeZone, now, ct);
        }
        else if (user.NeedsProfileSync(email, displayName, timeZone))
        {
            user.SyncProfile(email, displayName, timeZone, now);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            user.Touch(now);
            await db.SaveChangesAsync(ct);
        }

        LastCheckedUtc[accountId] = now;
        return user.Id;
    }

    private async Task<User> CreateUserAsync(Guid accountId, string email, string displayName, string timeZone, DateTime now, CancellationToken ct)
    {
        var user = User.Provision(accountId, email, displayName, timeZone, now);
        db.Users.Add(user);

        // Id do CLIENT đặt (= "sub" của token, không phải DB sinh) nên user.Id đã biết NGAY —
        // gán vai trò được TRƯỚC khi gọi SaveChangesAsync. Chèn User + UserRole trong MỘT lượt
        // ghi DUY NHẤT (review F3 17/09/2026): tách hai SaveChangesAsync để lại một khoảng hở —
        // request khác đọc /api/me ngay sau khi user (chưa kèm vai trò) vừa commit sẽ thấy
        // roles=[] ⇒ 403 oan + PermissionResolver cache rỗng 60 giây. EF Core tự sắp thứ tự
        // INSERT theo khoá ngoại (User trước UserRole) dựa trên metadata quan hệ, không cần
        // navigation property hai chiều.
        var roleCodes = DefaultRoleAssignmentPolicy.Resolve(email, accessOptions, adminOptions);
        List<string> missingRoleCodes = [];
        if (roleCodes.Count > 0)
        {
            var roles = await db.Roles.Where(r => roleCodes.Contains(r.Code)).ToListAsync(ct);
            foreach (var role in roles)
                db.UserRoles.Add(UserRole.Create(user.Id, role.Id, now));

            missingRoleCodes = roleCodes.Except(roles.Select(r => r.Code), StringComparer.OrdinalIgnoreCase).ToList();
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUsersPrimaryKeyViolation(ex))
        {
            // Đua hiếm: hai request cùng provision lần đầu gần như đồng thời — người thua GỠ HẾT
            // entity vừa Add() (User + UserRole) khỏi ChangeTracker rồi đọc lại dòng người thắng
            // vừa tạo. KHÔNG gỡ ⇒ entity còn "Added" trong DbContext (scoped/một request) ⇒ lượt
            // SaveChangesAsync kế tiếp trong CÙNG request (vd controller khác) chèn lại ⇒ 500.
            db.ClearTracking();
            var winner = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == accountId, ct);
            // Chỉ nuốt lỗi khi ĐÚNG LÀ trùng khoá users (đọc lại thấy dòng người thắng) — trường
            // hợp khác (vd lỗi FK role_id lạ) không phải race lành tính, ném lại lỗi gốc.
            if (winner is null)
                throw;

            return winner;
        }

        if (missingRoleCodes.Count > 0)
            logger.LogWarning(
                "Vai trò mặc định {RoleCodes} chưa có trong access.roles — bỏ qua gán cho user mới {UserId}. Kiểm AccessSeeder đã chạy chưa.",
                string.Join(",", missingRoleCodes), user.Id);

        return user;
    }

    /// <summary>Tên ràng buộc do CHÍNH ta đặt trong <c>UserConfiguration</c> (migration F3_Access) — ổn định, không phụ thuộc kiểu Npgsql cụ thể (cùng kỹ thuật với AuthService.IsEmailUniqueViolation ở identity-service).</summary>
    private static bool IsUsersPrimaryKeyViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("pk_users", StringComparison.OrdinalIgnoreCase) == true;

    private static Guid ParseAccountId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id)
            ? id
            : throw new UnauthenticatedException("Token thiếu claim 'sub' hợp lệ.");
    }

    private static string RequireClaim(ClaimsPrincipal principal, string type) =>
        principal.FindFirst(type)?.Value ?? throw new UnauthenticatedException($"Token thiếu claim '{type}'.");
}
