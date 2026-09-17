namespace AntFarm.Core.Errors;

/// <summary>
/// 400 — dữ liệu gửi lên sai nhưng CHỈ phát hiện được sau khi đọc DB (vd F9: <c>optionId</c> không
/// thuộc danh sách lựa chọn của MỘT câu hỏi cụ thể — FluentValidation không biết được vì không có DB
/// context). Cùng mã <c>VALIDATION</c> với <c>AddAfInvalidModelStateResponse</c> (kiểm hình dạng tĩnh)
/// để frontend xử lý nhất quán một mã lỗi duy nhất (§6.0).
/// </summary>
public sealed class ValidationAppException(string message, object? details = null) : AppException(message, details)
{
    public override string Code => "VALIDATION";
    public override int StatusCode => 400;
}
