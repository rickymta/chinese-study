using AntFarm.Identity.Api.Internal;
using AntFarm.Identity.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Identity.Api.Features.Internal;

/// <summary>
/// API nội bộ quản trị tài khoản (§6.5, W10) — chỉ tới được qua cổng riêng + đúng
/// <c>X-Service-Key</c> (<see cref="InternalAccessMiddleware"/>), KHÔNG qua JWT/[Authorize]:
/// identity-service vẫn KHÔNG có vai trò/quyền của riêng nó (R-W3). Người gọi DUY NHẤT dự kiến
/// là cms-backend (W11, quyền <c>accounts.manage</c> của CHÍNH cms).
/// </summary>
[ApiController]
[Route("internal/accounts")]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class InternalAccountsController(AccountAdminService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminAccountListResult>> List([FromQuery] AdminAccountsQuery query, CancellationToken ct)
        => Ok(await service.ListAsync(query.Q, query.Status, query.Page, query.PageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminAccountDto>> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    [HttpPost("{id:guid}/disable")]
    public async Task<ActionResult<AdminAccountDto>> Disable(Guid id, CancellationToken ct)
    {
        var actor = InternalActor.FromHeaders(Request);
        return Ok(await service.DisableAsync(actor.Id, id, ct));
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<ActionResult<AdminAccountDto>> Enable(Guid id, CancellationToken ct)
    {
        var actor = InternalActor.FromHeaders(Request);
        return Ok(await service.EnableAsync(actor.Id, id, ct));
    }

    [HttpPost("{id:guid}/clear-lockout")]
    public async Task<ActionResult<AdminAccountDto>> ClearLockout(Guid id, CancellationToken ct)
    {
        var actor = InternalActor.FromHeaders(Request);
        return Ok(await service.ClearLockoutAsync(actor.Id, id, ct));
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<ResetPasswordResult>> ResetPassword(Guid id, [FromBody] ResetPasswordRequest? request, CancellationToken ct)
    {
        var actor = InternalActor.FromHeaders(Request);
        var result = await service.ResetPasswordAsync(actor.Id, id, request?.NewPassword, ct);

        // RW10: mật khẩu tạm không được cache ở BẤT KỲ tầng trung gian nào (proxy/CDN/trình duyệt).
        Response.Headers.CacheControl = "no-store";
        return Ok(result);
    }

    [HttpPost("{id:guid}/revoke-sessions")]
    public async Task<ActionResult<RevokeSessionsResult>> RevokeSessions(Guid id, CancellationToken ct)
    {
        var actor = InternalActor.FromHeaders(Request);
        return Ok(await service.RevokeSessionsAsync(actor.Id, id, ct));
    }
}
