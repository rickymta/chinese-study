using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Admin;

/// <summary>
/// GET /api/admin/ping (§6.3, §7 F3) — endpoint "canh gác" tối thiểu để nghiệm thu phân quyền
/// cục bộ (learner ⇒ 403 JSON) trước khi có UsersController/RolesController thật (F4, §5.2.4:
/// backend quản trị người dùng/vai trò hoàn chỉnh — HĐG §6.3 phần còn lại thuộc F4, xem README bàn giao F3).
/// </summary>
[ApiController]
[Route("api/admin/ping")]
public sealed class PingController : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.UsersManage)]
    public ActionResult<PingResponse> Get() => Ok(new PingResponse(true));
}

public sealed record PingResponse(bool Ok);
