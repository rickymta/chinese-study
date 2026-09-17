using AntFarm.Identity.Domain.Accounts;
using Microsoft.AspNetCore.Identity;
using AppPasswordVerificationResult = AntFarm.Identity.Application.Common.Abstractions.PasswordVerificationResult;
using IAppPasswordHasherService = AntFarm.Identity.Application.Common.Abstractions.IPasswordHasherService;

namespace AntFarm.Identity.Infrastructure.Security;

/// <summary>
/// Bọc <see cref="PasswordHasher{TUser}"/> của ASP.NET Core Identity (R-A2, PBKDF2) — không tự
/// viết thuật toán băm. Alias tránh đụng tên với <see cref="PasswordVerificationResult"/> của
/// chính ASP.NET Core Identity (cùng using trong file này).
/// </summary>
public sealed class PasswordHasherService : IAppPasswordHasherService
{
    private readonly PasswordHasher<Account> _hasher = new();

    public string HashPassword(string password) => _hasher.HashPassword(null!, password);

    public AppPasswordVerificationResult VerifyPassword(string passwordHash, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(null!, passwordHash, providedPassword);
        return result switch
        {
            PasswordVerificationResult.Success => AppPasswordVerificationResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => AppPasswordVerificationResult.SuccessRehashNeeded,
            _ => AppPasswordVerificationResult.Failed
        };
    }
}
