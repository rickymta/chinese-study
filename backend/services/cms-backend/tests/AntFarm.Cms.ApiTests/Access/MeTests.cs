using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Cms.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Cms.ApiTests.Access;

/// <summary>GET /api/me (§6.1, §5.2.1 W1) — provisioning + phân quyền cục bộ fail-closed (R-W2).</summary>
[Collection(CmsApiCollection.Name)]
public class MeTests(CmsDbApiFactory factory) : IClassFixture<CmsDbApiFactory>
{
    [DbFact]
    public async Task LanDauGoi_TaoUserMoiKhongVaiTroKhongQuyen()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(
            email: $"bien-tap-{Guid.NewGuid():N}@vidu.com", audiences: ["af-cms"]));

        var response = await client.GetAsync("/api/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options);
        me.Should().NotBeNull();
        // R-W2: fail-closed — người mới provision KHÔNG nhận vai trò mặc định nào.
        me!.Roles.Should().BeEmpty();
        me.Permissions.Should().BeEmpty();
    }

    [DbFact]
    public async Task EmailBootstrap_DuocGanVaiTroAdminVaDuSauQuyen()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(
            factory.TokenFactory.CreateToken(email: CmsDbApiFactory.BootstrapAdminEmail, audiences: ["af-cms"]));

        var response = await client.GetAsync("/api/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options);
        me!.Roles.Should().BeEquivalentTo(["admin"]);
        me.Permissions.Should().BeEquivalentTo([
            "accounts.manage", "inbox.manage", "media.manage", "posts.manage", "site.manage", "users.manage"
        ]);
    }

    [DbFact]
    public async Task GoHetVaiTro_MeTraVeQuyenRongNgay()
    {
        var accountId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(
            factory.TokenFactory.CreateToken(accountId, email: CmsDbApiFactory.BootstrapAdminEmail, audiences: ["af-cms"]));

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
}
