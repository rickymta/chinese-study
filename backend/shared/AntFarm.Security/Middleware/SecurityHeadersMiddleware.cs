using Microsoft.AspNetCore.Http;

namespace AntFarm.Security.Middleware;

/// <summary>
/// Gắn các header bảo mật cơ bản vào mọi response. Đơn giản hơn CSP đầy đủ của
/// MedDental vì AntFarm F0 chưa có trang đăng nhập/OAuth cần CSP form-action.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";

        await next(context);
    }
}
