using System.Text;
using System.Text.RegularExpressions;
using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Dictionary;
using AntFarm.Chinese.Domain.Access;
using AntFarm.Core.Errors;
using AntFarm.Security.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Chinese.Api.Features.Dictionary;

/// <summary>Tra từ (§6.1) — mọi endpoint yêu cầu quyền <c>study.use</c> (R6-20, [RequirePermission] không khai <c>new string Policy</c> — CLAUDE.md).</summary>
[ApiController]
[Route("api/dictionary")]
[RequirePermission(PermissionCodes.StudyUse)]
public sealed partial class DictionaryController(DictionaryService dictionaryService) : ControllerBase
{
    // CJK Unified Ideographs + Extension A + 〇 — cùng bộ ký tự Hán dùng ở DictionaryQueryParser.HanziCharPattern.
    [GeneratedRegex(@"^[\p{IsCJKUnifiedIdeographs}\p{IsCJKUnifiedIdeographsExtensionA}〇]$")]
    private static partial Regex SingleHanziPattern();

    [HttpGet("search")]
    public async Task<ActionResult<DictionarySearchResultDto>> Search([FromQuery] DictionaryQuery query, CancellationToken ct)
        => Ok(await dictionaryService.SearchAsync(query, ct));

    [HttpGet("words/{id:guid}")]
    public async Task<ActionResult<WordDetailDto>> GetWord(Guid id, CancellationToken ct)
    {
        // F7: gắn khối srs của NGƯỜI ĐANG GỌI (§6.2 GET /api/dictionary/words/{id}).
        var userId = User.GetAccountId();
        var detail = await dictionaryService.GetWordAsync(id, userId, ct);
        if (detail is null)
            throw new NotFoundException($"Không tìm thấy từ '{id}'.");

        return Ok(detail);
    }

    [HttpGet("characters/{hanzi}")]
    public async Task<ActionResult<CharacterDetailDto>> GetCharacter(string hanzi, CancellationToken ct)
    {
        // hanzi tới từ ROUTE (không qua FluentValidation) — kiểm tay, cùng hình dạng lỗi với
        // AddAfInvalidModelStateResponse (§6.0) để frontend xử lý nhất quán.
        var normalized = (hanzi ?? "").Normalize(NormalizationForm.FormC);
        if (normalized.EnumerateRunes().Count() != 1 || !SingleHanziPattern().IsMatch(normalized))
        {
            var details = new Dictionary<string, string[]> { ["hanzi"] = ["Cần đúng một chữ Hán."] };
            return BadRequest(new ErrorResponseMapper.ErrorBody("Dữ liệu gửi lên không hợp lệ.", "VALIDATION", details));
        }

        var detail = await dictionaryService.GetCharacterAsync(normalized, ct);
        if (detail is null)
            throw new NotFoundException($"Không tìm thấy chữ '{normalized}'.");

        return Ok(detail);
    }
}
