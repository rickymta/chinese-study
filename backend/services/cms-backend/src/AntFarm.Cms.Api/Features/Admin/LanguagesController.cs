using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Cms.Application.Common.Audit;
using AntFarm.Cms.Application.Site;
using AntFarm.Cms.Application.Site.Dtos;
using AntFarm.Cms.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Cms.Api.Features.Admin;

/// <summary>
/// Danh mục ngôn ngữ hiển thị trên website/portal (§6.2 W3a) — mọi endpoint yêu cầu quyền
/// <c>site.manage</c>. Route <c>order</c> khai TRƯỚC <c>{id:guid}</c> (không xung đột thật sự vì
/// ràng buộc <c>:guid</c>, nhưng khai theo đúng thứ tự hợp đồng cho dễ đọc).
/// </summary>
[ApiController]
[Route("api/admin/languages")]
[RequirePermission(PermissionCodes.SiteManage)]
public sealed class LanguagesController(LanguageAdminService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LanguageDto>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<LanguageDto>> Create([FromBody] CreateLanguageRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(CurrentActor(), request, ct));

    [HttpPut("order")]
    public async Task<IActionResult> Reorder([FromBody] ReorderLanguagesRequest request, CancellationToken ct)
    {
        await service.ReorderAsync(CurrentActor(), request.Ids ?? [], ct);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LanguageDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await service.GetAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LanguageDto>> Update(Guid id, [FromBody] UpdateLanguageRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(CurrentActor(), id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(CurrentActor(), id, ct);
        return NoContent();
    }

    private Actor CurrentActor() => new(User.GetAccountId()!.Value, User.GetEmail()!);
}
