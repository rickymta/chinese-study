namespace AntFarm.Identity.Domain.Accounts;

/// <summary>
/// Refresh token xoay vòng (R-A5/R-A6). DB chỉ lưu <see cref="TokenHash"/> (SHA-256 hex) —
/// KHÔNG bao giờ lưu token gốc. <see cref="FamilyId"/> nối các token được xoay từ cùng một
/// phiên đăng nhập; phát hiện dùng lại token đã xoay ⇒ thu hồi CẢ họ (R-A6).
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid FamilyId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RotatedAt { get; private set; }
    public Guid? ReplacedById { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokeReason { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CreatedIp { get; private set; }

    /// <summary>M1 (RM-A3): kênh phát hành — mặc định <c>Web</c> (dòng cũ trước M1 qua migration <c>DEFAULT 'web'</c>).</summary>
    public RefreshClientType ClientType { get; private set; } = RefreshClientType.Web;

    /// <summary>M1 (RM-A5): chuỗi <c>X-AF-Client</c> đã kiểm định dạng — CHỈ để log/thống kê, không phải khoá bảo mật. <c>null</c> cho kênh web.</summary>
    public string? ClientApp { get; private set; }

    /// <summary>M1 (RM-A6): tên thiết bị tuỳ chọn do client gửi lúc đăng ký/đăng nhập mobile.</summary>
    public string? DeviceName { get; private set; }

    private RefreshToken()
    {
    }

    /// <summary>
    /// Phát token mới. <paramref name="familyId"/> = <c>Guid.CreateVersion7()</c> mới khi đăng
    /// nhập/đăng ký; giữ nguyên family cũ khi xoay vòng. <paramref name="clientType"/>/
    /// <paramref name="clientApp"/>/<paramref name="deviceName"/>: đăng ký/đăng nhập lấy từ
    /// <c>ClientContext</c> của request hiện tại; xoay vòng PHẢI truyền lại 3 trường của token
    /// CHA (kế thừa kênh, RM-A3) — không tự suy từ request đang xoay.
    /// </summary>
    public static RefreshToken CreateNew(
        Guid accountId, Guid familyId, string tokenHash, DateTime now, int refreshTokenDays,
        RefreshClientType clientType, string? clientApp, string? deviceName,
        string? userAgent, string? createdIp)
    {
        return new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(refreshTokenDays),
            UserAgent = userAgent,
            CreatedIp = createdIp,
            ClientType = clientType,
            ClientApp = clientApp,
            DeviceName = deviceName
        };
    }

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;

    public void MarkRotated(DateTime now, Guid replacedById)
    {
        RotatedAt = now;
        ReplacedById = replacedById;
    }

    public void Revoke(DateTime now, string reason)
    {
        RevokedAt = now;
        RevokeReason = reason;
    }
}
