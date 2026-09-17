using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Admin.Content;
using AntFarm.Chinese.Application.Admin.Content.Dtos;
using AntFarm.Chinese.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Admin;

/// <summary>
/// Quản trị bài học + quiz (§6.3, R-CA1) — mọi endpoint yêu cầu quyền <c>content.manage</c>
/// ([RequirePermission] không khai <c>new string Policy</c> — CLAUDE.md).
/// </summary>
[ApiController]
[Route("api/admin/lessons")]
[RequirePermission(PermissionCodes.ContentManage)]
public sealed class AdminLessonsController(LessonAdminService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminLessonListResponseDto>> List([FromQuery] AdminLessonsQuery query, CancellationToken ct) =>
        Ok(await service.ListAsync(query, ct));

    [HttpPost]
    public async Task<ActionResult<AdminLessonDto>> Create([FromBody] CreateLessonRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request, User.GetAccountId()!.Value, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminLessonDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await service.GetAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminLessonDto>> UpdateMeta(Guid id, [FromBody] UpdateLessonMetaRequest request, CancellationToken ct) =>
        Ok(await service.UpdateMetaAsync(id, request, User.GetAccountId()!.Value, ct));

    [HttpPut("{id:guid}/blocks")]
    public async Task<ActionResult<AdminLessonDto>> ReplaceBlocks(Guid id, [FromBody] ReplaceLessonBlocksRequest request, CancellationToken ct) =>
        Ok(await service.ReplaceBlocksAsync(id, request, User.GetAccountId()!.Value, ct));

    [HttpPut("{id:guid}/words")]
    public async Task<ActionResult<AdminLessonDto>> ReplaceWords(Guid id, [FromBody] ReplaceLessonWordsRequest request, CancellationToken ct) =>
        Ok(await service.ReplaceWordsAsync(id, request, User.GetAccountId()!.Value, ct));

    [HttpPut("{id:guid}/quiz")]
    public async Task<ActionResult<AdminLessonDto>> ReplaceQuiz(Guid id, [FromBody] ReplaceLessonQuizRequest request, CancellationToken ct) =>
        Ok(await service.ReplaceQuizAsync(id, request, User.GetAccountId()!.Value, ct));

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<AdminLessonDto>> Publish(Guid id, [FromBody] LessonVersionRequest request, CancellationToken ct) =>
        Ok(await service.PublishAsync(id, request, User.GetAccountId()!.Value, ct));

    [HttpPost("{id:guid}/unpublish")]
    public async Task<ActionResult<AdminLessonDto>> Unpublish(Guid id, [FromBody] LessonVersionRequest request, CancellationToken ct) =>
        Ok(await service.UnpublishAsync(id, request, User.GetAccountId()!.Value, ct));

    [HttpPost("{id:guid}/review")]
    public async Task<ActionResult<AdminLessonDto>> Review(Guid id, [FromBody] LessonVersionRequest request, CancellationToken ct) =>
        Ok(await service.ReviewAsync(id, request, User.GetAccountId()!.Value, ct));

    [HttpPost("{id:guid}/restore")]
    public async Task<ActionResult<AdminLessonDto>> Restore(Guid id, [FromBody] LessonVersionRequest request, CancellationToken ct) =>
        Ok(await service.RestoreAsync(id, request, User.GetAccountId()!.Value, ct));

    /// <summary>R-CA7: 204 khi xoá cứng (bài admin chưa có lần làm); 200 <c>{ result: "archived", lesson }</c> khi chuyển lưu trữ.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] uint version, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, version, User.GetAccountId()!.Value, ct);
        return result.HardDeleted ? NoContent() : Ok(new { result = "archived", lesson = result.Lesson });
    }
}
