using System.Collections.Concurrent;
using System.Security.Claims;
using AntFarm.Cms.Application.Common.Abstractions;
using AntFarm.Cms.Application.Common.Options;
using AntFarm.Cms.Domain.Access;
using AntFarm.Cms.Domain.Common;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Cms.Application.Access;

/// <summary>
/// Provision danh tính lần đầu request có token hợp lệ (R-W3, mượn R-P4 gốc) + gán vai trò
/// mặc định (rỗng, R-W2)/bootstrap admin (R-W10) lúc TẠO MỚI — nửa "user đã tồn tại lúc khởi
/// động" do <c>AccessSeeder</c> xử lý. Nhận thẳng <see cref="ClaimsPrincipal"/> (kiểu BCL thuần
/// System.Security.Claims, KHÔNG cần tham chiếu AntFarm.Auth/ASP.NET Core để đọc claim).
///
/// Cache 5 phút theo "sub" lưu ẢNH CHỤP hồ sơ (email/tên/múi giờ) — claim khác ảnh chụp ⇒ đồng
/// bộ NGAY bất kể còn trong cửa sổ 5 phút hay không; giống ảnh chụp thì mới áp cửa sổ 5 phút để
/// tránh dội DB mỗi request. <c>static</c> dù lớp đăng ký Scoped, lý do giống
/// <see cref="PermissionResolver"/>: phải sống qua nhiều request. Chép khuôn
/// <c>AntFarm.Chinese.Application.Access.UserProvisioningService</c>.
/// </summary>
public sealed class UserProvisioningService(
    ICmsDbContext db,
    CmsAccessOptions accessOptions,
    CmsAdminOptions adminOptions,
    TimeProvider timeProvider,
    ILogger<UserProvisioningService> logger)
{
    private static readonly ConcurrentDictionary<Guid, ProfileSnapshot> Cache = new();
    private static readonly TimeSpan CacheWindow = TimeSpan.FromMinutes(5);

    /// <summary>Trả về id người dùng (= "sub"). Bảo đảm có dòng trong access.users trước khi Authorization chạy.</summary>
    public async Task<Guid> EnsureAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var accountId = ParseAccountId(principal);
        var email = RequireClaim(principal, "email");
        var displayName = RequireClaim(principal, "name");
        var zoneinfoClaim = principal.FindFirst("zoneinfo")?.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (!Cache.TryGetValue(accountId, out var snapshot))
            return await EnsureFromDatabaseAsync(accountId, email, displayName, zoneinfoClaim, now, ct);

        // Claim "zoneinfo" rỗng/không hợp lệ ⇒ GIỮ múi giờ đã lưu (không âm thầm ép về mặc định).
        var targetTimeZone = ResolveTimeZoneOrKeep(zoneinfoClaim, snapshot.TimeZone, accountId);
        var changed = snapshot.Email != email || snapshot.DisplayName != displayName || snapshot.TimeZone != targetTimeZone;

        if (changed)
        {
            // Bỏ QUA cửa sổ 5 phút — đồng bộ NGAY. ExecuteUpdateAsync phát MỘT câu UPDATE, không
            // đọc-sửa-ghi ⇒ hai request đồng thời của cùng user không cần khoá riêng.
            await db.Users.Where(u => u.Id == accountId).ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Email, email)
                .SetProperty(u => u.DisplayName, displayName)
                .SetProperty(u => u.TimeZone, targetTimeZone)
                .SetProperty(u => u.LastSeenAt, now), ct);

            Cache[accountId] = new ProfileSnapshot(email, displayName, targetTimeZone, now);
            return accountId;
        }

        if (now - snapshot.CheckedAtUtc < CacheWindow)
            return accountId; // giống ảnh chụp + trong 5 phút ⇒ bỏ qua hoàn toàn, không đụng DB

        // Giống ảnh chụp + đã qua 5 phút ⇒ chỉ cần cập nhật mốc truy cập gần nhất.
        await db.Users.Where(u => u.Id == accountId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.LastSeenAt, now), ct);
        Cache[accountId] = snapshot with { CheckedAtUtc = now };
        return accountId;
    }

    /// <summary>Chưa có trong cache (lần đầu tiến trình thấy user này) ⇒ đọc thẳng DB — tạo mới nếu chưa có, đồng bộ/touch nếu đã có.</summary>
    private async Task<Guid> EnsureFromDatabaseAsync(
        Guid accountId, string email, string displayName, string? zoneinfoClaim, DateTime now, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == accountId, ct);
        if (user is null)
        {
            // Người dùng MỚI: không có múi giờ "cũ" nào để giữ ⇒ claim rỗng/hỏng về mặc định.
            var newTimeZone = TimeZoneCatalog.Normalize(zoneinfoClaim);
            user = await CreateUserAsync(accountId, email, displayName, newTimeZone, now, ct);
            Cache[user.Id] = new ProfileSnapshot(user.Email, user.DisplayName, user.TimeZone, now);
            return user.Id;
        }

        var targetTimeZone = ResolveTimeZoneOrKeep(zoneinfoClaim, user.TimeZone, accountId);
        if (user.NeedsProfileSync(email, displayName, targetTimeZone))
            user.SyncProfile(email, displayName, targetTimeZone, now);
        else
            user.Touch(now);

        await db.SaveChangesAsync(ct);

        Cache[accountId] = new ProfileSnapshot(email, displayName, targetTimeZone, now);
        return user.Id;
    }

    /// <summary>Claim rỗng hoặc không phải ID IANA hợp lệ (kể cả sau khi quy bí danh) ⇒ giữ <paramref name="fallback"/>, log Warning — KHÔNG chặn request.</summary>
    private string ResolveTimeZoneOrKeep(string? zoneinfoClaim, string fallback, Guid accountId)
    {
        if (string.IsNullOrWhiteSpace(zoneinfoClaim))
            return fallback;

        if (TimeZoneCatalog.TryNormalize(zoneinfoClaim, out var normalized))
            return normalized;

        logger.LogWarning(
            "Claim zoneinfo '{ZoneInfo}' không hợp lệ cho user {UserId} — giữ múi giờ cũ {Fallback}.",
            zoneinfoClaim, accountId, fallback);
        return fallback;
    }

    private async Task<User> CreateUserAsync(Guid accountId, string email, string displayName, string timeZone, DateTime now, CancellationToken ct)
    {
        var user = User.Provision(accountId, email, displayName, timeZone, now);
        db.Users.Add(user);

        // Id do CLIENT đặt (= "sub" của token, không phải DB sinh) nên user.Id đã biết NGAY —
        // gán vai trò được TRƯỚC khi gọi SaveChangesAsync. Chèn User + UserRole trong MỘT lượt
        // ghi DUY NHẤT (tránh khoảng hở request khác đọc /api/me ngay sau khi user chưa kèm vai
        // trò vừa commit sẽ thấy roles=[] oan). EF Core tự sắp thứ tự INSERT theo khoá ngoại.
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
            // vừa tạo (không gỡ ⇒ lượt SaveChangesAsync kế tiếp trong CÙNG request chèn lại ⇒ 500).
            db.ClearTracking();
            var winner = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == accountId, ct);
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

    /// <summary>Tên ràng buộc do CHÍNH ta đặt trong <c>UserConfiguration</c> (migration W1_Access) — ổn định, không phụ thuộc kiểu Npgsql cụ thể.</summary>
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

    /// <summary>Ảnh chụp hồ sơ tại lần kiểm gần nhất — so với claim của request hiện tại để quyết định đồng bộ NGAY hay bỏ qua.</summary>
    private sealed record ProfileSnapshot(string Email, string DisplayName, string TimeZone, DateTime CheckedAtUtc);
}
