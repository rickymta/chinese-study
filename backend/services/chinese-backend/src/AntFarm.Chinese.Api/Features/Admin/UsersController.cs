using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Access;
using AntFarm.Chinese.Application.Access.Dtos;
using AntFarm.Chinese.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Admin;

/// <summary>
/// Quản trị người dùng/vai trò cục bộ của chinese-backend (§6.3, R4-6..R4-9) — mọi endpoint yêu
/// cầu quyền <c>users.manage</c> ([RequirePermission] không khai <c>new string Policy</c> — CLAUDE.md).
/// </summary>
[ApiController]
[Route("api/admin/users")]
[RequirePermission(PermissionCodes.UsersManage)]
public sealed class UsersController(UserAdminService userAdminService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminUsersPageDto>> List([FromQuery] AdminUsersQuery query, CancellationToken ct)
        => Ok(await userAdminService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await userAdminService.GetAsync(id, ct));

    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult<AdminUserDto>> SetRoles(Guid id, [FromBody] SetUserRolesRequest request, CancellationToken ct)
    {
        var actorId = User.GetAccountId()!.Value;
        return Ok(await userAdminService.SetRolesAsync(actorId, id, request.Roles, ct));
    }
}
