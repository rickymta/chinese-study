namespace AntFarm.Identity.Application.Common.Options;

/// <summary>Bind từ section "Auth" — cookie refresh, CORS có credentials, đăng ký, khoá tài khoản (§5.2.0.6, §5.6.2, R-A7/R-A7b/R-A8/R-A10).</summary>
public sealed class AuthOptions
{
    public bool AllowRegistration { get; init; } = true;
    public string RefreshCookieName { get; init; } = "af_rt";
    public required string RefreshCookiePath { get; init; }

    /// <summary>Rỗng ở dev (cookie host-only "localhost" — trình duyệt từ chối Domain=.localhost); ".antfarms.xyz" ở production.</summary>
    public string RefreshCookieDomain { get; init; } = "";

    public bool RefreshCookieSecure { get; init; }
    public int RefreshReuseGraceSeconds { get; init; } = 30;
    public int MaxFailedLogins { get; init; } = 10;
    public int LockoutMinutes { get; init; } = 15;

    /// <summary>R-A7b: CORS có credentials + kiểm Origin mọi POST /api/auth, /api/account. Rỗng ⇒ chặn hết (an toàn theo mặc định).</summary>
    public string[] AllowedOrigins { get; init; } = [];

    /// <summary>R-A9: 20 req/phút/MỖI IP (Program.cs partition rate limiter "auth" theo Connection.RemoteIpAddress qua RateLimitPartition — không phải một ngân sách dùng chung cho mọi người gọi). Tách ra cấu hình để ApiTests nới rộng: TestServer thường chỉ có một địa chỉ IP (RemoteIpAddress null/loopback) cho MỌI request nên vẫn cần permit cao để nhiều test không tự đụng nhau.</summary>
    public int RateLimitPermitPerMinute { get; init; } = 20;
}
