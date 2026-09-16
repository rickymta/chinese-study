using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AntFarm.Security.Errors;

/// <summary>Middleware xử lý ngoại lệ + chuẩn hoá lỗi 400 do model binding/validation, dùng chung mọi service.</summary>
public static class ExceptionHandlingExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Bắt mọi exception chưa xử lý, dịch qua <see cref="ErrorResponseMapper"/> thành JSON
    /// { error, code, details } (§6.0). <see cref="AntFarm.Core.Errors.AppException"/> log Warning
    /// (lỗi nghiệp vụ dự kiến), còn lại log Error kèm exception đầy đủ — nhưng KHÔNG lộ ra client.
    /// </summary>
    public static IApplicationBuilder UseAfExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = feature?.Error ?? new Exception("Không xác định được ngoại lệ.");

                var (statusCode, body) = ErrorResponseMapper.Map(exception);

                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AntFarm.ExceptionHandler");

                if (statusCode >= 500)
                    logger.LogError(exception, "Lỗi nội bộ chưa xử lý tại {Path}", context.Request.Path);
                else
                    logger.LogWarning("Lỗi nghiệp vụ {Code} tại {Path}: {Message}", body.Code, context.Request.Path, body.Error);

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(body, JsonOptions);
            });
        });

        return app;
    }

    /// <summary>
    /// Ghi đè phản hồi mặc định (Problem Details) của model binding/validation lỗi
    /// thành { error, code: "VALIDATION", details: { field: [thông điệp...] } } (§6.0).
    /// </summary>
    public static IServiceCollection AddAfInvalidModelStateResponse(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var details = context.ModelState
                    .Where(kv => kv.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                var body = new ErrorResponseMapper.ErrorBody("Dữ liệu gửi lên không hợp lệ.", "VALIDATION", details);
                return new BadRequestObjectResult(body)
                {
                    ContentTypes = { "application/json" }
                };
            };
        });

        return services;
    }
}
