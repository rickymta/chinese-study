namespace AntFarm.Identity.ApiTests.Infrastructure;

/// <summary>M1 (RM-A9) — permit THẤP có chủ đích riêng cho policy <c>auth-mobile</c>, tách khỏi <see cref="IdentityDbApiFactoryLowRateLimit"/> (policy <c>auth</c> của web) để chứng minh hai policy đếm ĐỘC LẬP.</summary>
public sealed class IdentityDbApiFactoryLowMobileRateLimit : IdentityDbApiFactory
{
    public IdentityDbApiFactoryLowMobileRateLimit() => Environment.SetEnvironmentVariable("Auth__MobileRateLimitPermitPerMinute", "3");
}
