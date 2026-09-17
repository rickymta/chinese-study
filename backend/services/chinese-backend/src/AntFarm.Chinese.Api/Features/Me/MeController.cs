using AntFarm.Auth;
using AntFarm.Chinese.Application.Access;
using AntFarm.Chinese.Application.Access.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Me;

/// <summary>GET /api/me (§6.3) — chỉ [Authorize] (không cần quyền cụ thể): mọi người dùng đã đăng
/// nhập đều xem được hồ sơ + vai trò/quyền CỦA CHÍNH MÌNH, kể cả khi 0 quyền (R-P8: frontend dựa
/// vào "permissions" rỗng để đưa thẳng tới /403).</summary>
[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController(MeService meService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MeDto>> Get(CancellationToken ct)
        => Ok(await meService.GetAsync(User.GetAccountId()!.Value, ct));
}
