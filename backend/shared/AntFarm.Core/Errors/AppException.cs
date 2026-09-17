namespace AntFarm.Core.Errors;

/// <summary>
/// Lớp cơ sở cho mọi lỗi nghiệp vụ có chủ đích của AntFarm.
/// <see cref="AntFarm.Security.Errors.ErrorResponseMapper"/> dịch instance này
/// thành body JSON { error, code, details } — xem §6.0 hợp đồng thực thi.
/// </summary>
public abstract class AppException(string message, object? details = null) : Exception(message)
{
    /// <summary>Mã lỗi ngắn gọn, SNAKE_CASE, để frontend switch theo (không phải i18n key).</summary>
    public abstract string Code { get; }

    /// <summary>Mã HTTP tương ứng.</summary>
    public abstract int StatusCode { get; }

    /// <summary>Dữ liệu phụ trợ (vd { "field": ["thông điệp"] }), tuỳ loại lỗi.</summary>
    public object? Details { get; } = details;
}
