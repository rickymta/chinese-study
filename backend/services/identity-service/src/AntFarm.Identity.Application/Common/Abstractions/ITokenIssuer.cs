using AntFarm.Identity.Domain.Accounts;

namespace AntFarm.Identity.Application.Common.Abstractions;

/// <summary>Phát access token JWT RS256 theo đúng khuôn claim §6.2.</summary>
public interface ITokenIssuer
{
    (string AccessToken, DateTime ExpiresAt) IssueAccessToken(Account account);
}
