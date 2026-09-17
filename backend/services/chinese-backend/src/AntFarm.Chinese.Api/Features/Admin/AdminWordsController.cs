using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Admin.Content;
using AntFarm.Chinese.Application.Admin.Content.Dtos;
using AntFarm.Chinese.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Admin;

/// <summary>
/// Duyệt nghĩa Việt/Hán Việt của từ vựng (§6.3, R-CA9/R-CA11) — mọi endpoint yêu cầu quyền
/// <c>content.manage</c> ([RequirePermission] không khai <c>new string Policy</c> — CLAUDE.md).
/// </summary>
[ApiController]
[Route("api/admin/words")]
[RequirePermission(PermissionCodes.ContentManage)]
public sealed class AdminWordsController(WordReviewService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminWordsPageDto>> List([FromQuery] AdminWordsQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminWordDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await service.GetAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminWordDto>> Update(Guid id, [FromBody] UpdateWordRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, User.GetAccountId()!.Value, ct));

    [HttpPost("review")]
    public async Task<ActionResult<BulkReviewResultDto>> BulkReview([FromBody] BulkReviewWordsRequest request, CancellationToken ct) =>
        Ok(await service.BulkReviewAsync(request, User.GetAccountId()!.Value, ct));
}
