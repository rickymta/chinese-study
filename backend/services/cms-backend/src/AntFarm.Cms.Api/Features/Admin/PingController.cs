using AntFarm.Auth.Authorization;
using AntFarm.Cms.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Cms.Api.Features.Admin;

/// <summary>
/// GET /api/admin/ping (§6.1, §7 W1) — endpoint "canh gác" tối thiểu để nghiệm thu phân quyền
/// cục bộ (tài khoản 0 quyền ⇒ 403 JSON). Dùng để xác nhận quyền có hiệu lực NGAY sau khi đổi vai
/// trò (không chờ cache 60 giây, PermissionResolver.Invalidate).
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
