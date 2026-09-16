namespace AntFarm.Core.Errors;

/// <summary>403 — không đủ quyền hoặc bị chặn nghiệp vụ (vd <c>ACCOUNT_DISABLED</c>, §6.0).</summary>
/// <param name="code">Mã lỗi cụ thể, mặc định <c>FORBIDDEN</c>.</param>
public sealed class ForbiddenException(string message, string code = "FORBIDDEN", object? details = null)
    : AppException(message, details)
{
    public override string Code { get; } = code;
    public override int StatusCode => 403;
}
