using Microsoft.AspNetCore.Builder;

namespace AntFarm.Security.Middleware;

public static class MiddlewareExtensions
{
    /// <summary>Gắn header bảo mật cơ bản. Gọi sớm trong pipeline, trước UseRouting().</summary>
    public static IApplicationBuilder UseAfSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();

    /// <summary>Lan truyền X-Correlation-ID + đẩy vào LogContext. Gọi trước log request.</summary>
    public static IApplicationBuilder UseAfCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
