namespace AntFarm.Cms.Application.Common.Audit;

/// <summary>Người thực hiện thao tác — dựng từ claim JWT (<c>User.GetAccountId()</c>/<c>GetEmail()</c>, <c>AntFarm.Auth</c>), KHÔNG suy quyền từ đây.</summary>
public sealed record Actor(Guid Id, string Email);

/// <summary>
/// Ghi một dòng <c>site.audit_logs</c> (§5.2.3 W3a) — CHỈ <c>db.AuditLogs.Add</c>, KHÔNG tự
/// <c>SaveChangesAsync</c> (ghi CÙNG giao dịch với thao tác nghiệp vụ gọi nó, để rollback đồng bộ
/// nếu thao tác chính thất bại).
/// </summary>
public interface IAuditLogger
{
    void Add(Actor actor, string action, string targetType, string? targetId, string summary, bool success = true);
}
