using AntFarm.Identity.Domain.Accounts;

namespace AntFarm.Identity.Application.Accounts;

/// <summary>
/// M1: mô tả client đang gọi luồng xác thực — dùng để tạo/kiểm kênh của refresh token
/// (<see cref="RefreshClientType"/>, RM-A3). Web luôn dùng <see cref="Web"/> (không
/// <c>ClientApp</c>/<c>DeviceName</c> — cookie không cần nhận diện nền tảng); mobile do
/// <c>MobileAuthController</c> dựng từ header <c>X-AF-Client</c> + body <c>deviceName</c>.
/// </summary>
public sealed record ClientContext(RefreshClientType Type, string? ClientApp, string? DeviceName, string? UserAgent, string? Ip)
{
    public static ClientContext Web(string? userAgent, string? ip) => new(RefreshClientType.Web, null, null, userAgent, ip);
}
