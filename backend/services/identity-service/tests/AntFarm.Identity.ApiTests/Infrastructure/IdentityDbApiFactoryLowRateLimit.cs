namespace AntFarm.Identity.ApiTests.Infrastructure;

/// <summary>Permit thấp CÓ CHỦ ĐÍCH — dùng riêng cho test kiểm 429 (R-A9); các factory khác nới permit rất cao để KHÔNG tự đụng rate limit khi test gọi liên tiếp.</summary>
public sealed class IdentityDbApiFactoryLowRateLimit : IdentityDbApiFactory
{
    public IdentityDbApiFactoryLowRateLimit() => Environment.SetEnvironmentVariable("Auth__RateLimitPermitPerMinute", "3");
}
