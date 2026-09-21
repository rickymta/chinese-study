using AntFarm.Auth.Authorization;
using AntFarm.Cms.Application.Access;
using AntFarm.Cms.Application.Access.Dtos;
using AntFarm.Cms.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Cms.Api.Features.Admin;

/// <summary>Danh mục vai trò + quyền của mỗi vai trò (§6.1) — dùng để dựng UI dialog gán vai trò.</summary>
[ApiController]
[Route("api/admin/roles")]
[RequirePermission(PermissionCodes.UsersManage)]
public sealed class RolesController(UserAdminService userAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> List(CancellationToken ct)
        => Ok(await userAdminService.GetRolesCatalogAsync(ct));
}
