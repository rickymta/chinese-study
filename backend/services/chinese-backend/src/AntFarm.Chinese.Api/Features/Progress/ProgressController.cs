using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Progress;
using AntFarm.Chinese.Application.Progress.Dtos;
using AntFarm.Chinese.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Progress;

/// <summary>Tổng quan tiến độ — trang chủ (§6.4, F11). Yêu cầu quyền <c>study.use</c> (R-PG*, [RequirePermission] không khai <c>new string Policy</c> — CLAUDE.md).</summary>
[ApiController]
[Route("api/progress")]
[RequirePermission(PermissionCodes.StudyUse)]
public sealed class ProgressController(ProgressOverviewService overviewService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<ProgressOverviewDto>> GetOverview(CancellationToken ct) =>
        Ok(await overviewService.GetAsync(User.GetAccountId()!.Value, ct));
}
