using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Learning;
using AntFarm.Chinese.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Me;

/// <summary>GET/PUT /api/me/learning-settings (§6.2) — yêu cầu quyền <c>study.use</c> ([RequirePermission] không khai <c>new string Policy</c> — CLAUDE.md).</summary>
[ApiController]
[Route("api/me/learning-settings")]
[RequirePermission(PermissionCodes.StudyUse)]
public sealed class LearningSettingsController(LearnerSettingsService settingsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<LearnerSettingsDto>> Get(CancellationToken ct) =>
        Ok(await settingsService.GetAsync(User.GetAccountId()!.Value, ct));

    [HttpPut]
    public async Task<ActionResult<LearnerSettingsDto>> Update([FromBody] UpdateLearnerSettingsCommand command, CancellationToken ct) =>
        Ok(await settingsService.UpdateAsync(User.GetAccountId()!.Value, command, ct));
}
