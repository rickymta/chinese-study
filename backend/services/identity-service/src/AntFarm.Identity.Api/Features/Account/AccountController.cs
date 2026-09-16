using AntFarm.Auth;
using AntFarm.Identity.Application.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Identity.Api.Features.Account;

/// <summary>Hồ sơ tài khoản (§6.2). Đổi mật khẩu KHÔNG ở đây — xem <c>AuthController.ChangePassword</c> (D21).</summary>
[ApiController]
[Route("api/account")]
[Authorize]
public sealed class AccountController(AccountService accountService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AccountDto>> Get(CancellationToken ct)
        => Ok(await accountService.GetAsync(User.GetAccountId()!.Value, ct));

    [HttpPut]
    public async Task<ActionResult<AccountDto>> Update([FromBody] UpdateProfileRequest request, CancellationToken ct)
        => Ok(await accountService.UpdateProfileAsync(User.GetAccountId()!.Value, request.DisplayName, request.TimeZone, ct));
}
