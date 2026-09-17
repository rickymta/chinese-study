using AntFarm.Identity.Application.Common.Abstractions;
using AntFarm.Identity.Application.Common.Options;
using AntFarm.Identity.Domain.Accounts;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AntFarm.Identity.Infrastructure.Security;

/// <summary>Phát access token JWT RS256 theo đúng khuôn claim §6.2 (iss, aud[], sub, email, name, zoneinfo, jti, iat, nbf, exp).</summary>
public sealed class TokenIssuer(ISigningKeyStore signingKeyStore, JwtOptions jwtOptions, TimeProvider timeProvider) : ITokenIssuer
{
    private static readonly JsonWebTokenHandler Handler = new();

    public (string AccessToken, DateTime ExpiresAt) IssueAccessToken(Account account)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(jwtOptions.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = signingKeyStore.GetActiveSigningCredentials(),
            // "aud" đặt qua Claims (không dùng SecurityTokenDescriptor.Audience) vì Audience chỉ
            // nhận MỘT chuỗi — R-A4 yêu cầu "aud" là MẢNG nhiều audience (["af-identity","af-chinese"]).
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = account.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = account.Email,
                ["name"] = account.DisplayName,
                ["zoneinfo"] = account.TimeZone,
                [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
                ["aud"] = jwtOptions.Audiences
            }
        };

        var token = Handler.CreateToken(descriptor);
        return (token, expires);
    }
}
