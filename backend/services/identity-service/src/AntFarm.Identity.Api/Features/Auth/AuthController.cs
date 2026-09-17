using AntFarm.Auth;
using AntFarm.Core.Errors;
using AntFarm.Identity.Api.Configuration;
using AntFarm.Identity.Application.Accounts;
using AntFarm.Identity.Application.Common.Options;
using AntFarm.Identity.Domain.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AntFarm.Identity.Api.Features.Auth;

/// <summary>
/// Đăng ký/đăng nhập/làm mới/đăng xuất + đổi mật khẩu (§6.2, D21). Mọi action ở đây "nhạy cảm
/// cookie" nên đều <see cref="ValidateOriginAttribute"/> + rate limit "auth" (R-A7b, R-A9).
/// Đổi mật khẩu đặt CÙNG controller (không phải AccountController) vì cookie <c>af_rt</c> có
/// <c>Path=/api/auth</c> — đặt ở <c>/api/account/*</c> thì trình duyệt không gửi cookie kèm
/// theo, identity không biết được "họ" phiên hiện tại để giữ lại (D21/RK34).
/// </summary>
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
[ValidateOrigin]
public sealed class AuthController(
    AuthService authService,
    AccountService accountService,
    AuthOptions authOptions,
    JwtOptions jwtOptions) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ClientContext.Web(GetUserAgent(), GetClientIp()), ct);
        SetRefreshCookie(result.RefreshTokenPlain);
        return StatusCode(StatusCodes.Status201Created, ToResponse(result));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ClientContext.Web(GetUserAgent(), GetClientIp()), ct);
        SetRefreshCookie(result.RefreshTokenPlain);
        return Ok(ToResponse(result));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshResponse>> Refresh(CancellationToken ct)
    {
        var cookieToken = Request.Cookies[authOptions.RefreshCookieName];
        try
        {
            var result = await authService.RefreshAsync(cookieToken, ClientContext.Web(GetUserAgent(), GetClientIp()), ct);
            SetRefreshCookie(result.RefreshTokenPlain);
            return Ok(new RefreshResponse(result.AccessToken, result.AccessTokenExpiresAt));
        }
        catch (AppException)
        {
            // CHỈ bắt AppException (401 REFRESH_INVALID, 403 ACCOUNT_DISABLED — lỗi NGHIỆP VỤ dự
            // kiến) để xoá cookie chết, giữ lại cookie chỉ khiến lần refresh kế tiếp lại lỗi y
            // hệt. KHÔNG bắt mọi exception: lỗi hạ tầng ngoài dự kiến (vd mất kết nối DB tạm
            // thời) không có nghĩa refresh token đã hỏng — xoá cookie trong trường hợp đó ép
            // người dùng đăng nhập lại oan dù phiên vẫn còn hợp lệ (review F2 17/09/2026).
            DeleteRefreshCookie();
            throw;
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var cookieToken = Request.Cookies[authOptions.RefreshCookieName];
        await authService.LogoutAsync(cookieToken, RefreshClientType.Web, ct);
        DeleteRefreshCookie();
        return NoContent();
    }

    /// <summary>D21: route mới thay <c>POST /api/account/password</c> của §6.2 gốc.</summary>
    [HttpPost("password")]
    [Authorize]
    public async Task<ActionResult<ChangePasswordResponse>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var accountId = User.GetAccountId()!.Value;
        var cookieToken = Request.Cookies[authOptions.RefreshCookieName];
        var currentFamilyId = await authService.GetActiveFamilyIdForAccountAsync(accountId, cookieToken, RefreshClientType.Web, ct);

        var result = await accountService.ChangePasswordAsync(accountId, request.CurrentPassword, request.NewPassword, currentFamilyId, ct);

        return Ok(new ChangePasswordResponse(result.OtherSessionsRevoked, result.CurrentSessionKept));
    }

    private static AuthResponse ToResponse(AuthResult result) => new(result.AccessToken, result.AccessTokenExpiresAt, result.Account);

    private string? GetUserAgent() => Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private void SetRefreshCookie(string refreshTokenPlain)
    {
        Response.Cookies.Append(authOptions.RefreshCookieName, refreshTokenPlain, BuildCookieOptions());
    }

    private void DeleteRefreshCookie()
    {
        // Xoá cookie phải dùng ĐÚNG Domain + Path lúc đặt — lệch là cookie cũ còn nguyên (§5.2.2).
        Response.Cookies.Delete(authOptions.RefreshCookieName, BuildCookieOptions());
    }

    private CookieOptions BuildCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = authOptions.RefreshCookieSecure,
        SameSite = SameSiteMode.Strict,
        Path = authOptions.RefreshCookiePath,
        Domain = string.IsNullOrEmpty(authOptions.RefreshCookieDomain) ? null : authOptions.RefreshCookieDomain,
        Expires = DateTimeOffset.UtcNow.AddDays(jwtOptions.RefreshTokenDays)
    };
}
