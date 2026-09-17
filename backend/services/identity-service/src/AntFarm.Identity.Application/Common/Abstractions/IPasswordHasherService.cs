namespace AntFarm.Identity.Application.Common.Abstractions;

public enum PasswordVerificationResult
{
    Failed,
    Success,

    /// <summary>Đúng mật khẩu nhưng thuật toán/tham số băm đã lạc hậu — gọi lại <see cref="IPasswordHasherService.HashPassword"/> và lưu lại.</summary>
    SuccessRehashNeeded
}

/// <summary>
/// Bọc <c>PasswordHasher&lt;TUser&gt;</c> của ASP.NET Core Identity (R-A2) — interface thuần để
/// Application (không FrameworkReference AspNetCore.App) không phụ thuộc thẳng kiểu đó.
/// </summary>
public interface IPasswordHasherService
{
    string HashPassword(string password);

    PasswordVerificationResult VerifyPassword(string passwordHash, string providedPassword);
}
