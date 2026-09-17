using AntFarm.Identity.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Identity.Api.Features.Internal;

/// <summary>§6.5, W10 — thống kê đăng ký cho màn quản trị tài khoản (W11).</summary>
[ApiController]
[Route("internal/stats")]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class InternalStatsController(RegistrationStatsService service) : ControllerBase
{
    [HttpGet("registrations")]
    public async Task<ActionResult<RegistrationStatsDto>> Registrations([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(await service.GetAsync(from, to, ct));
}
