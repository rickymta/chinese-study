using System.Text.RegularExpressions;
using AntFarm.Security.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AntFarm.Identity.Api.Configuration;

/// <summary>
/// M1 (RM-A5): header <c>X-AF-Client</c> bắt buộc ở mọi endpoint mobile — CHỈ để nhận diện
/// nền tảng/phiên bản cho log/thống kê (giả được, KHÔNG phải kiểm soát an ninh). Đặt chuỗi đã
/// kiểm hợp lệ (cắt 64 ký tự) vào <c>HttpContext.Items["af.client"]</c> để controller đọc mà
/// không phải parse lại.
/// </summary>
public sealed partial class RequireClientHeaderAttribute : Attribute, IAsyncActionFilter
{
    public const string ItemsKey = "af.client";

    // vd "chinese-mobile/1.0.0+1 (android)" — tên app / phiên bản (semver hoặc build number) / nền tảng.
    [GeneratedRegex(@"^[a-z0-9-]{1,32}/[0-9A-Za-z.+-]{1,32} \((android|ios|web)\)$")]
    private static partial Regex ClientPattern();

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var header = context.HttpContext.Request.Headers["X-AF-Client"].FirstOrDefault();
        if (string.IsNullOrEmpty(header) || !ClientPattern().IsMatch(header))
        {
            var details = new Dictionary<string, string[]> { ["X-AF-Client"] = ["Thiếu hoặc sai định dạng header X-AF-Client."] };
            context.Result = new ObjectResult(new ErrorResponseMapper.ErrorBody("Dữ liệu gửi lên không hợp lệ.", "VALIDATION", details))
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            return Task.CompletedTask;
        }

        context.HttpContext.Items[ItemsKey] = header.Length > 64 ? header[..64] : header;
        return next();
    }
}
