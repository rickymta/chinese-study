using AntFarm.Cms.Application.Common.Abstractions;
using AntFarm.Cms.Application.Site.Dtos;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Cms.Application.Site;

/// <summary>
/// Truy vấn nhật ký thao tác ghi của cms-backend (§5.2.3, §6.2 W3b) — <c>GET /api/admin/audit-logs</c>,
/// quyền <c>users.manage</c> (nhật ký là dữ liệu nhạy, chỉ vai trò quản trị người dùng mới xem —
/// khác <c>site.manage</c> dùng cho FAQ/ngôn ngữ/cấu hình).
/// </summary>
public sealed class AuditLogQueryService(ICmsDbContext db)
{
    private const int MaxPageSize = 100;
    private const int MaxTargetTypeLength = 32;
    private const int MaxTargetIdLength = 64;

    public async Task<AuditLogsPageDto> ListAsync(AuditLogQuery query, CancellationToken ct)
    {
        var targetType = query.TargetType?.Trim();
        var targetId = query.TargetId?.Trim();

        if (query.PageSize is < 1 or > MaxPageSize)
            throw new ValidationAppException("pageSize không hợp lệ.", new { pageSize = new[] { $"pageSize phải từ 1 đến {MaxPageSize}." } });
        if (query.Page is < 1 or > 100_000) // trần: (Page-1)*PageSize không được tràn int ⇒ Skip âm ⇒ 500
            throw new ValidationAppException("page không hợp lệ.", new { page = new[] { "page phải từ 1 đến 100000." } });
        if (targetType is { Length: > MaxTargetTypeLength })
            throw new ValidationAppException("targetType không hợp lệ.", new { targetType = new[] { $"targetType tối đa {MaxTargetTypeLength} ký tự." } });
        if (targetId is { Length: > MaxTargetIdLength })
            throw new ValidationAppException("targetId không hợp lệ.", new { targetId = new[] { $"targetId tối đa {MaxTargetIdLength} ký tự." } });

        var logsQuery = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(targetType))
            logsQuery = logsQuery.Where(a => a.TargetType == targetType);
        if (!string.IsNullOrEmpty(targetId))
            logsQuery = logsQuery.Where(a => a.TargetId == targetId);

        var totalCount = await logsQuery.CountAsync(ct);
        var items = await logsQuery
            .OrderByDescending(a => a.At).ThenByDescending(a => a.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AuditLogDto(a.Id, a.At, a.ActorId, a.ActorEmail, a.Action, a.TargetType, a.TargetId, a.Summary, a.Success))
            .ToListAsync(ct);

        return new AuditLogsPageDto(items, query.Page, query.PageSize, totalCount);
    }
}
