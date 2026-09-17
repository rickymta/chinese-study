using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Srs;
using AntFarm.Chinese.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Srs;

/// <summary>Tóm tắt/hàng đợi/chấm thẻ/thêm-tạm dừng thẻ SRS (§6.2) — mọi endpoint yêu cầu quyền <c>study.use</c> (R7-*, [RequirePermission] không khai <c>new string Policy</c> — CLAUDE.md).</summary>
[ApiController]
[Route("api/srs")]
[RequirePermission(PermissionCodes.StudyUse)]
public sealed class SrsController(
    SrsSummaryService summaryService,
    SrsQueueService queueService,
    SrsReviewService reviewService,
    ISrsCardService cardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<SrsSummaryDto>> GetSummary(CancellationToken ct) =>
        Ok(await summaryService.GetAsync(User.GetAccountId()!.Value, ct));

    [HttpGet("queue")]
    public async Task<ActionResult<SrsQueueDto>> GetQueue([FromQuery] SrsQueueQuery query, CancellationToken ct) =>
        Ok(await queueService.GetQueueAsync(User.GetAccountId()!.Value, query.Limit, ct));

    [HttpPost("cards/{cardId:guid}/reviews")]
    public async Task<ActionResult<ReviewCardResultDto>> Review(Guid cardId, [FromBody] ReviewCardCommand command, CancellationToken ct) =>
        Ok(await reviewService.ReviewAsync(User.GetAccountId()!.Value, cardId, command, ct));

    [HttpPost("cards")]
    public async Task<ActionResult<AddCardsResultDto>> AddCards([FromBody] AddCardsCommand command, CancellationToken ct)
    {
        var result = await cardService.AddAsync(User.GetAccountId()!.Value, command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("cards/{cardId:guid}/suspension")]
    public async Task<ActionResult<SrsCardDto>> SetSuspension(Guid cardId, [FromBody] SetCardSuspensionCommand command, CancellationToken ct) =>
        Ok(await cardService.SetSuspendedAsync(User.GetAccountId()!.Value, cardId, command.Suspended, ct));
}
