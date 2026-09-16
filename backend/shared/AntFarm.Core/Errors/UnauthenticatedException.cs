namespace AntFarm.Core.Errors;

/// <summary>401 — thiếu/hỏng xác thực (vd <c>INVALID_CREDENTIALS</c>, <c>REFRESH_INVALID</c>, §6.0).</summary>
/// <param name="code">Mã lỗi cụ thể, mặc định <c>UNAUTHENTICATED</c>.</param>
public sealed class UnauthenticatedException(string message, string code = "UNAUTHENTICATED", object? details = null)
    : AppException(message, details)
{
    public override string Code { get; } = code;
    public override int StatusCode => 401;
}
