using AntFarm.Identity.Application.Accounts;

namespace AntFarm.Identity.Api.Features.Auth;

public sealed record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, AccountDto Account);

public sealed record RefreshResponse(string AccessToken, DateTime AccessTokenExpiresAt);

public sealed record ChangePasswordResponse(int OtherSessionsRevoked, bool CurrentSessionKept);
