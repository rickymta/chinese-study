using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Identity.Api.Features.Internal;

/// <summary>Canh gác nhanh cho tay/VERIFY-DOCKER (§8.1) — không nghiệp vụ. Chỉ tới được qua cổng nội bộ + khoá dịch vụ đúng (InternalAccessMiddleware).</summary>
[ApiController]
[Route("internal/ping")]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class InternalPingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { ok = true });
}
