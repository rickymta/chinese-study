namespace AntFarm.Chinese.Domain.Access;

/// <summary>
/// Người dùng cục bộ của chinese-backend (schema `access`, §5.1.2). <see cref="Id"/> = claim
/// "sub" của access token (cùng giá trị với identity.accounts.id) — R-N3: KHÔNG lưu mật khẩu/
/// refresh token ở đây, identity-service sở hữu xác thực (R-N1); bảng này chỉ phục vụ PHÂN QUYỀN
/// cục bộ + hiển thị (email/tên/múi giờ là BẢN SAO đồng bộ từ token, không phải nguồn sự thật).
/// </summary>
public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string TimeZone { get; private set; } = null!;
    public DateTime FirstSeenAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }

    // EF Core cần constructor không tham số — không lộ ra ngoài assembly để buộc luôn tạo qua Provision().
    private User()
    {
    }

    /// <summary>R-P4: tạo dòng user lần đầu request có token hợp lệ mà "sub" chưa có trong access.users.</summary>
    public static User Provision(Guid id, string email, string displayName, string timeZone, DateTime now) => new()
    {
        Id = id,
        Email = email,
        DisplayName = displayName,
        TimeZone = timeZone,
        FirstSeenAt = now,
        LastSeenAt = now
    };

    /// <summary>So khớp claim hiện tại với bản ghi đã lưu — dùng để quyết định có cần <see cref="SyncProfile"/> hay chỉ <see cref="Touch"/>.</summary>
    public bool NeedsProfileSync(string email, string displayName, string timeZone) =>
        Email != email || DisplayName != displayName || TimeZone != timeZone;

    /// <summary>R-P4: đồng bộ email/tên/múi giờ khi claim token khác bản ghi (đổi hồ sơ ở identity-service).</summary>
    public void SyncProfile(string email, string displayName, string timeZone, DateTime now)
    {
        Email = email;
        DisplayName = displayName;
        TimeZone = timeZone;
        LastSeenAt = now;
    }

    /// <summary>Không có gì đổi — chỉ cập nhật mốc truy cập gần nhất.</summary>
    public void Touch(DateTime now) => LastSeenAt = now;
}
