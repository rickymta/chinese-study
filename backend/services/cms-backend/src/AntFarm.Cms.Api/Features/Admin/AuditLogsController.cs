using AntFarm.Auth.Authorization;
using AntFarm.Cms.Application.Site;
using AntFarm.Cms.Application.Site.Dtos;
using AntFarm.Cms.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Cms.Api.Features.Admin;

/// <summary>Nhật ký thao tác ghi của cms-backend (§6.2 W3b) — quyền <c>users.manage</c>.</summary>
[ApiController]
[Route("api/admin/audit-logs")]
[RequirePermission(PermissionCodes.UsersManage)]
public sealed class AuditLogsController(AuditLogQueryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AuditLogsPageDto>> List([FromQuery] AuditLogQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));
}
