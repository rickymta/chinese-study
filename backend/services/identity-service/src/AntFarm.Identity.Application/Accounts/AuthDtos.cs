using AntFarm.Identity.Domain.Accounts;

namespace AntFarm.Identity.Application.Accounts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName, string TimeZone);

public sealed record LoginRequest(string Email, string Password);

public sealed record UpdateProfileRequest(string DisplayName, string TimeZone);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>M1 §6.1 — client mobile dùng endpoint riêng, cùng luật nghiệp vụ đăng ký của web + <c>DeviceName</c> tuỳ chọn (RM-A6).</summary>
public sealed record MobileRegisterRequest(string Email, string Password, string DisplayName, string TimeZone, string? DeviceName);

public sealed record MobileLoginRequest(string Email, string Password, string? DeviceName);

/// <summary>Refresh token nằm trong body (không cookie, RM-A2) — 64 ký tự hex thường.</summary>
public sealed record MobileRefreshRequest(string RefreshToken);

public sealed record MobileLogoutRequest(string RefreshToken);

/// <summary><c>RefreshToken</c> tuỳ chọn để xác định họ phiên mobile hiện tại (RM-A8) — thiếu ⇒ thu hồi mọi họ như D22.</summary>
public sealed record MobileChangePasswordRequest(string CurrentPassword, string NewPassword, string? RefreshToken);

/// <summary>Hình dạng "account" trong mọi phản hồi §6.2 (register/login/refresh trả accessToken riêng, không lặp lại ở đây).</summary>
public sealed record AccountDto(Guid Id, string Email, string DisplayName, string TimeZone, DateTime CreatedAt)
{
    public static AccountDto From(Account account) => new(account.Id, account.Email, account.DisplayName, account.TimeZone, account.CreatedAt);
}

/// <summary>
/// Kết quả nghiệp vụ của đăng ký/đăng nhập — <see cref="RefreshTokenPlain"/> KHÔNG đưa vào JSON
/// của luồng web (Api chỉ dùng nó để đặt cookie, R-A7); luồng mobile (M1) trả thẳng
/// <see cref="RefreshTokenPlain"/> + <see cref="RefreshTokenExpiresAt"/> trong body (RM-A2).
/// </summary>
public sealed record AuthResult(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshTokenPlain, DateTime RefreshTokenExpiresAt, AccountDto Account);

public sealed record RefreshResult(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshTokenPlain, DateTime RefreshTokenExpiresAt);

/// <summary>D21/D22 — §6.2 (bản sửa 17/09/2026): POST /api/auth/password trả số HỌ phiên khác bị thu hồi + có giữ được phiên hiện tại hay không.</summary>
public sealed record ChangePasswordResult(int OtherSessionsRevoked, bool CurrentSessionKept);
