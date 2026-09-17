using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Admin;

/// <summary>
/// GET /api/admin/ping (§6.3, §7 F3) — endpoint "canh gác" tối thiểu để nghiệm thu phân quyền
/// cục bộ (learner ⇒ 403 JSON). Từ F4, <see cref="UsersController"/>/<see cref="RolesController"/>
/// đã có đầy đủ chức năng quản trị thật — endpoint này VẪN GIỮ vì frontend/kiểm thử F4 dùng nó
/// để xác nhận quyền có hiệu lực NGAY sau khi đổi vai trò (không chờ cache 60 giây, R4-9).
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
