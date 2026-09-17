using System.Text;
using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Writing;
using AntFarm.Chinese.Application.Writing.Dtos;
using AntFarm.Chinese.Domain.Access;
using AntFarm.Core.Errors;
using AntFarm.Security.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Writing;

/// <summary>Luyện viết chữ Hán (§6.2) — mọi endpoint yêu cầu quyền <c>study.use</c> (R-W*, [RequirePermission] không khai <c>new string Policy</c> — CLAUDE.md). Server CHỈ NHẬN kết quả, không phục vụ dữ liệu nét (R-W7 — dữ liệu nét đóng gói sẵn ở frontend, F8.1).</summary>
[ApiController]
[Route("api/writing")]
[RequirePermission(PermissionCodes.StudyUse)]
public sealed class WritingController(WritingService writingService, WritingCharacterQueryService queryService) : ControllerBase
{
    [HttpPost("attempts")]
    public async Task<ActionResult<RecordWritingAttemptResponseDto>> RecordAttempt([FromBody] RecordWritingAttemptRequest request, CancellationToken ct)
    {
        var (response, isReplay) = await writingService.RecordAttemptAsync(User.GetAccountId()!.Value, request, ct);
        return isReplay ? Ok(response) : StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("characters")]
    public async Task<ActionResult<WritingCharacterListResponseDto>> ListCharacters([FromQuery] WritingCharactersQuery query, CancellationToken ct) =>
        Ok(await queryService.ListAsync(User.GetAccountId()!.Value, query.Set, query.Page, query.PageSize, ct));

    [HttpGet("characters/{hanzi}")]
    public async Task<ActionResult<WritingCharacterDetailDto>> GetCharacter(string hanzi, CancellationToken ct)
    {
        // hanzi tới từ ROUTE (không qua FluentValidation) — kiểm tay, cùng hình dạng lỗi với
        // AddAfInvalidModelStateResponse (§6.0), cùng quy ước DictionaryController.GetCharacter (F6).
        var normalized = (hanzi ?? "").Normalize(NormalizationForm.FormC);
        if (!CjkCharacterValidation.IsSingleCjkCharacter(normalized))
        {
            var details = new Dictionary<string, string[]> { ["hanzi"] = ["Cần đúng một chữ Hán."] };
            return BadRequest(new ErrorResponseMapper.ErrorBody("Dữ liệu gửi lên không hợp lệ.", "VALIDATION", details));
        }

        var detail = await queryService.GetAsync(User.GetAccountId()!.Value, normalized, ct);
        if (detail is null)
            throw new NotFoundException($"Không tìm thấy chữ '{normalized}'.");

        return Ok(detail);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<WritingSummaryDto>> GetSummary(CancellationToken ct) =>
        Ok(await writingService.GetSummaryAsync(User.GetAccountId()!.Value, ct));
}
