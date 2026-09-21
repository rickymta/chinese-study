namespace AntFarm.Cms.Domain.Site;

/// <summary>
/// Một dòng nhật ký thao tác ghi trên cms-backend (schema <c>site.audit_logs</c>, §5.1.2 W3a) —
/// KHÔNG chép giá trị dài/Markdown vào <see cref="Summary"/> (§5.2.3), chỉ tóm tắt ngắn.
/// </summary>
public sealed class AuditLog
{
    private const int MaxSummaryLength = 500;

    public Guid Id { get; private set; }
    public DateTime At { get; private set; }
    public Guid ActorId { get; private set; }
    public string ActorEmail { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string TargetType { get; private set; } = null!;
    public string? TargetId { get; private set; }
    public string Summary { get; private set; } = null!;
    public bool Success { get; private set; }

    // EF Core cần constructor không tham số — không lộ ra ngoài assembly để buộc luôn tạo qua Create().
    private AuditLog()
    {
    }

    public static AuditLog Create(
        DateTime at, Guid actorId, string actorEmail, string action, string targetType, string? targetId,
        string summary, bool success) => new()
    {
        Id = Guid.CreateVersion7(),
        At = at,
        ActorId = actorId,
        ActorEmail = actorEmail,
        Action = action,
        TargetType = targetType,
        TargetId = targetId,
        Summary = summary.Length > MaxSummaryLength ? summary[..MaxSummaryLength] : summary,
        Success = success
    };
}
