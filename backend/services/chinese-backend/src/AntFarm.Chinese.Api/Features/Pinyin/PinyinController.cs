using AntFarm.Auth;
using AntFarm.Auth.Authorization;
using AntFarm.Chinese.Application.Pinyin;
using AntFarm.Chinese.Application.Pinyin.Dtos;
using AntFarm.Chinese.Domain.Access;
using AntFarm.Core.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace AntFarm.Chinese.Api.Features.Pinyin;

/// <summary>
/// GET chart/guide, POST tone-drills, GET tone-stats (§6.1) — mọi endpoint yêu cầu quyền
/// <c>study.use</c> (R-P1, [RequirePermission] không khai <c>new string Policy</c> — CLAUDE.md).
/// </summary>
[ApiController]
[Route("api/pinyin")]
[RequirePermission(PermissionCodes.StudyUse)]
public sealed class PinyinController(
    IPinyinCatalog catalog,
    ToneDrillService toneDrillService,
    ToneStatsService toneStatsService) : ControllerBase
{
    [HttpGet("chart")]
    public ActionResult<PinyinChartDto> GetChart()
    {
        EnsureCatalogAvailable();
        return WithCaching(() => catalog.Chart);
    }

    [HttpGet("guide")]
    public ActionResult<PinyinGuideDto> GetGuide()
    {
        EnsureCatalogAvailable();
        return WithCaching(() => catalog.Guide);
    }

    [HttpPost("tone-drills")]
    public async Task<IActionResult> SubmitToneDrill([FromBody] SubmitToneDrillRequest request, CancellationToken ct)
    {
        var userId = User.GetAccountId()!.Value;
        var (body, created) = await toneDrillService.SubmitAsync(userId, request, ct);
        return created ? StatusCode(StatusCodes.Status201Created, body) : Ok(body);
    }

    [HttpGet("tone-stats")]
    public async Task<ActionResult<ToneStatsResponse>> GetToneStats(CancellationToken ct)
    {
        var userId = User.GetAccountId()!.Value;
        return Ok(await toneStatsService.GetAsync(userId, ct));
    }

    private void EnsureCatalogAvailable()
    {
        if (!catalog.IsAvailable)
            throw new ServiceUnavailableException("CONTENT_UNAVAILABLE", "Học liệu pinyin chưa sẵn sàng — báo quản trị viên.");
    }

    /// <summary>ETag = phiên bản học liệu (SHA-256 16 hex) — nạp lại học liệu (khởi động lại service) đổi ETag, trình duyệt tự tải lại (§6.1).</summary>
    private ActionResult<T> WithCaching<T>(Func<T> getBody)
    {
        var etag = new EntityTagHeaderValue($"\"{catalog.Version}\"");
        Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue { Private = true, MaxAge = TimeSpan.FromHours(1) };
        Response.GetTypedHeaders().ETag = etag;

        if (Request.GetTypedHeaders().IfNoneMatch.Any(v => v.Equals(etag)))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(getBody());
    }
}
