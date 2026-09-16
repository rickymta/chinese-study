using System.Security.Claims;

namespace AntFarm.Auth;

/// <summary>Đọc claim chuẩn từ access token (§6.2) — dùng ở mọi service đã <c>AddAfJwtBearer</c>.</summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid? GetAccountId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public static string? GetEmail(this ClaimsPrincipal user)
        => user.FindFirstValue("email") ?? user.FindFirstValue(ClaimTypes.Email);

    public static string? GetDisplayName(this ClaimsPrincipal user)
        => user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name);

    /// <summary>ID múi giờ IANA (R-T2), claim "zoneinfo" — KHÔNG có mặc định ở đây, service ngôn ngữ tự quyết khi thiếu.</summary>
    public static string? GetTimeZone(this ClaimsPrincipal user) => user.FindFirstValue("zoneinfo");
}
