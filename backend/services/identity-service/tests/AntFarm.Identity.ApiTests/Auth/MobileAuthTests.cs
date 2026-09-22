using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Identity.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>M1 §5.2.3 #1, #2, #12 — đăng ký/đăng nhập mobile: token trong body, không cookie, DB ghi đúng kênh; token mobile được chấp nhận như token web.</summary>
[Collection(IdentityApiCollection.Name)]
public class MobileAuthTests(IdentityDbApiFactory factory) : IClassFixture<IdentityDbApiFactory>
{
    private static string NewEmail() => $"mobile-{Guid.NewGuid():N}@vidu.com";

    /// <summary>Client KHÔNG có Origin mặc định (khác các test web) — app native không gửi Origin (RM-A4).</summary>
    private HttpClient CreateClient() => factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
        HandleCookies = false,
        AllowAutoRedirect = false
    });

    private static HttpRequestMessage MobileRequest(string path, object body, string clientHeader = "chinese-mobile/1.0.0+1 (android)")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: JsonDefaults.Options)
        };
        request.Headers.Add("X-AF-Client", clientHeader);
        return request;
    }

    [DbFact]
    public async Task Register_TraVe201_CoRefreshTokenTrongBodyKhongCookie_DbGhiDungKenh()
    {
        var client = CreateClient();
        var email = NewEmail();

        var response = await client.SendAsync(MobileRequest("/api/auth/mobile/register",
            new { email, password = "mat-khau-du-dai", displayName = "Học viên", timeZone = "Asia/Ho_Chi_Minh", deviceName = "Pixel 8" }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.TryGetValues("Set-Cookie", out _).Should().BeFalse();
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.Pragma.Should().Contain(v => v.Name == "no-cache");

        var body = await response.Content.ReadFromJsonAsync<MobileAuthResponseDto>(JsonDefaults.Options);
        body.Should().NotBeNull();
        body!.RefreshToken.Should().MatchRegex("^[0-9a-f]{64}$");
        body.RefreshTokenExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
        body.Account.Email.Should().Be(email);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AntFarm.Identity.Infrastructure.Persistence.IdentityDbContext>();
        var token = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.AccountId == body.Account.Id);
        token.ClientType.Should().Be(AntFarm.Identity.Domain.Accounts.RefreshClientType.Mobile);
        token.ClientApp.Should().Be("chinese-mobile/1.0.0+1 (android)");
        token.DeviceName.Should().Be("Pixel 8");
    }

    [DbFact]
    public async Task Login_SaiMatKhau_TraVe401_MuoiLanSai_TraVe423_DangKyDongThiTraVe403()
    {
        var client = CreateClient();
        var email = NewEmail();
        (await client.SendAsync(MobileRequest("/api/auth/mobile/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }))).EnsureSuccessStatusCode();

        var wrong = await client.SendAsync(MobileRequest("/api/auth/mobile/login", new { email, password = "sai" }));
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await wrong.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("INVALID_CREDENTIALS");

        for (var i = 0; i < 8; i++)
            await client.SendAsync(MobileRequest("/api/auth/mobile/login", new { email, password = "sai" }));

        var locked = await client.SendAsync(MobileRequest("/api/auth/mobile/login", new { email, password = "sai" }));
        locked.StatusCode.Should().Be((HttpStatusCode)423);
        (await locked.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("ACCOUNT_LOCKED");
    }

    /// <summary>
    /// Dựng FACTORY RIÊNG (không phải <paramref name="factory"/> dùng chung cả collection) vì cần
    /// đổi biến môi trường DÙNG CHUNG TOÀN TIẾN TRÌNH <c>Auth__AllowRegistration</c> — constructor
    /// của <see cref="IdentityDbApiFactory"/> LUÔN đặt lại thành "true" nên phải override SAU khi
    /// dựng, TRƯỚC <c>CreateClient()</c> (nơi host thật sự được build và đọc cấu hình).
    /// </summary>
    [DbFact]
    public async Task DangKyDong_TraVe403RegistrationClosed()
    {
        using var closedFactory = new IdentityDbApiFactory();
        Environment.SetEnvironmentVariable("Auth__AllowRegistration", "false");
        try
        {
            var client = closedFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
            var response = await client.SendAsync(MobileRequest("/api/auth/mobile/register",
                new { email = NewEmail(), password = "mat-khau-du-dai", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }));

            response.StatusCode.Should().Be((HttpStatusCode)403);
            (await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options))!.Code.Should().Be("REGISTRATION_CLOSED");
        }
        finally
        {
            Environment.SetEnvironmentVariable("Auth__AllowRegistration", "true");
        }
    }

    /// <summary>§5.2.3 #12 — token mobile không khác gì token web (TokenIssuer không biết client type): xác nhận bằng Bearer thật ở endpoint /api/account (audience af-identity, cùng cơ chế af-chinese dùng qua JWKS — đã kiểm ở JwksAndTokenTests).</summary>
    [DbFact]
    public async Task TokenMobile_DuocChapNhanOEndpointBearer()
    {
        var client = CreateClient();
        var email = NewEmail();
        var register = await client.SendAsync(MobileRequest("/api/auth/mobile/register",
            new { email, password = "mat-khau-dung", displayName = "A", timeZone = "Asia/Ho_Chi_Minh" }));
        var body = await register.Content.ReadFromJsonAsync<MobileAuthResponseDto>(JsonDefaults.Options);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/account");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record MobileAuthResponseDto(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt, AccountDto Account);

    private sealed record AccountDto(Guid Id, string Email, string DisplayName, string TimeZone, DateTime CreatedAt);

    private sealed record ErrorDto(string Error, string Code);
}
