using AntFarm.Identity.Application.Common.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AntFarm.Identity.Api.Configuration;

/// <summary>
/// R-A7b: mọi POST của /api/auth/* và /api/account/* phải có header Origin (hoặc Referer khi
/// thiếu) khớp <c>Auth:AllowedOrigins</c> — CORS của trình duyệt chỉ chặn ĐỌC phản hồi, không
/// chặn request được GỬI đi, nên cookie <c>af_rt</c> (Domain=.antfarms.xyz, gửi được từ MỌI
/// subdomain) vẫn tới được server nếu không kiểm thêm ở đây (RK18: subdomain bị chiếm quyền vẫn
/// gửi được request kèm cookie tới identity, dù trình duyệt sẽ không cho JS đọc phản hồi).
/// </summary>
public sealed class ValidateOriginAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var authOptions = context.HttpContext.RequestServices.GetRequiredService<AuthOptions>();
        var origin = ExtractOrigin(context.HttpContext.Request);

        if (origin is null || !authOptions.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("AntFarm.Identity.OriginGuard");
            logger.LogWarning(
                "Chặn request vì Origin không hợp lệ: {Origin} tại {Path}",
                origin ?? "(trống)", context.HttpContext.Request.Path);

            context.Result = new ObjectResult(new { error = "Nguồn gọi không được phép.", code = "ORIGIN_NOT_ALLOWED" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }

    private static string? ExtractOrigin(HttpRequest request)
    {
        var origin = request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrEmpty(origin))
            return origin.TrimEnd('/');

        var referer = request.Headers.Referer.FirstOrDefault();
        if (string.IsNullOrEmpty(referer))
            return null;

        return Uri.TryCreate(referer, UriKind.Absolute, out var uri) ? $"{uri.Scheme}://{uri.Authority}" : null;
    }
}
