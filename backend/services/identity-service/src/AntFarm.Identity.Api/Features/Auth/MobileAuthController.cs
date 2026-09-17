using System.Globalization;
using AntFarm.Auth;
using AntFarm.Identity.Api.Configuration;
using AntFarm.Identity.Application.Accounts;
using AntFarm.Identity.Domain.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AntFarm.Identity.Api.Features.Auth;

/// <summary>
/// M1 §3.1/§5.2/§6.1 — luồng phiên riêng cho client mobile (app Flutter `mobile/`): refresh token
/// trả THẲNG trong body JSON (RM-A2), KHÔNG đọc/đặt/xoá cookie <c>af_rt</c> dù trình duyệt có gửi
/// kèm cookie hay không (RM-A1 — cookie web bị LỜ ĐI hoàn toàn ở đây). Luồng cookie của web
/// (<see cref="AuthController"/>) giữ nguyên, không đổi.
///
/// Nghiệp vụ đăng ký/đăng nhập/đổi mật khẩu DÙNG LẠI <see cref="AuthService"/>/<see cref="AccountService"/>
/// của web (không nhân bản thuật toán) — controller chỉ khác ở: (1) map DTO mobile ⇆ DTO dùng
/// chung qua <see cref="ClientContext"/>, (2) trả token trong body thay vì cookie, (3) filter
/// riêng <see cref="RejectBrowserOriginAttribute"/>/<see cref="RequireClientHeaderAttribute"/>,
/// (4) rate limit policy <c>auth-mobile</c> riêng (RM-A9), (5) <see cref="DisableCorsAttribute"/> —
/// luồng mobile không dành cho trình duyệt (RM-A4) nên KHÔNG công bố CORS: preflight
/// (<c>OPTIONS</c> kèm <c>Access-Control-Request-Method</c>) từ bất kỳ origin nào cũng không nhận
/// được <c>Access-Control-Allow-Origin</c> ⇒ trình duyệt tự chặn NGAY tại bước preflight, không
/// cần đợi tới <see cref="RejectBrowserOriginAttribute"/> (vốn chỉ chạy được nếu preflight lọt qua).
/// </summary>
[ApiController]
[Route("api/auth/mobile")]
[EnableRateLimiting("auth-mobile")]
[RejectBrowserOrigin]
[RequireClientHeader]
[DisableCors]
public sealed class MobileAuthController(AuthService authService, AccountService accountService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<MobileAuthResponse>> Register([FromBody] MobileRegisterRequest request, CancellationToken ct)
    {
        var context = BuildContext(NormalizeDeviceName(request.DeviceName));
        var result = await authService.RegisterAsync(
            new RegisterRequest(request.Email, request.Password, request.DisplayName, request.TimeZone), context, ct);

        SetNoStore();
        return StatusCode(StatusCodes.Status201Created, ToResponse(result));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<MobileAuthResponse>> Login([FromBody] MobileLoginRequest request, CancellationToken ct)
    {
        var context = BuildContext(NormalizeDeviceName(request.DeviceName));
        var result = await authService.LoginAsync(new LoginRequest(request.Email, request.Password), context, ct);

        SetNoStore();
        return Ok(ToResponse(result));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<MobileRefreshResponse>> Refresh([FromBody] MobileRefreshRequest request, CancellationToken ct)
    {
        // Không có deviceName/clientApp ở đây theo THIẾT KẾ — token xoay kế thừa 3 trường của
        // token CHA (RM-A3, AuthService.RefreshAsync), không lấy từ request xoay hiện tại.
        var context = BuildContext(deviceName: null);
        var result = await authService.RefreshAsync(request.RefreshToken, context, ct);

        SetNoStore();
        return Ok(new MobileRefreshResponse(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshTokenPlain, result.RefreshTokenExpiresAt));
    }

    /// <summary>RM-A7: luôn 204 (trừ 400/403/429 chung) — không lộ token có tồn tại/hợp lệ hay không. Không cần Bearer.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] MobileLogoutRequest request, CancellationToken ct)
    {
        await authService.LogoutAsync(request.RefreshToken, RefreshClientType.Mobile, ct);
        return NoContent();
    }

    /// <summary>RM-A8: Bearer audience `af-identity` (cùng scheme với web) — refreshToken tuỳ chọn để giữ đúng phiên hiện tại.</summary>
    [HttpPost("password")]
    [Authorize]
    public async Task<ActionResult<ChangePasswordResponse>> ChangePassword([FromBody] MobileChangePasswordRequest request, CancellationToken ct)
    {
        var accountId = User.GetAccountId()!.Value;
        var currentFamilyId = await authService.GetActiveFamilyIdForAccountAsync(accountId, request.RefreshToken, RefreshClientType.Mobile, ct);

        var result = await accountService.ChangePasswordAsync(accountId, request.CurrentPassword, request.NewPassword, currentFamilyId, ct);

        return Ok(new ChangePasswordResponse(result.OtherSessionsRevoked, result.CurrentSessionKept));
    }

    private static MobileAuthResponse ToResponse(AuthResult result)
        => new(result.AccessToken, result.AccessTokenExpiresAt, result.RefreshTokenPlain, result.RefreshTokenExpiresAt, result.Account);

    private ClientContext BuildContext(string? deviceName)
        => new(RefreshClientType.Mobile, GetClientApp(), deviceName, GetUserAgent(), GetClientIp());

    private string? GetClientApp() => HttpContext.Items[RequireClientHeaderAttribute.ItemsKey] as string;

    private string? GetUserAgent() => Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>RM-A10: response chứa token không được cache (kể cả bởi proxy trung gian).</summary>
    private void SetNoStore()
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Pragma"] = "no-cache";
    }

    /// <summary>
    /// RM-A6: trim, bỏ ký tự điều khiển + ký tự định dạng Unicode (category <c>Cf</c> — dấu
    /// zero-width, đánh dấu chiều chữ như U+202E RIGHT-TO-LEFT OVERRIDE) vốn không hiển thị nhưng
    /// có thể dùng để giả mạo/che nội dung tên thiết bị hiện trong log/UI quản trị; cắt 100 ký
    /// tự; chuỗi rỗng sau khi lọc ⇒ null (không lưu chuỗi rỗng vô nghĩa).
    /// </summary>
    private static string? NormalizeDeviceName(string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
            return null;

        var filtered = new string(deviceName.Trim()
            .Where(c => !char.IsControl(c) && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.Format)
            .ToArray());
        if (filtered.Length == 0)
            return null;

        return filtered.Length > 100 ? filtered[..100] : filtered;
    }
}
