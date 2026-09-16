using AntFarm.Identity.Domain.Accounts;

namespace AntFarm.Identity.Application.Accounts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName, string TimeZone);

public sealed record LoginRequest(string Email, string Password);

public sealed record UpdateProfileRequest(string DisplayName, string TimeZone);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Hình dạng "account" trong mọi phản hồi §6.2 (register/login/refresh trả accessToken riêng, không lặp lại ở đây).</summary>
public sealed record AccountDto(Guid Id, string Email, string DisplayName, string TimeZone, DateTime CreatedAt)
{
    public static AccountDto From(Account account) => new(account.Id, account.Email, account.DisplayName, account.TimeZone, account.CreatedAt);
}

/// <summary>Kết quả nghiệp vụ của đăng ký/đăng nhập — <see cref="RefreshTokenPlain"/> KHÔNG đưa vào JSON, chỉ dùng để Api đặt cookie (R-A7).</summary>
public sealed record AuthResult(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshTokenPlain, AccountDto Account);

public sealed record RefreshResult(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshTokenPlain);

/// <summary>D21/D22 — §6.2 (bản sửa 17/09/2026): POST /api/auth/password trả số HỌ phiên khác bị thu hồi + có giữ được phiên hiện tại hay không.</summary>
public sealed record ChangePasswordResult(int OtherSessionsRevoked, bool CurrentSessionKept);
