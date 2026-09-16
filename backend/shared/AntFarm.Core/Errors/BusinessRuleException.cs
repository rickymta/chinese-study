namespace AntFarm.Core.Errors;

/// <summary>422 — vi phạm quy tắc nghiệp vụ (vd <c>LAST_ADMIN</c>, <c>INVALID_TIME_ZONE</c>, §6.0).</summary>
/// <param name="code">Mã lỗi nghiệp vụ cụ thể.</param>
public sealed class BusinessRuleException(string code, string message, object? details = null) : AppException(message, details)
{
    public override string Code { get; } = code;
    public override int StatusCode => 422;
}
