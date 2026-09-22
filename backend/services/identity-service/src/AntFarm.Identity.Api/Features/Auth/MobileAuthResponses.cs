using AntFarm.Identity.Application.Accounts;

namespace AntFarm.Identity.Api.Features.Auth;

/// <summary>M1 §6.1 — register/login mobile: refresh token trả THẲNG trong body (RM-A2, không cookie).</summary>
public sealed record MobileAuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt, AccountDto Account);

public sealed record MobileRefreshResponse(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);
