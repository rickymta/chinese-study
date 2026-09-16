using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using AntFarm.Chinese.Application.Access;
using AntFarm.Chinese.Application.Common.Options;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Me;

/// <summary>
/// Review F3 17/09/2026 — hai request cùng provision LẦN ĐẦU một user gần như đồng thời không
/// được: (a) để lộ khoảng hở "user tồn tại nhưng chưa có vai trò" (roles rỗng oan), (b) ném 500 khi
/// một trong hai đụng độ khoá chính <c>access.users</c>.
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class ProvisioningRaceTests(ChineseDbApiFactory factory) : IClassFixture<ChineseDbApiFactory>
{
    /// <summary>Mức HTTP đầy đủ (đúng như review yêu cầu) — không khẳng định CHẮC CHẮN trúng đúng
    /// khoảng hở race (phụ thuộc lịch trình hệ điều hành), nhưng bảo đảm hành vi ĐÚNG dù có trúng
    /// race hay không: cả hai request đều 200 và có ĐỦ vai trò mặc định, không request nào 500.</summary>
    [DbFact]
    public async Task HaiRequestDongThoi_LanDauProvisionCungMotUser_Deu200VaCoDuVaiTro()
    {
        var accountId = Guid.NewGuid();
        var token = factory.TokenFactory.CreateToken(accountId, email: $"dua-{Guid.NewGuid():N}@vidu.com");

        HttpClient NewClient()
        {
            var c = factory.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return c;
        }

        var requests = Enumerable.Range(0, 6).Select(_ => NewClient().GetAsync("/api/me"));
        var responses = await Task.WhenAll(requests);

        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

        foreach (var response in responses)
        {
            var body = await response.Content.ReadFromJsonAsync<MeResponseDto>(JsonDefaults.Options);
            body!.Roles.Should().BeEquivalentTo(["learner"]);
        }

        // Không nhân đôi user_roles dù nhiều request cùng cố gán vai trò lần đầu.
        await using var db = TestDbContextFactory.Create();
        (await db.UserRoles.Where(ur => ur.UserId == accountId).CountAsync()).Should().Be(1);
        (await db.Users.Where(u => u.Id == accountId).CountAsync()).Should().Be(1);
    }

    /// <summary>
    /// Mức trực tiếp (khít cửa sổ race hơn HTTP — hai <see cref="UserProvisioningService"/> dùng
    /// hai <see cref="AntFarm.Chinese.Infrastructure.Persistence.ChineseDbContext"/> RIÊNG, gọi
    /// <c>EnsureAsync</c> đồng thời cho CÙNG accountId): người thua phải tự phục hồi qua nhánh bắt
    /// <c>DbUpdateException</c> (pk_users) trong <c>UserProvisioningService.CreateUserAsync</c>,
    /// KHÔNG được ném lỗi ra ngoài.
    /// </summary>
    [DbFact]
    public async Task HaiServiceDongThoi_CungTaoMotUser_KhongNemLoiVaChiConDungMotDong()
    {
        var accountId = Guid.NewGuid();
        var email = $"dua-truc-tiep-{Guid.NewGuid():N}@vidu.com";
        var accessOptions = new ChineseAccessOptions { DefaultRoles = ["learner"] };
        var adminOptions = new ChineseAdminOptions();

        async Task<Guid> RunAsync()
        {
            await using var db = TestDbContextFactory.Create();
            var service = new UserProvisioningService(
                db, accessOptions, adminOptions, TimeProvider.System, NullLogger<UserProvisioningService>.Instance);
            var principal = BuildPrincipal(accountId, email);
            return await service.EnsureAsync(principal, CancellationToken.None);
        }

        var act = () => Task.WhenAll(RunAsync(), RunAsync());

        await act.Should().NotThrowAsync();

        await using var verifyDb = TestDbContextFactory.Create();
        (await verifyDb.Users.CountAsync(u => u.Id == accountId)).Should().Be(1);
        (await verifyDb.UserRoles.CountAsync(ur => ur.UserId == accountId)).Should().Be(1);
    }

    private static ClaimsPrincipal BuildPrincipal(Guid accountId, string email)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", accountId.ToString()),
            new Claim("email", email),
            new Claim("name", "Học viên đua"),
            new Claim("zoneinfo", "Asia/Ho_Chi_Minh")
        ], authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private sealed record MeResponseDto(Guid Id, string Email, string DisplayName, string TimeZone, string[] Roles, string[] Permissions, DateTime FirstSeenAt);
}
