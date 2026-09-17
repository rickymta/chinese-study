using AntFarm.Identity.Api.Internal;
using AntFarm.Identity.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Identity.Api.Features.Internal;

/// <summary>§6.5, W10, D-W4 — bật/tắt đăng ký runtime.</summary>
[ApiController]
[Route("internal/settings")]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class InternalSettingsController(PlatformSettingsService service) : ControllerBase
{
    [HttpGet("registration")]
    public async Task<ActionResult<RegistrationStateDto>> Get(CancellationToken ct)
        => Ok(await service.GetRegistrationAsync(ct));

    [HttpPut("registration")]
    public async Task<ActionResult<RegistrationStateDto>> Put([FromBody] UpdateRegistrationRequest request, CancellationToken ct)
    {
        var actor = InternalActor.FromHeaders(Request);
        return Ok(await service.SetRegistrationAsync(actor.Id, request.Enabled, ct));
    }
}
