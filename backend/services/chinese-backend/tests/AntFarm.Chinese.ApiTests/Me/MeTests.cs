using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Me;

/// <summary>GET /api/me (§6.3, §7 F3) — provisioning + phân quyền cục bộ.</summary>
[Collection(ChineseApiCollection.Name)]
public class MeTests(ChineseDbApiFactory factory) : IClassFixture<ChineseDbApiFactory>
{
    [DbFact]
    public async Task LanDauGoi_TaoUserMoiVaGanVaiTroHocVien()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(email: $"hoc-vien-{Guid.NewGuid():N}@vidu.com"));

        var response = await client.GetAsync("/api/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options);
        me.Should().NotBeNull();
        me!.Roles.Should().BeEquivalentTo(["learner"]);
        me.Permissions.Should().BeEquivalentTo(["study.use"]);
    }

    [DbFact]
    public async Task EmailBootstrap_DuocGanVaiTroAdmin()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(email: ChineseDbApiFactory.BootstrapAdminEmail));

        var response = await client.GetAsync("/api/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options);
        me!.Roles.Should().BeEquivalentTo(["admin", "learner"]);
        me.Permissions.Should().BeEquivalentTo(["content.manage", "study.use", "users.manage"]);
    }

    [DbFact]
    public async Task KhongCoToken_TraVe401Json()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("UNAUTHENTICATED");
    }

    [DbFact]
    public async Task TokenSaiAudience_TraVe401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(audiences: ["af-identity"]));

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task TokenSaiIssuer_TraVe401()
    {
        using var otherIssuerFactory = new TestTokenFactory("https://issuer-khac.test");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(otherIssuerFactory.CreateToken());

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task TokenHetHan_TraVe401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(
            factory.TokenFactory.CreateToken(expiresAt: DateTime.UtcNow.AddMinutes(-1)));

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>R-P8: admin gỡ hết vai trò một người dùng (thao tác trực tiếp trên DB, mô phỏng
    /// F4 UserAdminService.SetRolesAsync(roles: [])) ⇒ /api/me của người đó phải phản ánh NGAY
    /// (không qua cache 60s của PermissionResolver — MeService đọc thẳng DB, xem comment lớp đó).</summary>
    [DbFact]
    public async Task GoHetVaiTro_MeTraVeQuyenRong()
    {
        var accountId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(accountId));

        var first = await client.GetAsync("/api/me");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options);
        firstBody!.Roles.Should().NotBeEmpty();

        await using (var db = TestDbContextFactory.Create())
        {
            var toRemove = await db.UserRoles.Where(ur => ur.UserId == accountId).ToListAsync();
            db.UserRoles.RemoveRange(toRemove);
            await db.SaveChangesAsync();
        }

        var second = await client.GetAsync("/api/me");
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options);
        secondBody!.Roles.Should().BeEmpty();
        secondBody.Permissions.Should().BeEmpty();
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private sealed record MeDto(Guid Id, string Email, string DisplayName, string TimeZone, string[] Roles, string[] Permissions, DateTime FirstSeenAt);

    private sealed record ErrorDto(string Error, string Code);
}
