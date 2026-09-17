using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Lessons;
using AntFarm.Chinese.Application.Lessons.Dtos;
using AntFarm.Chinese.Domain.Access;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Lessons;

/// <summary>Bài học + quiz cho học viên (§6.1) — mọi endpoint yêu cầu quyền <c>study.use</c> (R-LS*, [RequirePermission] không khai <c>new string Policy</c> — CLAUDE.md).</summary>
[ApiController]
[Route("api/lessons")]
[RequirePermission(PermissionCodes.StudyUse)]
public sealed class LessonsController(
    LessonQueryService queryService,
    LessonProgressService progressService,
    QuizSubmissionService quizSubmissionService) : ControllerBase
{
    [HttpGet("")]
    public async Task<ActionResult<LessonListResponseDto>> List(CancellationToken ct) =>
        Ok(await queryService.ListPublishedAsync(User.GetAccountId()!.Value, ct));

    [HttpGet("{slug}")]
    public async Task<ActionResult<LessonDetailDto>> GetBySlug(string slug, CancellationToken ct) =>
        Ok(await queryService.GetPublishedBySlugAsync(User.GetAccountId()!.Value, slug, ct));

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<LessonProgressDto>> Start(Guid id, CancellationToken ct) =>
        Ok(await progressService.StartAsync(User.GetAccountId()!.Value, id, ct));

    [HttpPost("{id:guid}/quiz-attempts")]
    public async Task<ActionResult<SubmitQuizResponseDto>> SubmitQuiz(Guid id, [FromBody] SubmitQuizRequest request, CancellationToken ct)
    {
        var (response, isReplay) = await quizSubmissionService.SubmitAsync(User.GetAccountId()!.Value, id, request, ct);
        return isReplay ? Ok(response) : StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{id:guid}/quiz-attempts")]
    public async Task<ActionResult<QuizAttemptListResponseDto>> ListAttempts(Guid id, [FromQuery] QuizAttemptsQuery query, CancellationToken ct) =>
        Ok(await queryService.ListAttemptsAsync(User.GetAccountId()!.Value, id, query.Limit, ct));
}
