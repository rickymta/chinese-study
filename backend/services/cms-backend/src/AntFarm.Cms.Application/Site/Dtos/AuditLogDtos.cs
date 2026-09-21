namespace AntFarm.Cms.Application.Site.Dtos;

/// <summary>Một dòng nhật ký thao tác (§6.2 W3b).</summary>
public sealed record AuditLogDto(
    Guid Id, DateTime At, Guid ActorId, string ActorEmail, string Action,
    string TargetType, string? TargetId, string Summary, bool Success);

/// <summary>Trang kết quả <c>GET /api/admin/audit-logs</c> (§6.2 W3b).</summary>
public sealed record AuditLogsPageDto(IReadOnlyList<AuditLogDto> Items, int Page, int PageSize, int TotalCount);

/// <summary>Query string <c>GET /api/admin/audit-logs?targetType=&amp;targetId=&amp;page=&amp;pageSize=</c> (§5.2.3 W3b) — property có giá trị mặc định để thiếu tham số vẫn bind được.</summary>
public sealed class AuditLogQuery
{
    public string? TargetType { get; init; }
    public string? TargetId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
