using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Cms.Application.Common.Audit;
using AntFarm.Cms.Application.Site;
using AntFarm.Cms.Application.Site.Dtos;
using AntFarm.Cms.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Cms.Api.Features.Admin;

/// <summary>
/// Câu hỏi thường gặp hiển thị trên website/portal (§6.2 W3b) — mọi endpoint yêu cầu quyền
/// <c>site.manage</c>. Route <c>order</c> khai TRƯỚC <c>{id:guid}</c> (khuôn <c>LanguagesController</c> W3a).
/// </summary>
[ApiController]
[Route("api/admin/faqs")]
[RequirePermission(PermissionCodes.SiteManage)]
public sealed class FaqsController(FaqAdminService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FaqDto>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<FaqDto>> Create([FromBody] CreateFaqRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(CurrentActor(), request, ct));

    [HttpPut("order")]
    public async Task<IActionResult> Reorder([FromBody] ReorderFaqsRequest request, CancellationToken ct)
    {
        await service.ReorderAsync(CurrentActor(), request.GroupKey, request.Ids ?? [], ct);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FaqDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await service.GetAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FaqDto>> Update(Guid id, [FromBody] UpdateFaqRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(CurrentActor(), id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(CurrentActor(), id, ct);
        return NoContent();
    }

    private Actor CurrentActor() => new(User.GetAccountId()!.Value, User.GetEmail()!);
}
