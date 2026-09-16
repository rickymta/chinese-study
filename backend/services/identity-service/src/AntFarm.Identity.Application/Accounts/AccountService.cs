using AntFarm.Core.Errors;
using AntFarm.Identity.Application.Common;
using AntFarm.Identity.Application.Common.Abstractions;
using AntFarm.Identity.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Identity.Application.Accounts;

/// <summary>Hồ sơ tài khoản (§6.2 GET/PUT /api/account). Đổi mật khẩu KHÔNG nằm ở đây — chuyển sang <c>POST /api/auth/password</c> (D21, AuthController) vì cookie <c>af_rt</c> chỉ gửi tới path /api/auth.</summary>
public sealed class AccountService(IIdentityDbContext db, IPasswordHasherService passwordHasher, TimeProvider timeProvider)
{
    public async Task<AccountDto> GetAsync(Guid accountId, CancellationToken ct)
        => AccountDto.From(await GetEntityAsync(accountId, ct));

    public async Task<AccountDto> UpdateProfileAsync(Guid accountId, string displayName, string timeZone, CancellationToken ct)
    {
        ValidateTimeZoneOrThrow(timeZone);

        var account = await GetEntityAsync(accountId, ct);
        account.UpdateProfile(displayName, timeZone, timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);

        return AccountDto.From(account);
    }

    /// <summary>
    /// D21/D22: sai mật khẩu hiện tại ⇒ 422 WRONG_PASSWORD (KHÔNG tính vào đếm khoá đăng nhập
    /// R-A8 — đó là quy tắc riêng của đăng NHẬP). Mật khẩu mới trùng mật khẩu hiện tại ⇒ 422
    /// PASSWORD_UNCHANGED. Thành công: thu hồi mọi HỌ refresh token KHÁC họ hiện tại (R-A12,
    /// giữ phiên đang đổi mật khẩu sống) — <paramref name="currentFamilyId"/> null (cookie
    /// thiếu/không thuộc tài khoản này/đã hết hạn) ⇒ thu hồi TẤT CẢ, an toàn hơn.
    /// </summary>
    public async Task<ChangePasswordResult> ChangePasswordAsync(
        Guid accountId, string currentPassword, string newPassword, Guid? currentFamilyId, CancellationToken ct)
    {
        var account = await GetEntityAsync(accountId, ct);

        var verifyCurrent = passwordHasher.VerifyPassword(account.PasswordHash, currentPassword);
        if (verifyCurrent == Common.Abstractions.PasswordVerificationResult.Failed)
            throw new BusinessRuleException("WRONG_PASSWORD", "Mật khẩu hiện tại không đúng.");

        var verifyNew = passwordHasher.VerifyPassword(account.PasswordHash, newPassword);
        if (verifyNew != Common.Abstractions.PasswordVerificationResult.Failed)
            throw new BusinessRuleException("PASSWORD_UNCHANGED", "Mật khẩu mới phải khác mật khẩu hiện tại.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        account.ChangePassword(passwordHasher.HashPassword(newPassword), now);

        var query = db.RefreshTokens.Where(t => t.AccountId == accountId && t.RevokedAt == null);
        if (currentFamilyId is not null)
            query = query.Where(t => t.FamilyId != currentFamilyId.Value);

        var tokensToRevoke = await query.ToListAsync(ct);
        var revokedFamilyCount = tokensToRevoke.Select(t => t.FamilyId).Distinct().Count();
        foreach (var t in tokensToRevoke)
            t.Revoke(now, "password_changed");

        await db.SaveChangesAsync(ct);

        return new ChangePasswordResult(revokedFamilyCount, CurrentSessionKept: currentFamilyId is not null);
    }

    private async Task<Account> GetEntityAsync(Guid accountId, CancellationToken ct)
        => await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

    private static void ValidateTimeZoneOrThrow(string timeZone)
    {
        if (!TimeZoneValidation.IsValidIana(timeZone))
            throw new BusinessRuleException("INVALID_TIME_ZONE", $"Múi giờ '{timeZone}' không hợp lệ.");
    }
}
