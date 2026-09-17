using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Admin;

/// <summary>GET /api/admin/users (+ /{id}) và GET /api/admin/roles (§6.3, R4-6).</summary>
[Collection(ChineseApiCollection.Name)]
public class AdminUsersTests(ChineseDbApiFactory factory) : IClassFixture<ChineseDbApiFactory>
{
    private HttpClient AdminClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(email: ChineseDbApiFactory.BootstrapAdminEmail));
        return client;
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    /// <summary>Provision một learner (qua /api/me) để chắc chắn đã có dòng trong access.users trước khi list.</summary>
    private async Task<Guid> ProvisionLearnerAsync(string email, string displayName = "Học viên")
    {
        var accountId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(accountId, email: email, displayName: displayName));
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        return accountId;
    }

    [DbFact]
    public async Task HocVien_GoiList_TraVe403()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(email: $"hv-list-{Guid.NewGuid():N}@vidu.com"));

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DbFact]
    public async Task KhongToken_GoiList_TraVe401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task TimTheoMotPhanEmail_TraVeDungMotKetQua()
    {
        var uniquePart = Guid.NewGuid().ToString("N")[..12];
        var email = $"tim-{uniquePart}@vidu.com";
        await ProvisionLearnerAsync(email);

        var response = await AdminClient().GetAsync($"/api/admin/users?q={uniquePart}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PageDto>(JsonDefaults.Options);
        page!.Items.Should().ContainSingle(u => u.Email == email);
    }

    [DbFact]
    public async Task PageSizeVuotQua100_TraVe400Validation()
    {
        var response = await AdminClient().GetAsync("/api/admin/users?pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("VALIDATION");
    }

    [DbFact]
    public async Task PageNho1_TraVe400Validation()
    {
        var response = await AdminClient().GetAsync("/api/admin/users?page=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [DbFact]
    public async Task GetById_IdLa_TraVe404()
    {
        var response = await AdminClient().GetAsync($"/api/admin/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("NOT_FOUND");
    }

    [DbFact]
    public async Task GetById_NguoiDungThat_TraVe200KemIsBootstrapAdmin()
    {
        var accountId = await ProvisionLearnerAsync($"getbyid-{Guid.NewGuid():N}@vidu.com");

        var response = await AdminClient().GetAsync($"/api/admin/users/{accountId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonDefaults.Options);
        user!.Id.Should().Be(accountId);
        user.IsBootstrapAdmin.Should().BeFalse();
        user.Roles.Should().Contain("learner");
    }

    [DbFact]
    public async Task GetRoles_TraVeAdminVaLearnerKemMoTaVaQuyen()
    {
        var response = await AdminClient().GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>(JsonDefaults.Options);
        roles.Should().HaveCount(2);
        roles![0].Code.Should().Be("admin");
        roles[0].Permissions.Should().Contain(["study.use", "content.manage", "users.manage"]);
        roles[1].Code.Should().Be("learner");
        roles[1].Permissions.Should().BeEquivalentTo(["study.use"]);
        roles.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.Description));
    }

    [DbFact]
    public async Task HocVien_GoiGetRoles_TraVe403()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(email: $"hv-roles-{Guid.NewGuid():N}@vidu.com"));

        var response = await client.GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record UserDto(Guid Id, string Email, string DisplayName, List<string> Roles, bool IsBootstrapAdmin, DateTime FirstSeenAt, DateTime LastSeenAt);
    private sealed record PageDto(List<UserDto> Items, int Page, int PageSize, int TotalCount);
    private sealed record RoleDto(string Code, string Name, string Description, List<string> Permissions);
    private sealed record ErrorDto(string Error, string Code);
}
