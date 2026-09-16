using System.Text.Json.Serialization;
using AntFarm.Identity.Application.Common.Abstractions;
using AntFarm.Identity.Application.Common.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntFarm.Identity.Api.Features.WellKnown;

/// <summary>JWKS công khai (R-A3, §6.2) — service ngôn ngữ tải qua mạng nội bộ để kiểm chữ ký token.</summary>
[ApiController]
[AllowAnonymous]
public sealed class WellKnownController(ISigningKeyStore signingKeyStore, JwtOptions jwtOptions) : ControllerBase
{
    [HttpGet("/.well-known/jwks.json")]
    public IActionResult GetJwks()
    {
        // 300s — đủ ngắn để nhận khoá mới sau khi xoay, đủ dài để không đấm liên tục vào endpoint này.
        Response.Headers.CacheControl = "public, max-age=300";

        var keys = signingKeyStore.GetPublicJsonWebKeys()
            .Select(k => new JwkDto(k.Kty, k.Use, k.Alg, k.Kid, k.N, k.E));

        return Ok(new { keys });
    }

    [HttpGet("/.well-known/openid-configuration")]
    public IActionResult GetOpenIdConfiguration()
        => Ok(new OpenIdConfigurationResponse(jwtOptions.Issuer, $"{jwtOptions.Issuer}/.well-known/jwks.json"));

    private sealed record JwkDto(string Kty, string Use, string Alg, string Kid, string N, string E);

    private sealed record OpenIdConfigurationResponse(
        string Issuer,
        [property: JsonPropertyName("jwks_uri")] string JwksUri);
}
