namespace AntFarm.Core.Errors;

/// <summary>404 — không tìm thấy tài nguyên.</summary>
public sealed class NotFoundException(string message, object? details = null) : AppException(message, details)
{
    public override string Code => "NOT_FOUND";
    public override int StatusCode => 404;
}
