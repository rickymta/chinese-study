using AntFarm.Identity.Application.Common.Options;
using AntFarm.Identity.Domain.Accounts;
using AntFarm.Identity.Infrastructure.Security;
using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace AntFarm.Identity.UnitTests.Security;

/// <summary>§5.2.2/§6.2 — TokenIssuer: đủ claim, kid trong header, aud là mảng, hết hạn theo TimeProvider giả.</summary>
public class TokenIssuerTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public void IssueAccessToken_DuClaimDungKhuon()
    {
        var dir = Directory.CreateTempSubdirectory("antfarm-tokenissuer-test-").FullName;
        try
        {
            var signingKeyStore = new FileSigningKeyStore(dir, activeKeyId: null, isDevelopment: true);
            var now = new DateTimeOffset(2026, 9, 17, 8, 0, 0, TimeSpan.Zero);
            var timeProvider = new FixedTimeProvider(now);
            var jwtOptions = new JwtOptions
            {
                Issuer = "https://id.antfarms.xyz",
                Audiences = ["af-identity", "af-chinese"],
                AccessTokenMinutes = 15,
                RefreshTokenDays = 30,
                KeysPath = dir
            };
            var issuer = new TokenIssuer(signingKeyStore, jwtOptions, timeProvider);
            var account = Account.Register("hoc@vidu.com", "Học viên", "hash", "Asia/Ho_Chi_Minh", now.UtcDateTime);

            var (accessToken, expiresAt) = issuer.IssueAccessToken(account);

            expiresAt.Should().Be(now.UtcDateTime.AddMinutes(15));

            var jwt = new JsonWebTokenHandler().ReadJsonWebToken(accessToken);
            jwt.Kid.Should().NotBeNullOrWhiteSpace();
            jwt.Issuer.Should().Be("https://id.antfarms.xyz");
            jwt.Audiences.Should().BeEquivalentTo(["af-identity", "af-chinese"]);
            jwt.Subject.Should().Be(account.Id.ToString());
            jwt.TryGetClaim("email", out var emailClaim).Should().BeTrue();
            emailClaim.Value.Should().Be("hoc@vidu.com");
            jwt.TryGetClaim("name", out var nameClaim).Should().BeTrue();
            nameClaim.Value.Should().Be("Học viên");
            jwt.TryGetClaim("zoneinfo", out var tzClaim).Should().BeTrue();
            tzClaim.Value.Should().Be("Asia/Ho_Chi_Minh");
            jwt.TryGetClaim("jti", out var jtiClaim).Should().BeTrue();
            jtiClaim.Value.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
