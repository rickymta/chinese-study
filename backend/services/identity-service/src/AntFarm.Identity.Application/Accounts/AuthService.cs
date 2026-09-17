using System.Security.Cryptography;
using AntFarm.Core.Errors;
using AntFarm.Identity.Application.Admin;
using AntFarm.Identity.Application.Common;
using AntFarm.Identity.Application.Common.Abstractions;
using AntFarm.Identity.Application.Common.Options;
using AntFarm.Identity.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Identity.Application.Accounts;

/// <summary>
/// Đăng ký/đăng nhập/làm mới/đăng xuất (§5.2.2, R-A1..R-A13; M1 §3.1/§5.2.2 thêm kênh mobile).
/// <c>ILogger&lt;T&gt;</c> dùng được ở đây dù Application không FrameworkReference AspNetCore.App vì
/// <c>Microsoft.Extensions.Logging.Abstractions</c> là dependency bắc cầu của
/// <c>Microsoft.EntityFrameworkCore</c> (đã tham chiếu sẵn).
/// </summary>
public sealed class AuthService(
    IIdentityDbContext db,
    IPasswordHasherService passwordHasher,
    ITokenIssuer tokenIssuer,
    TimeProvider timeProvider,
    AuthOptions authOptions,
    JwtOptions jwtOptions,
    IRegistrationGate registrationGate,
    ILogger<AuthService> logger)
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, ClientContext context, CancellationToken ct)
    {
        // D-W4/W10: "đăng ký mở" là cài đặt RUNTIME (bảng identity.settings) — authOptions.AllowRegistration
        // giờ chỉ còn là giá trị KHỞI TẠO dùng khi chưa có dòng nào trong DB (đọc bên trong gate).
        var registrationState = await registrationGate.GetAsync(ct);
        if (!registrationState.Enabled)
            throw new ForbiddenException("Đăng ký hiện đang đóng.", "REGISTRATION_CLOSED");

        ValidateTimeZoneOrThrow(request.TimeZone);

        var normalized = Account.NormalizeEmail(request.Email);
        var exists = await db.Accounts.AnyAsync(a => a.EmailNormalized == normalized, ct);
        if (exists)
            throw new ConflictException("EMAIL_TAKEN", "Email đã được đăng ký.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = passwordHasher.HashPassword(request.Password);
        var account = Account.Register(request.Email, request.DisplayName, hash, request.TimeZone, now);

        db.Accounts.Add(account);

        var (refreshPlain, refreshEntity) = CreateRefreshToken(account.Id, Guid.CreateVersion7(), now, context);
        db.RefreshTokens.Add(refreshEntity);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsEmailUniqueViolation(ex))
        {
            // R-A1: check AnyAsync ở trên không chặn được ĐUA THẬT — hai request đăng ký cùng
            // email gần như đồng thời đều thấy "chưa tồn tại" trước khi cái nào SaveChanges
            // trước. Unique index DB (ix_accounts_email_normalized) là chốt chặn cuối cùng —
            // dịch lỗi ràng buộc (sẽ thành 500 mơ hồ nếu không bắt) thành 409 EMAIL_TAKEN quen
            // thuộc mà FE đã biết cách hiển thị.
            throw new ConflictException("EMAIL_TAKEN", "Email đã được đăng ký.");
        }

        var (accessToken, expiresAt) = tokenIssuer.IssueAccessToken(account);
        return new AuthResult(accessToken, expiresAt, refreshPlain, refreshEntity.ExpiresAt, AccountDto.From(account));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, ClientContext context, CancellationToken ct)
    {
        var normalized = Account.NormalizeEmail(request.Email);
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.EmailNormalized == normalized, ct);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // R-A8: thông báo không phân biệt sai email/sai mật khẩu.
        if (account is null)
            throw new UnauthenticatedException("Email hoặc mật khẩu không đúng.", "INVALID_CREDENTIALS");

        if (account.IsLockedOut(now))
            throw new LockedException("Tài khoản tạm khoá do đăng nhập sai nhiều lần liên tiếp.", new { lockedUntil = account.LockoutUntil });

        if (!account.IsActive)
            throw new ForbiddenException("Tài khoản đã bị khoá.", "ACCOUNT_DISABLED");

        var verify = passwordHasher.VerifyPassword(account.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            account.RegisterFailedLogin(now, authOptions.MaxFailedLogins, authOptions.LockoutMinutes);
            await db.SaveChangesAsync(ct);

            // R-A8: lần sai đủ để chạm ngưỡng phải báo 423 NGAY (không bắt người dùng thử thêm
            // một lần nữa mới biết đã bị khoá).
            if (account.IsLockedOut(now))
                throw new LockedException("Tài khoản tạm khoá do đăng nhập sai nhiều lần liên tiếp.", new { lockedUntil = account.LockoutUntil });

            throw new UnauthenticatedException("Email hoặc mật khẩu không đúng.", "INVALID_CREDENTIALS");
        }

        if (verify == PasswordVerificationResult.SuccessRehashNeeded)
            account.RehashPassword(passwordHasher.HashPassword(request.Password), now);

        account.RegisterSuccessfulLogin(now);
        await RemoveLongExpiredTokensAsync(account.Id, now, ct);

        var (refreshPlain, refreshEntity) = CreateRefreshToken(account.Id, Guid.CreateVersion7(), now, context);
        db.RefreshTokens.Add(refreshEntity);

        await db.SaveChangesAsync(ct);

        var (accessToken, expiresAt) = tokenIssuer.IssueAccessToken(account);
        return new AuthResult(accessToken, expiresAt, refreshPlain, refreshEntity.ExpiresAt, AccountDto.From(account));
    }

    /// <summary>
    /// Luồng xoay refresh token (R-A5/R-A6, §5.2.2; M1 thêm kiểm kênh RM-A3) — toàn bộ trong MỘT
    /// transaction, khoá dòng bằng <c>SELECT ... FOR UPDATE</c> để hai tab cùng làm mới gần như
    /// đồng thời không đua nhau tạo hai nhánh xoay khác nhau (RK7).
    /// </summary>
    public async Task<RefreshResult> RefreshAsync(string? tokenPlain, ClientContext context, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(tokenPlain))
            throw new UnauthenticatedException("Thiếu refresh token.", "REFRESH_INVALID");

        var hash = HashToken(tokenPlain);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        await using var transaction = await db.BeginTransactionAsync(ct);

        var token = await db.RefreshTokens
            .FromSqlInterpolated($"SELECT * FROM identity.refresh_tokens WHERE token_hash = {hash} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

        if (token is null || token.RevokedAt is not null || token.ExpiresAt <= now)
            throw new UnauthenticatedException("Refresh token không hợp lệ hoặc đã hết hạn.", "REFRESH_INVALID");

        // M1/RM-A3: token phát hành ở kênh khác (vd cookie web gửi tới /mobile/refresh, hoặc
        // ngược lại) KHÔNG được coi là dùng lại/đánh cắp — chỉ là gọi nhầm endpoint. Không thu hồi
        // gì (transaction không COMMIT, tự ROLLBACK khi `await using` dispose) để không đăng xuất
        // oan phiên đang hoạt động ở kênh đúng.
        if (token.ClientType != context.Type)
        {
            // ĐãXoay=true: token sai kênh này đã từng được xoay hợp lệ ở kênh gốc — dấu hiệu hữu
            // ích để phân biệt "gọi nhầm endpoint ngay từ token đầu" với "cầm token cũ nhầm kênh
            // sau khi đã dùng đúng kênh một thời gian" (không log token, chỉ log cờ boolean).
            logger.LogWarning(
                "Refresh token sai kênh {Expected}/{Actual} — họ {FamilyId} của tài khoản {AccountId}, đãXoay={DaXoay}",
                context.Type, token.ClientType, token.FamilyId, token.AccountId, token.RotatedAt is not null);
            throw new UnauthenticatedException("Refresh token không hợp lệ hoặc đã hết hạn.", "REFRESH_INVALID");
        }

        if (token.RotatedAt is not null)
        {
            var elapsedSinceRotation = now - token.RotatedAt.Value;
            if (elapsedSinceRotation.TotalSeconds > authOptions.RefreshReuseGraceSeconds)
            {
                await RevokeFamilyAsync(token.FamilyId, now, "reuse_detected", ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                logger.LogWarning(
                    "Phát hiện dùng lại refresh token đã xoay — thu hồi cả họ {FamilyId} của tài khoản {AccountId}",
                    token.FamilyId, token.AccountId);
                throw new UnauthenticatedException("Refresh token không hợp lệ hoặc đã hết hạn.", "REFRESH_INVALID");
            }
            // Trong cửa sổ ân hạn (hai tab cùng làm mới): cấp token mới CÙNG family, KHÔNG thu hồi.
        }

        var account = await db.Accounts.FirstAsync(a => a.Id == token.AccountId, ct);
        if (!account.IsActive)
        {
            await RevokeFamilyAsync(token.FamilyId, now, "account_disabled", ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            throw new ForbiddenException("Tài khoản đã bị khoá.", "ACCOUNT_DISABLED");
        }

        // RM-A3: token kế nhiệm kế thừa NGUYÊN kênh/clientApp/deviceName của token cha — chỉ
        // UserAgent/Ip cập nhật theo request xoay hiện tại (giống hành vi web trước M1).
        var (refreshPlain, newToken) = CreateRefreshToken(
            account.Id, token.FamilyId, now, token.ClientType, token.ClientApp, token.DeviceName, context.UserAgent, context.Ip);
        db.RefreshTokens.Add(newToken);

        if (token.RotatedAt is null)
            token.MarkRotated(now, newToken.Id);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var (accessToken, expiresAt) = tokenIssuer.IssueAccessToken(account);
        return new RefreshResult(accessToken, expiresAt, refreshPlain, newToken.ExpiresAt);
    }

    /// <summary>
    /// Đăng xuất. Web (<see cref="RefreshClientType.Web"/>) giữ hành vi cũ: chỉ thu hồi ĐÚNG token
    /// đang dùng. Mobile (RM-A7): một thiết bị = một họ ⇒ thu hồi CẢ HỌ. Token không tồn tại/đã
    /// thu hồi/thuộc kênh khác ⇒ bỏ qua im lặng (không lộ thông tin qua thời gian phản hồi/lỗi —
    /// caller luôn nhận 204).
    /// </summary>
    public async Task LogoutAsync(string? tokenPlain, RefreshClientType channel, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(tokenPlain))
            return;

        var hash = HashToken(tokenPlain);
        var token = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null || token.RevokedAt is not null || token.ClientType != channel)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (channel == RefreshClientType.Mobile)
            await RevokeFamilyAsync(token.FamilyId, now, "logout", ct);
        else
            token.Revoke(now, "logout");

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// D21/D22: dùng bởi <c>POST /api/auth/password</c> (web) và <c>POST /api/auth/mobile/password</c>
    /// (M1, RM-A8) để biết family hiện tại (KHÔNG tự đăng xuất phiên đang đổi mật khẩu). Chỉ tin
    /// token khi nó ACTIVE (chưa hết hạn/thu hồi), thuộc ĐÚNG tài khoản trong Bearer, VÀ đúng
    /// <paramref name="channel"/> — token của kênh khác/người khác/đã hỏng ⇒ null (AccountService
    /// thu hồi TẤT CẢ, an toàn hơn).
    /// </summary>
    public async Task<Guid?> GetActiveFamilyIdForAccountAsync(Guid accountId, string? tokenPlain, RefreshClientType channel, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(tokenPlain))
            return null;

        var hash = HashToken(tokenPlain);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var token = await db.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null || token.AccountId != accountId || token.ClientType != channel || !token.IsActive(now))
            return null;

        return token.FamilyId;
    }

    private static void ValidateTimeZoneOrThrow(string timeZone)
    {
        if (!TimeZoneValidation.IsValidIana(timeZone))
            throw new BusinessRuleException("INVALID_TIME_ZONE", $"Múi giờ '{timeZone}' không hợp lệ.");
    }

    /// <summary>
    /// Dò tên ràng buộc unique trong thông điệp lỗi thay vì kiểu Npgsql cụ thể (vd
    /// <c>Npgsql.PostgresException.SqlState == "23505"</c>) — Application không nên phụ thuộc
    /// thẳng provider DB (DDD 4 lớp); tên chỉ mục do CHÍNH ta đặt trong AccountConfiguration nên
    /// ổn định. Không khớp ⇒ để nguyên DbUpdateException bay lên thành 500 (lỗi thật, không che giấu).
    /// </summary>
    private static bool IsEmailUniqueViolation(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("ix_accounts_email_normalized", StringComparison.OrdinalIgnoreCase) == true;

    private async Task RevokeFamilyAsync(Guid familyId, DateTime now, string reason, CancellationToken ct)
    {
        var activeTokens = await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var t in activeTokens)
            t.Revoke(now, reason);
    }

    /// <summary>Dọn lười token hết hạn quá 7 ngày của chính tài khoản đăng nhập — tránh bảng phình vô hạn mà không cần job nền riêng.</summary>
    private async Task RemoveLongExpiredTokensAsync(Guid accountId, DateTime now, CancellationToken ct)
    {
        var cutoff = now.AddDays(-7);
        var stale = await db.RefreshTokens
            .Where(t => t.AccountId == accountId && t.ExpiresAt < cutoff)
            .ToListAsync(ct);

        db.RefreshTokens.RemoveRange(stale);
    }

    /// <summary>Đăng ký/đăng nhập: kênh/clientApp/deviceName/UA/IP đều lấy từ <see cref="ClientContext"/> của request hiện tại.</summary>
    private (string Plain, RefreshToken Entity) CreateRefreshToken(Guid accountId, Guid familyId, DateTime now, ClientContext context)
        => CreateRefreshToken(accountId, familyId, now, context.Type, context.ClientApp, context.DeviceName, context.UserAgent, context.Ip);

    private (string Plain, RefreshToken Entity) CreateRefreshToken(
        Guid accountId, Guid familyId, DateTime now, RefreshClientType clientType, string? clientApp, string? deviceName, string? userAgent, string? ip)
    {
        var plain = GenerateRandomToken();
        var hash = HashToken(plain);
        var entity = RefreshToken.CreateNew(
            accountId, familyId, hash, now, jwtOptions.RefreshTokenDays,
            clientType, clientApp, deviceName, Truncate(userAgent, 300), ip);
        return (plain, entity);
    }

    /// <summary>R-A5: 32 byte ngẫu nhiên, mã hex (64 ký tự) — dễ debug hơn base64url mà vẫn an toàn cho giá trị cookie.</summary>
    private static string GenerateRandomToken() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    /// <summary>DB chỉ lưu SHA-256 (R-A5) — char(64) hex thường.</summary>
    private static string HashToken(string token) => Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private static string? Truncate(string? value, int maxLength)
        => string.IsNullOrEmpty(value) ? value : value[..Math.Min(value.Length, maxLength)];
}
