namespace AntFarm.Core.Errors;

/// <summary>409 — xung đột dữ liệu (trùng khoá duy nhất, ghi đè lạc hậu...).</summary>
/// <param name="code">Mã lỗi cụ thể, vd <c>EMAIL_TAKEN</c>, <c>CONCURRENCY_CONFLICT</c> (§6.0).</param>
public sealed class ConflictException(string code, string message, object? details = null) : AppException(message, details)
{
    public override string Code { get; } = code;
    public override int StatusCode => 409;
}
