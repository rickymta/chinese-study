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

    /// <summary>
    /// M1 (RM-A4): origin trình duyệt được PHÉP gọi <c>/api/auth/mobile/*</c> — CHỈ có tác dụng
    /// khi môi trường là Development (dev web của app Flutter, cổng 3290 mặc định). Rỗng ⇒ chặn
    /// mọi request có Origin. Khác Development thì bị BỎ QUA hoàn toàn (Program.cs log Warning
    /// lúc khởi động nếu khác rỗng) — production không có lý do hợp lệ nào để trình duyệt gọi
    /// luồng mobile (app native không gửi Origin).
    /// </summary>
    public string[] MobileDevOrigins { get; init; } = [];

    /// <summary>M1 (RM-A9): rate limit riêng cho <c>auth-mobile</c>, cao hơn web (20) vì CGNAT của nhà mạng di động dồn nhiều máy vào một IP (RK-M14).</summary>
    public int MobileRateLimitPermitPerMinute { get; init; } = 30;
}
