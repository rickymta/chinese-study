using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AntFarm.HealthChecks;

public static class HealthCheckExtensions
{
    /// <summary>
    /// Đăng ký health check "self" (tag "live") — luôn khoẻ, chỉ chứng minh tiến trình còn sống.
    /// Service tự thêm check "postgres" (tag "ready") qua <c>AddDbContextCheck&lt;TContext&gt;</c>
    /// trong <c>AddInfrastructure</c> của chính nó.
    /// </summary>
    public static IServiceCollection AddAfHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Service is running."), tags: ["live"]);
        return services;
    }

    /// <summary>
    /// Map ba endpoint theo quy ước Kubernetes probe:
    ///   GET /health/live  — liveness (chỉ "self")
    ///   GET /health/ready — readiness (mọi check tag "ready", vd DB)
    ///   GET /health       — báo cáo đầy đủ
    ///
    /// Cả ba đều <c>AllowAnonymous</c> BẮT BUỘC — nếu service đặt
    /// <c>FallbackPolicy = RequireAuthenticatedUser()</c> (từ F3) mà quên AllowAnonymous ở đây,
    /// HEALTHCHECK của Dockerfile (dùng wget, không có Bearer token) sẽ nhận 401 và container bị
    /// đánh dấu unhealthy vĩnh viễn dù service chạy hoàn toàn bình thường (bài học MedDental 28/08/2026).
    /// </summary>
    public static IEndpointRouteBuilder MapAfHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteJsonAsync,
            AllowCachingResponses = false
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteJsonAsync,
            AllowCachingResponses = false
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteJsonAsync,
            AllowCachingResponses = false
        }).AllowAnonymous();

        return endpoints;
    }

    private static async Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        using var ms = new MemoryStream();
        await using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("status", report.Status.ToString());
            writer.WriteString("totalDuration", report.TotalDuration.ToString());

            writer.WriteStartObject("checks");
            foreach (var (name, entry) in report.Entries)
            {
                writer.WriteStartObject(name);
                writer.WriteString("status", entry.Status.ToString());
                writer.WriteString("duration", entry.Duration.ToString());
                writer.WriteString("description", entry.Description ?? string.Empty);
                if (entry.Exception is not null)
                    writer.WriteString("exception", entry.Exception.Message);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        await context.Response.WriteAsync(Encoding.UTF8.GetString(ms.ToArray()));
    }
}
