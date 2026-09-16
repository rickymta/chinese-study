using AntFarm.Core.Errors;

namespace AntFarm.Security.Errors;

/// <summary>
/// Hàm thuần dịch <see cref="Exception"/> sang body lỗi thống nhất (§6.0) — không phụ
/// thuộc HttpContext nên unit test trực tiếp được, không cần dựng WebApplicationFactory.
/// </summary>
public static class ErrorResponseMapper
{
    public sealed record ErrorBody(string Error, string Code, object? Details);

    public static (int StatusCode, ErrorBody Body) Map(Exception exception)
    {
        if (exception is AppException appException)
            return (appException.StatusCode, new ErrorBody(appException.Message, appException.Code, appException.Details));

        // Lỗi không dự kiến: KHÔNG lộ ex.Message ra client — có thể chứa chi tiết nội bộ
        // (connection string, đường dẫn file...). Log đầy đủ ở tầng gọi, chỉ trả câu chung.
        return (500, new ErrorBody("Đã xảy ra lỗi nội bộ.", "INTERNAL_ERROR", null));
    }
}
