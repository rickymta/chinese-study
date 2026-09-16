using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Identity.Api.Features.System;

/// <summary>GET /api/system/info — kiểm tra service sống + biết đang chạy phiên bản/môi trường nào (§6.1).</summary>
[ApiController]
[Route("api/system")]
[AllowAnonymous]
public sealed class SystemController(IHostEnvironment environment, TimeProvider timeProvider) : ControllerBase
{
    private const string Version = "0.1.0";

    [HttpGet("info")]
    public ActionResult<SystemInfoResponse> GetInfo() => Ok(new SystemInfoResponse(
        Service: "identity-service",
        Version: Version,
        Environment: environment.EnvironmentName,
        ServerTimeUtc: timeProvider.GetUtcNow().UtcDateTime));
}

public sealed record SystemInfoResponse(string Service, string Version, string Environment, DateTime ServerTimeUtc);
