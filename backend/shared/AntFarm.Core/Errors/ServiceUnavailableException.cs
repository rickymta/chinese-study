namespace AntFarm.Core.Errors;

/// <summary>
/// 503 — phụ thuộc bên ngoài (học liệu, dịch vụ khác) chưa sẵn sàng, KHÔNG phải lỗi nội bộ chưa
/// dự kiến (500). F5: <c>PinyinCatalogLoader</c> nạp học liệu thất bại lúc khởi động ⇒ endpoint
/// pinyin ném lỗi này (code <c>CONTENT_UNAVAILABLE</c>) thay vì sập cả service (§5.2.1).
/// </summary>
public sealed class ServiceUnavailableException(string code, string message, object? details = null) : AppException(message, details)
{
    public override string Code { get; } = code;
    public override int StatusCode => 503;
}
