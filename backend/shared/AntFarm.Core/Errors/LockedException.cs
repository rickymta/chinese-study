namespace AntFarm.Core.Errors;

/// <summary>423 — tài khoản tạm khoá do đăng nhập sai nhiều lần liên tiếp (R-A8, §6.0).</summary>
public sealed class LockedException(string message, object? details = null) : AppException(message, details)
{
    public override string Code => "ACCOUNT_LOCKED";
    public override int StatusCode => 423;
}
