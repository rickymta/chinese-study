namespace AntFarm.Identity.Application.Common.Options;

/// <summary>Bind từ section "Jwt" (§5.2.0.6, §6.2). POCO thuần — đăng ký DI như singleton INSTANCE (không dùng IOptions&lt;T&gt;) để Application (không FrameworkReference AspNetCore.App) vẫn tiêm thẳng được.</summary>
public sealed class JwtOptions
{
    public required string Issuer { get; init; }
    public required string[] Audiences { get; init; }
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
    public required string KeysPath { get; init; }
    public string? ActiveKeyId { get; init; }
}
