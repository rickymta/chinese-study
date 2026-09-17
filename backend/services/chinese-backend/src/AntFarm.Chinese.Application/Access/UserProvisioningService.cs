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
/// F4/RK10/R4-4: cache 5 phút theo "sub" lưu ẢNH CHỤP hồ sơ (email/tên/múi giờ) — KHÔNG PHẢI chỉ
/// một mốc thời gian như F3 (bug F3: trong 5 phút thì bỏ qua HOÀN TOÀN dù claim đã đổi, khiến
/// người dùng đổi múi giờ ở /ho-so không thấy chinese-backend cập nhật ngay). Quy tắc mới: claim
/// khác ảnh chụp ⇒ đồng bộ NGAY bất kể còn trong cửa sổ 5 phút hay không; giống ảnh chụp thì mới
/// áp cửa sổ 5 phút để tránh dội DB mỗi request. <c>static</c> dù lớp đăng ký Scoped, lý do giống
/// <see cref="PermissionResolver"/>: phải sống qua nhiều request.
/// </summary>
public sealed class UserProvisioningService(
    IChineseDbContext db,
    ChineseAccessOptions accessOptions,
    ChineseAdminOptions adminOptions,
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

        // R4-4: claim "zoneinfo" rỗng/không hợp lệ ⇒ GIỮ múi giờ đã lưu (không âm thầm ép về mặc
        // định — người dùng có thể đã cố tình chọn múi giờ khác Asia/Ho_Chi_Minh).
        var targetTimeZone = ResolveTimeZoneOrKeep(zoneinfoClaim, snapshot.TimeZone, accountId);
        var changed = snapshot.Email != email || snapshot.DisplayName != displayName || snapshot.TimeZone != targetTimeZone;

        if (changed)
        {
            // RK10: bỏ QUA cửa sổ 5 phút — đồng bộ NGAY. ExecuteUpdateAsync phát MỘT câu UPDATE,
            // không đọc-sửa-ghi ⇒ hai request đồng thời của cùng user không cần khoá riêng (câu
            // UPDATE sau cùng thắng, luôn phản ánh claim mới nhất của chính request đó).
            await db.Users.Where(u => u.Id == accountId).ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Email, email)
                .SetProperty(u => u.DisplayName, displayName)
                .SetProperty(u => u.TimeZone, targetTimeZone)
                .SetProperty(u => u.LastSeenAt, now), ct);

            Cache[accountId] = new ProfileSnapshot(email, displayName, targetTimeZone, now);
            return accountId;
        }

        if (now - snapshot.CheckedAtUtc < CacheWindow)
            return accountId; // (c) giống ảnh chụp + trong 5 phút ⇒ bỏ qua hoàn toàn, không đụng DB

        // (d) giống ảnh chụp + đã qua 5 phút ⇒ chỉ cần cập nhật mốc truy cập gần nhất.
        await db.Users.Where(u => u.Id == accountId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.LastSeenAt, now), ct);
        Cache[accountId] = snapshot with { CheckedAtUtc = now };
        return accountId;
    }

    /// <summary>(a) Chưa có trong cache (lần đầu tiến trình thấy user này) ⇒ đọc thẳng DB — tạo mới nếu chưa có, đồng bộ/touch nếu đã có.</summary>
    private async Task<Guid> EnsureFromDatabaseAsync(
        Guid accountId, string email, string displayName, string? zoneinfoClaim, DateTime now, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == accountId, ct);
        if (user is null)
        {
            // Người dùng MỚI: không có múi giờ "cũ" nào để giữ ⇒ claim rỗng/hỏng về mặc định (khác nhánh đồng bộ người ĐÃ có ở trên).
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

    /// <summary>R4-4/RK36: claim rỗng hoặc không phải ID IANA hợp lệ (kể cả sau khi quy bí danh) ⇒ giữ <paramref name="fallback"/>, log Warning — KHÔNG chặn request.</summary>
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

    /// <summary>Ảnh chụp hồ sơ tại lần kiểm gần nhất — so với claim của request hiện tại để quyết định đồng bộ NGAY hay bỏ qua (RK10).</summary>
    private sealed record ProfileSnapshot(string Email, string DisplayName, string TimeZone, DateTime CheckedAtUtc);
}
