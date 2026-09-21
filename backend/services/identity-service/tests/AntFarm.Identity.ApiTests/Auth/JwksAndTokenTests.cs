using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Auth;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>R-A3/R-A4/§6.2 — JWKS công khai + access token có claim đúng khuôn, được CHÍNH identity-service (audience "af-identity", khoá cục bộ) chấp nhận ở endpoint Bearer thật.</summary>
[Collection(IdentityApiCollection.Name)]
public class JwksAndTokenTests(IdentityDbApiFactory factory) : IClassFixture<IdentityDbApiFactory>
{
    private HttpClient CreateClient()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3280");
        return client;
    }

    [DbFact]
    public async Task Jwks_TraVeKhoaCoKidKhopVoiHeaderToken()
    {
        var client = CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"jwks-{Guid.NewGuid():N}@vidu.com", password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" },
            JsonDefaults.Options);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonDefaults.Options);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(auth!.AccessToken);
        var kidInToken = jwt.Kid;
        kidInToken.Should().NotBeNullOrWhiteSpace();

        var jwksResponse = await client.GetAsync("/.well-known/jwks.json");
        jwksResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var jwks = await jwksResponse.Content.ReadFromJsonAsync<JwksDto>(JsonDefaults.Options);

        jwks!.Keys.Should().Contain(k => k.Kid == kidInToken && k.Kty == "RSA" && k.Use == "sig" && k.Alg == "RS256");
    }

    [DbFact]
    public async Task Token_CoAudMangVaClaimDungKhuon()
    {
        var client = CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"claims-{Guid.NewGuid():N}@vidu.com", password = "mat-khau-dung", displayName = "Học viên A", timeZone = "Asia/Ho_Chi_Minh" },
            JsonDefaults.Options);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonDefaults.Options);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(auth!.AccessToken);
        jwt.Audiences.Should().Contain(["af-identity", "af-chinese", "af-cms"]);
        jwt.TryGetClaim("name", out var nameClaim).Should().BeTrue();
        nameClaim.Value.Should().Be("Học viên A");
        jwt.TryGetClaim("zoneinfo", out var tzClaim).Should().BeTrue();
        tzClaim.Value.Should().Be("Asia/Ho_Chi_Minh");
    }

    [DbFact]
    public async Task Token_DuocChapNhanOEndpointBearerThat_GetApiAccount()
    {
        var client = CreateClient();
        var email = $"bearer-{Guid.NewGuid():N}@vidu.com";
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }, JsonDefaults.Options);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonDefaults.Options);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/account");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<AccountDto>(JsonDefaults.Options);
        account!.Email.Should().Be(email);
    }

    [DbFact]
    public async Task ThieuBearer_GetApiAccount_TraVe401()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/account");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// §5.2.2 — mô phỏng ĐÚNG nhánh service ngôn ngữ dùng thật (JwksConfigurationRetriever +
    /// ConfigurationManager, không phải khoá cục bộ của chính identity-service): tải JWKS qua
    /// <see cref="IDocumentRetriever"/> GIẢ (nội dung lấy từ chính endpoint /.well-known/jwks.json
    /// của factory), dựng TokenValidationParameters như <c>JwtBearerExtensions.ConfigureCommon</c>,
    /// kiểm token phát ra được CHẤP NHẬN với audience "af-chinese" — audience lạ bị TỪ CHỐI.
    /// </summary>
    [DbFact]
    public async Task TokenPhatRa_DuocNhanhJwksChapNhanVoiAudienceAfChinese()
    {
        var client = CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"jwksflow-{Guid.NewGuid():N}@vidu.com", password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" },
            JsonDefaults.Options);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonDefaults.Options);

        var jwksJson = await client.GetStringAsync("/.well-known/jwks.json");
        const string issuer = "http://localhost:5280/identity"; // Jwt:Issuer do IdentityApiFactory nạp (§9.2)

        var retriever = new JwksConfigurationRetriever(issuer);
        var configuration = await retriever.GetConfigurationAsync("ignored", new StaticDocumentRetriever(jwksJson), CancellationToken.None);

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(auth!.AccessToken, BuildValidationParameters(issuer, "af-chinese", configuration.SigningKeys));

        result.IsValid.Should().BeTrue(result.Exception?.ToString());
    }

    [DbFact]
    public async Task TokenPhatRa_AudienceLa_BiTuChoi()
    {
        var client = CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"jwksflowbad-{Guid.NewGuid():N}@vidu.com", password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" },
            JsonDefaults.Options);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonDefaults.Options);

        var jwksJson = await client.GetStringAsync("/.well-known/jwks.json");
        const string issuer = "http://localhost:5280/identity";

        var retriever = new JwksConfigurationRetriever(issuer);
        var configuration = await retriever.GetConfigurationAsync("ignored", new StaticDocumentRetriever(jwksJson), CancellationToken.None);

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(auth!.AccessToken, BuildValidationParameters(issuer, "af-mot-ngon-ngu-khac-khong-ton-tai", configuration.SigningKeys));

        result.IsValid.Should().BeFalse();
    }

    private static TokenValidationParameters BuildValidationParameters(string issuer, string audience, IEnumerable<SecurityKey> signingKeys) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        ClockSkew = TimeSpan.FromSeconds(30),
        IssuerSigningKeys = signingKeys
    };

    /// <summary>IDocumentRetriever giả — trả thẳng nội dung đã có sẵn thay vì tự gọi mạng (JwksConfigurationRetriever chỉ cần interface này, không quan tâm nguồn thật hay giả).</summary>
    private sealed class StaticDocumentRetriever(string document) : IDocumentRetriever
    {
        public Task<string> GetDocumentAsync(string address, CancellationToken cancel) => Task.FromResult(document);
    }

    private sealed record AuthResponseDto(string AccessToken, DateTime AccessTokenExpiresAt, AccountDto Account);

    private sealed record AccountDto(string Id, string Email, string DisplayName, string TimeZone, DateTime CreatedAt);

    private sealed record JwksDto(JwkDto[] Keys);

    private sealed record JwkDto(string Kty, string Use, string Alg, string Kid, string N, string E);
}
