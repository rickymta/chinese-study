using AntFarm.Identity.Application.Common.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AntFarm.Identity.Api.Internal;

/// <summary>
/// Thi hành <see cref="InternalAccessPolicy"/> (§5.2.9, R-W4). Đặt trong pipeline NGAY SAU
/// <c>UseAfExceptionHandler()</c>, TRƯỚC <c>UseIdentityCors</c>/<c>UseRateLimiter</c>/
/// <c>UseAuthentication</c> — route nội bộ không CORS, không rate limit "auth", không JWT (xác
/// thực bằng khoá dịch vụ tĩnh <c>X-Service-Key</c>).
/// </summary>
public sealed class InternalAccessMiddleware(
    RequestDelegate next,
    InternalOptions options,
    ILocalPortAccessor localPortAccessor,
    ILogger<InternalAccessMiddleware> logger)
{
    private const string ServiceKeyHeader = "X-Service-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        var isInternalPath = context.Request.Path.StartsWithSegments("/internal");
        var localPort = localPortAccessor.GetLocalPort(context);
        var providedKey = context.Request.Headers.TryGetValue(ServiceKeyHeader, out var value) ? value.ToString() : null;

        var decision = InternalAccessPolicy.Evaluate(isInternalPath, localPort, options, providedKey);

        switch (decision)
        {
            case InternalAccessDecision.NotFound:
                // KHÔNG body — 404 trơn để không lộ thông tin (§5.2.9).
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;

            case InternalAccessDecision.Unauthorized:
                // Log IP nguồn + path — KHÔNG BAO GIỜ log khoá (đúng lẫn sai).
                logger.LogWarning(
                    "Khoá dịch vụ không hợp lệ từ {RemoteIp} tới {Path}",
                    context.Connection.RemoteIpAddress, context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new { error = "Khoá dịch vụ không hợp lệ.", code = "SERVICE_KEY_INVALID" });
                return;

            default: // PassThrough | Allow
                await next(context);
                return;
        }
    }
}
