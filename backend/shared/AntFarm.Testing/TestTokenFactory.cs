using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AntFarm.Testing;

/// <summary>
/// Ký JWT RS256 bằng khoá RSA tạm (sinh mới mỗi instance) — cho phép ApiTests của service ngôn
/// ngữ (chinese-backend từ F3) chấp nhận token mà KHÔNG cần identity-service chạy thật (§5.2.2,
/// §9.2). Test tự nối <see cref="PublicKey"/> vào <c>JwtBearerOptions</c> của chính service qua
/// <c>services.PostConfigure&lt;JwtBearerOptions&gt;(o =&gt; { o.ConfigurationManager = null;
/// o.Configuration = new OpenIdConnectConfiguration { Issuer = factory.Issuer };
/// o.Configuration.SigningKeys.Add(factory.PublicKey); })</c> — AntFarm.Testing cố tình KHÔNG
/// tham chiếu ASP.NET Core Authentication để giữ nhẹ, việc nối dây là của từng ApiFactory.
/// </summary>
public sealed class TestTokenFactory : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly SigningCredentials _signingCredentials;

    public TestTokenFactory(string issuer = "https://id.antfarms.xyz.test")
    {
        Issuer = issuer;
        var privateKey = new RsaSecurityKey(_rsa) { KeyId = "test-key" };
        _signingCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
        PublicKey = new RsaSecurityKey(_rsa.ExportParameters(includePrivateParameters: false)) { KeyId = "test-key" };
    }

    public string Issuer { get; }

    /// <summary>Chỉ phần công khai — nối vào <c>TokenValidationParameters.IssuerSigningKeys</c> phía kiểm token.</summary>
    public SecurityKey PublicKey { get; }

    /// <summary>Ký một access token hợp lệ theo đúng khuôn claim §6.2 — mặc định hết hạn sau 15 phút giống identity-service thật.</summary>
    public string CreateToken(
        Guid? accountId = null,
        string email = "hocvien@vidu.com",
        string displayName = "Học viên",
        string timeZone = "Asia/Ho_Chi_Minh",
        IEnumerable<string>? audiences = null,
        DateTime? issuedAt = null,
        DateTime? notBefore = null,
        DateTime? expiresAt = null)
    {
        var now = issuedAt ?? DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            IssuedAt = now,
            NotBefore = notBefore ?? now,
            Expires = expiresAt ?? now.AddMinutes(15),
            SigningCredentials = _signingCredentials,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = (accountId ?? Guid.NewGuid()).ToString(),
                ["email"] = email,
                ["name"] = displayName,
                ["zoneinfo"] = timeZone,
                ["jti"] = Guid.NewGuid().ToString(),
                ["aud"] = (audiences ?? ["af-identity", "af-chinese"]).ToArray()
            }
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public void Dispose() => _rsa.Dispose();
}
