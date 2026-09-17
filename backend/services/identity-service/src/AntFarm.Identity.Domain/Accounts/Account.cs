namespace AntFarm.Identity.Domain.Accounts;

/// <summary>
/// Tài khoản đăng nhập (R-N1: identity-service CHỈ xác thực, không chứa vai trò/quyền của
/// service ngôn ngữ nào). <see cref="Id"/> chính là claim "sub" phát vào access token.
/// </summary>
public sealed class Account
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string EmailNormalized { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string TimeZone { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTime? LockoutUntil { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime PasswordChangedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core cần constructor không tham số (dùng qua reflection, không lộ ra ngoài assembly để
    // buộc code khác luôn tạo Account qua Register()).
    private Account()
    {
    }

    public static Account Register(string email, string displayName, string passwordHash, string timeZone, DateTime now)
    {
        var normalized = NormalizeEmail(email);
        return new Account
        {
            Id = Guid.CreateVersion7(),
            Email = email.Trim(),
            EmailNormalized = normalized,
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            TimeZone = timeZone,
            IsActive = true,
            FailedLoginCount = 0,
            PasswordChangedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>R-A1: so khớp email không phân biệt hoa thường, bỏ khoảng trắng thừa.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public bool IsLockedOut(DateTime now) => LockoutUntil is not null && LockoutUntil.Value > now;

    /// <summary>R-A8: sai mật khẩu <paramref name="maxFailedLogins"/> lần liên tiếp ⇒ khoá <paramref name="lockoutMinutes"/> phút.</summary>
    public void RegisterFailedLogin(DateTime now, int maxFailedLogins, int lockoutMinutes)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= maxFailedLogins)
        {
            LockoutUntil = now.AddMinutes(lockoutMinutes);
            FailedLoginCount = 0; // đã khoá ⇒ đếm lại từ 0, tránh khoá dồn dập nối tiếp
        }

        UpdatedAt = now;
    }

    public void RegisterSuccessfulLogin(DateTime now)
    {
        FailedLoginCount = 0;
        LockoutUntil = null;
        LastLoginAt = now;
        UpdatedAt = now;
    }

    /// <summary>Băm lại mật khẩu khi <c>PasswordHasher</c> báo cần rehash — KHÔNG coi là người dùng chủ động đổi mật khẩu nên không cập nhật <see cref="PasswordChangedAt"/> (R-A12 không áp dụng).</summary>
    public void RehashPassword(string newPasswordHash, DateTime now)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = now;
    }

    /// <summary>Người dùng chủ động đổi mật khẩu — R-A12 (thu hồi refresh token) xử lý ở Application, không phải Domain.</summary>
    public void ChangePassword(string newPasswordHash, DateTime now)
    {
        PasswordHash = newPasswordHash;
        PasswordChangedAt = now;
        UpdatedAt = now;
    }

    public void UpdateProfile(string displayName, string timeZone, DateTime now)
    {
        DisplayName = displayName.Trim();
        TimeZone = timeZone;
        UpdatedAt = now;
    }
}
