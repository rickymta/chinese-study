using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace AntFarm.Security.Middleware;

/// <summary>
/// Đọc/sinh <c>X-Correlation-ID</c>, ghi lại vào response, và đẩy vào
/// <see cref="LogContext"/> để mọi dòng log của request này (kể cả log do
/// AntFarm.Logging in ra) mang cùng một mã tương quan.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..16];

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
