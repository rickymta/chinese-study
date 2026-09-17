using AntFarm.Identity.Application.Common.Options;
using AntFarm.Security.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AntFarm.Identity.Api.Configuration;

/// <summary>
/// M1 (RM-A4): chặn TRÌNH DUYỆT ở luồng mobile — mọi request tới <c>/api/auth/mobile/*</c> có
/// header <c>Origin</c> chỉ được qua khi môi trường là Development VÀ origin nằm trong
/// <c>Auth:MobileDevOrigins</c> (dev web của app Flutter). App native (dio trên Android/iOS)
/// không gửi <c>Origin</c> nên không bị ảnh hưởng. Khác <see cref="ValidateOriginAttribute"/> của
/// web ở hai điểm: (1) đây là DANH SÁCH CHẶN chứ không phải danh sách cho phép — KHÔNG có Origin
/// thì luôn qua; (2) KHÔNG xét <c>Referer</c> khi thiếu Origin (RM-A4: chỉ Origin).
/// </summary>
public sealed class RejectBrowserOriginAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var origin = context.HttpContext.Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrEmpty(origin))
        {
            var environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            var authOptions = context.HttpContext.RequestServices.GetRequiredService<AuthOptions>();
            var normalizedOrigin = origin.TrimEnd('/');

            var allowed = environment.IsDevelopment()
                && authOptions.MobileDevOrigins.Contains(normalizedOrigin, StringComparer.OrdinalIgnoreCase);

            if (!allowed)
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AntFarm.Identity.MobileOriginGuard");
                logger.LogWarning(
                    "Chặn request trình duyệt (Origin={Origin}) tới luồng đăng nhập mobile {Path}",
                    normalizedOrigin, context.HttpContext.Request.Path);

                context.Result = new ObjectResult(new ErrorResponseMapper.ErrorBody(
                    "Luồng đăng nhập của ứng dụng di động không dùng được từ trình duyệt.", "ORIGIN_NOT_ALLOWED", null))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }

        await next();
    }
}
