using AntFarm.Core.Errors;
using AntFarm.Identity.Application.Common.Abstractions;
using AntFarm.Identity.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Identity.Application.Admin;

/// <summary>
/// Nghiệp vụ quản trị tài khoản qua API nội bộ (§5.2.9, W10) — người gọi DUY NHẤT dự kiến là
/// cms-backend (W11) qua khoá dịch vụ tĩnh, KHÔNG phải trình duyệt. identity-service vẫn KHÔNG có
/// vai trò/quyền của riêng nó (R-W3) — "ai được phép gọi API này" là chuyện của cms-backend
/// (quyền <c>accounts.manage</c>), ở đây chỉ có nghiệp vụ tài khoản + chặn actor tự thao tác lên
/// chính mình (R-W17, <c>SELF_ACTION_FORBIDDEN</c>).
/// </summary>
public sealed class AccountAdminService(
    IIdentityDbContext db,
    IPasswordHasherService passwordHasher,
    ITemporaryPasswordGenerator temporaryPasswordGenerator,
    TimeProvider timeProvider,
    ILogger<AccountAdminService> logger)
{
    public async Task<AdminAccountListResult> ListAsync(string? q, string? status, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is >= 1 and <= 100 ? pageSize : 20;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var query = db.Accounts.AsNoTracking().AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            // Hạ cả hai vế về chữ thường rồi LIKE thường (không ILIKE của Npgsql) để Application
            // không cần thêm gói Npgsql.EntityFrameworkCore.PostgreSQL riêng (DDD 4 lớp) — cùng
            // khuôn UserAdminService của chinese-backend. Escape %/_/\ để ký tự đặc biệt người
            // dùng gõ không biến thành wildcard ngoài ý muốn.
            var pattern = BuildLikePattern(trimmed.ToLowerInvariant());
            query = query.Where(a =>
                EF.Functions.Like(a.EmailNormalized, pattern, "\\") ||
                EF.Functions.Like(a.DisplayName.ToLower(), pattern, "\\"));
        }

        query = status switch
        {
            "active" => query.Where(a => a.IsActive && !(a.LockoutUntil != null && a.LockoutUntil > now)),
            "disabled" => query.Where(a => !a.IsActive),
            "locked" => query.Where(a => a.IsActive && a.LockoutUntil != null && a.LockoutUntil > now),
            _ => query
        };

        var totalCount = await query.CountAsync(ct);
        var accounts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new AdminAccountListResult(accounts.Select(ToListItem).ToList(), page, pageSize, totalCount);
    }

    public async Task<AdminAccountDto> GetAsync(Guid id, CancellationToken ct)
    {
        var account = await GetEntityAsync(id, ct);
        return ToDetailDto(account, await CountActiveSessionsAsync(id, ct));
    }

    public async Task<AdminAccountDto> DisableAsync(Guid actorId, Guid id, CancellationToken ct)
    {
        EnsureNotSelf(actorId, id);
        var account = await GetEntityAsync(id, ct);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        account.Disable(now);
        await RevokeAllActiveTokensAsync(id, now, "admin_disabled", ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Admin {ActorId} khoá tài khoản {AccountId}", actorId, id);
        return ToDetailDto(account, await CountActiveSessionsAsync(id, ct));
    }

    public async Task<AdminAccountDto> EnableAsync(Guid actorId, Guid id, CancellationToken ct)
    {
        var account = await GetEntityAsync(id, ct);
        account.Enable(timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Admin {ActorId} mở khoá tài khoản {AccountId}", actorId, id);
        return ToDetailDto(account, await CountActiveSessionsAsync(id, ct));
    }

    public async Task<AdminAccountDto> ClearLockoutAsync(Guid actorId, Guid id, CancellationToken ct)
    {
        var account = await GetEntityAsync(id, ct);
        account.ClearLockout(timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Admin {ActorId} gỡ khoá đăng nhập sai của tài khoản {AccountId}", actorId, id);
        return ToDetailDto(account, await CountActiveSessionsAsync(id, ct));
    }

    /// <summary>
    /// <paramref name="newPassword"/> rỗng/null ⇒ sinh mật khẩu tạm 16 ký tự (trả về trong
    /// <see cref="ResetPasswordResult.TemporaryPassword"/>); có giá trị ⇒ dùng đúng mật khẩu đó,
    /// <c>TemporaryPassword</c> trả về null (§6.5). Độ dài <paramref name="newPassword"/> đã được
    /// <see cref="ResetPasswordRequestValidator"/> kiểm ở tầng Api trước khi tới đây.
    /// </summary>
    public async Task<ResetPasswordResult> ResetPasswordAsync(Guid actorId, Guid id, string? newPassword, CancellationToken ct)
    {
        EnsureNotSelf(actorId, id);
        var account = await GetEntityAsync(id, ct);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        string? temporaryPassword = null;
        var passwordPlain = newPassword;
        if (string.IsNullOrEmpty(passwordPlain))
        {
            temporaryPassword = temporaryPasswordGenerator.Generate();
            passwordPlain = temporaryPassword;
        }

        account.ResetPasswordByAdmin(passwordHasher.HashPassword(passwordPlain), now);
        var revoked = await RevokeAllActiveTokensAsync(id, now, "admin_password_reset", ct);
        await db.SaveChangesAsync(ct);

        // RW10: KHÔNG log mật khẩu/mật khẩu tạm — chỉ log AI đã thao tác và LÊN tài khoản nào.
        logger.LogInformation("Admin {ActorId} đặt lại mật khẩu tài khoản {AccountId}", actorId, id);
        return new ResetPasswordResult(temporaryPassword, revoked);
    }

    public async Task<RevokeSessionsResult> RevokeSessionsAsync(Guid actorId, Guid id, CancellationToken ct)
    {
        EnsureNotSelf(actorId, id);
        await GetEntityAsync(id, ct); // 404 nếu không có tài khoản
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var revoked = await RevokeAllActiveTokensAsync(id, now, "admin_revoked", ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Admin {ActorId} thu hồi phiên của tài khoản {AccountId}", actorId, id);
        return new RevokeSessionsResult(revoked);
    }

    /// <summary>R-W17: admin không được tự khoá/reset mật khẩu/thu hồi phiên của CHÍNH mình qua API nội bộ (tự khoá mình ⇒ mất luôn quyền gọi tiếp).</summary>
    private static void EnsureNotSelf(Guid actorId, Guid targetId)
    {
        if (actorId == targetId)
            throw new BusinessRuleException("SELF_ACTION_FORBIDDEN", "Không thể tự thao tác lên chính tài khoản của mình.");
    }

    private async Task<Account> GetEntityAsync(Guid id, CancellationToken ct)
        => await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

    /// <summary>Đếm theo FAMILY (không theo dòng refresh_tokens) — mỗi họ xoay vòng là MỘT phiên đăng nhập logic, khớp field "activeSessionCount" (§6.5).</summary>
    private async Task<int> CountActiveSessionsAsync(Guid accountId, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await db.RefreshTokens.AsNoTracking()
            .Where(t => t.AccountId == accountId && t.RevokedAt == null && t.RotatedAt == null && t.ExpiresAt > now)
            .Select(t => t.FamilyId)
            .Distinct()
            .CountAsync(ct);
    }

    /// <summary>Thu hồi MỌI refresh token chưa thu hồi của tài khoản — trả số HỌ (session) đã thu hồi, khớp "revokedSessions" (§6.5).</summary>
    private async Task<int> RevokeAllActiveTokensAsync(Guid accountId, DateTime now, string reason, CancellationToken ct)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.AccountId == accountId && t.RevokedAt == null)
            .ToListAsync(ct);

        var familyCount = tokens.Select(t => t.FamilyId).Distinct().Count();
        foreach (var t in tokens)
            t.Revoke(now, reason);

        return familyCount;
    }

    private static AdminAccountListItemDto ToListItem(Account a) => new(
        a.Id, a.Email, a.DisplayName, a.TimeZone, a.IsActive, a.LockoutUntil, a.LastLoginAt, a.CreatedAt);

    private static AdminAccountDto ToDetailDto(Account a, int activeSessionCount) => new(
        a.Id, a.Email, a.DisplayName, a.TimeZone, a.IsActive, a.LockoutUntil, a.LastLoginAt, a.CreatedAt,
        a.PasswordChangedAt, activeSessionCount);

    private static string BuildLikePattern(string value) =>
        $"%{value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
}
