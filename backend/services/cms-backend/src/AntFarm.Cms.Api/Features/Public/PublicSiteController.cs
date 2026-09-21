using AntFarm.Cms.Application.Site;
using AntFarm.Cms.Application.Site.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Cms.Api.Features.Public;

/// <summary>
/// Dữ liệu nền website công khai (§6.2 W3a) — <c>[AllowAnonymous]</c> TƯỜNG MINH vì FallbackPolicy
/// của service là <c>RequireAuthenticatedUser</c> (Program.cs). Cache 60 giây — website đọc qua
/// ISR nên không cần realtime tuyệt đối, giảm tải gateway/cms-backend.
/// </summary>
[ApiController]
[Route("api/public/site")]
[AllowAnonymous]
public sealed class PublicSiteController(PublicSiteService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PublicSiteDto>> Get(CancellationToken ct)
    {
        Response.Headers.CacheControl = "public, max-age=60";
        return Ok(await service.GetAsync(ct));
    }
}
