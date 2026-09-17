using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Cms.Application.Common.Audit;
using AntFarm.Cms.Application.Site;
using AntFarm.Cms.Application.Site.Dtos;
using AntFarm.Cms.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Cms.Api.Features.Admin;

/// <summary>
/// Cấu hình site/SEO (§6.2 W3a) — mọi endpoint yêu cầu quyền <c>site.manage</c>
/// ([RequirePermission] không khai <c>new string Policy</c> — CLAUDE.md).
/// </summary>
[ApiController]
[Route("api/admin/site-settings")]
[RequirePermission(PermissionCodes.SiteManage)]
public sealed class SiteSettingsController(SiteSettingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SiteSettingsDto>> Get(CancellationToken ct) => Ok(await service.GetAsync(ct));

    [HttpPut]
    public async Task<ActionResult<SiteSettingsDto>> Update([FromBody] UpdateSiteSettingsRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(CurrentActor(), request.Values ?? new Dictionary<string, string>(), ct));

    private Actor CurrentActor() => new(User.GetAccountId()!.Value, User.GetEmail()!);
}
