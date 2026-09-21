using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Cms.Application.Common.Options;
using AntFarm.Cms.ApiTests.Infrastructure;
using AntFarm.Cms.Infrastructure.Seeding;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AntFarm.Cms.ApiTests.Access;

/// <summary>
/// GET /api/admin/users (+ /{id}), GET /api/admin/roles, PUT /api/admin/users/{id}/roles (§6.1,
/// §5.2.1 W1) — mọi endpoint canh quyền "users.manage". Chép khuôn
/// AntFarm.Chinese.ApiTests.Admin.{AdminUsersTests,UserRolesTests} (gộp một file theo cây thư mục
/// hợp đồng W1–W15 §5.2.1).
/// </summary>
[Collection(CmsApiCollection.Name)]
public class AdminUsersTests(CmsDbApiFactory factory) : IClassFixture<CmsDbApiFactory>
{
    private HttpClient AdminClient(Guid? accountId = null, string? email = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(
            accountId, email: email ?? CmsDbApiFactory.BootstrapAdminEmail, audiences: ["af-cms"]));
        return client;
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    /// <summary>Provision một tài khoản 0 quyền (qua /api/me) để chắc chắn đã có dòng trong access.users trước khi list/gán vai trò.</summary>
    private async Task<(Guid Id, HttpClient Client)> ProvisionAsync(string? email = null)
    {
        var accountId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(
            factory.TokenFactory.CreateToken(accountId, email: email ?? $"nd-{Guid.NewGuid():N}@vidu.com", audiences: ["af-cms"]));
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        return (accountId, client);
    }

    /// <summary>Provision + trả về client của một admin "hạt giống" MỚI (email bootstrap, accountId cố định do test tự sinh).</summary>
    private Task<(Guid Id, HttpClient Client)> ProvisionSeedAdminAsync() => ProvisionAsync(CmsDbApiFactory.BootstrapAdminEmail);

    private static Task<HttpResponseMessage> PutRolesAsync(HttpClient actingClient, Guid targetId, params string[] roles) =>
        actingClient.PutAsJsonAsync($"/api/admin/users/{targetId}/roles", new { roles }, JsonDefaults.Options);

    /// <summary>Xoá TRỰC TIẾP qua DB mọi vai trò admin KHÔNG thuộc <paramref name="keepAdminIds"/> — cô lập admin dư thừa từ các test khác chạy TRƯỚC trong CÙNG collection (chép khuôn UserRolesTests.IsolateAdminsAsync).</summary>
    private static async Task IsolateAdminsAsync(params Guid[] keepAdminIds)
    {
        await using var db = TestDbContextFactory.Create();
        var adminRoleId = await db.Roles.Where(r => r.Code == "admin").Select(r => r.Id).SingleAsync();
        var extraAdminAssignments = await db.UserRoles
            .Where(ur => ur.RoleId == adminRoleId && !keepAdminIds.Contains(ur.UserId))
            .ToListAsync();
        if (extraAdminAssignments.Count > 0)
        {
            db.UserRoles.RemoveRange(extraAdminAssignments);
            await db.SaveChangesAsync();
        }
    }

    // ── GET /api/admin/users ──────────────────────────────────────────────

    [DbFact]
    public async Task NguoiDungKhongQuyen_GoiList_TraVe403()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(email: $"list-{Guid.NewGuid():N}@vidu.com", audiences: ["af-cms"]));

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
        await ProvisionAsync(email);

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
    public async Task GetById_KhongTonTai_TraVe404()
    {
        var response = await AdminClient().GetAsync($"/api/admin/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("NOT_FOUND");
    }

    [DbFact]
    public async Task GetById_NguoiDungThat_TraVe200KemIsBootstrapAdmin()
    {
        var (accountId, _) = await ProvisionAsync($"getbyid-{Guid.NewGuid():N}@vidu.com");

        var response = await AdminClient().GetAsync($"/api/admin/users/{accountId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonDefaults.Options);
        user!.Id.Should().Be(accountId);
        user.IsBootstrapAdmin.Should().BeFalse();
        user.Roles.Should().BeEmpty(); // R-W2: fail-closed
    }

    // ── GET /api/admin/roles ──────────────────────────────────────────────

    [DbFact]
    public async Task GetRoles_TraVeBaVaiTroDungThuTuVaQuyen()
    {
        var response = await AdminClient().GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>(JsonDefaults.Options);
        roles.Should().HaveCount(3);
        roles![0].Code.Should().Be("admin");
        roles[0].Name.Should().Be("Quản trị viên");
        roles[0].Permissions.Should().BeEquivalentTo(["accounts.manage", "inbox.manage", "media.manage", "posts.manage", "site.manage", "users.manage"]);
        roles[1].Code.Should().Be("editor");
        roles[1].Name.Should().Be("Biên tập website");
        roles[1].Permissions.Should().BeEquivalentTo(["media.manage", "posts.manage", "site.manage"]);
        roles[2].Code.Should().Be("support");
        roles[2].Name.Should().Be("Hỗ trợ người học");
        roles[2].Permissions.Should().BeEquivalentTo(["accounts.manage", "inbox.manage"]);
    }

    [DbFact]
    public async Task NguoiDungKhongQuyen_GoiGetRoles_TraVe403()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(email: $"roles-{Guid.NewGuid():N}@vidu.com", audiences: ["af-cms"]));

        var response = await client.GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── PUT /api/admin/users/{id}/roles ───────────────────────────────────

    [DbFact]
    public async Task GanEditor_TraVe200VaNguoiDoNhanQuyenPostsManageNgay()
    {
        var (targetId, targetClient) = await ProvisionAsync();
        var (_, admin) = await ProvisionSeedAdminAsync();

        var response = await PutRolesAsync(admin, targetId, "editor");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDto>(JsonDefaults.Options);
        body!.Roles.Should().BeEquivalentTo(["editor"]);

        var me = await (await targetClient.GetAsync("/api/me")).Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options);
        me!.Permissions.Should().Contain("posts.manage");
    }

    [DbFact]
    public async Task VaiTroLa_TraVe422UnknownRole()
    {
        var (targetId, _) = await ProvisionAsync();
        var (_, admin) = await ProvisionSeedAdminAsync();

        var response = await PutRolesAsync(admin, targetId, "sieu-quan-tri");

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await response.Content.ReadFromJsonAsync<ErrorWithDetailsDto>(JsonDefaults.Options);
        error!.Code.Should().Be("UNKNOWN_ROLE");
        error.Details!.Roles.Should().BeEquivalentTo(["sieu-quan-tri"]);
    }

    [DbFact]
    public async Task IdKhongTonTai_TraVe404()
    {
        var (_, admin) = await ProvisionSeedAdminAsync();

        var response = await PutRolesAsync(admin, Guid.NewGuid(), "editor");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [DbFact]
    public async Task ThieuRoles_TraVe400Validation()
    {
        var (targetId, _) = await ProvisionAsync();
        var (_, admin) = await ProvisionSeedAdminAsync();

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{targetId}/roles", new { }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [DbFact]
    public async Task DuyNhatMotAdmin_TuGoAdmin_TraVe422LastAdmin()
    {
        var (seedAdminId, seedAdmin) = await ProvisionSeedAdminAsync();
        var soloAdminEmail = $"solo-admin-{Guid.NewGuid():N}@vidu.com";
        var (soloAdminId, _) = await ProvisionAsync(soloAdminEmail);

        (await PutRolesAsync(seedAdmin, soloAdminId, "admin")).StatusCode.Should().Be(HttpStatusCode.OK);
        // Cô lập: chỉ seedAdmin và soloAdmin được coi là admin lúc này, rồi mới gỡ chính admin
        // "hạt giống" — vẫn còn soloAdmin ⇒ hợp lệ. Sau bước này soloAdmin là ADMIN DUY NHẤT thật sự.
        await IsolateAdminsAsync(seedAdminId, soloAdminId);
        (await PutRolesAsync(seedAdmin, seedAdminId, "editor")).StatusCode.Should().Be(HttpStatusCode.OK);

        var soloAdminClient = AdminClient(soloAdminId, soloAdminEmail);
        var response = await PutRolesAsync(soloAdminClient, soloAdminId, "editor");

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("LAST_ADMIN");
    }

    [DbFact]
    public async Task CoHaiAdmin_GoMot_TraVe200VaNguoiBiGoMatQuyenNgay()
    {
        var (seedAdminId, seedAdmin) = await ProvisionSeedAdminAsync();
        var secondAdminEmail = $"second-admin-{Guid.NewGuid():N}@vidu.com";
        var (secondAdminId, _) = await ProvisionAsync(secondAdminEmail);

        (await PutRolesAsync(seedAdmin, secondAdminId, "admin")).StatusCode.Should().Be(HttpStatusCode.OK);

        var secondAdminClient = AdminClient(secondAdminId, secondAdminEmail);
        (await secondAdminClient.GetAsync("/api/admin/ping")).StatusCode.Should().Be(HttpStatusCode.OK);

        var removeResponse = await PutRolesAsync(seedAdmin, secondAdminId, "editor");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // PermissionResolver.Invalidate ngay ⇒ lần gọi KẾ TIẾP (không cần đợi 60 giây) đã mất quyền.
        var pingAfterRemoval = await secondAdminClient.GetAsync("/api/admin/ping");
        pingAfterRemoval.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DbFact]
    public async Task NguoiDungKhongQuyen_GoiPutRoles_TraVe403()
    {
        var (targetId, _) = await ProvisionAsync();
        var (_, noPermClient) = await ProvisionAsync();

        var response = await PutRolesAsync(noPermClient, targetId, "editor");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Seeder idempotent ──────────────────────────────────────────────────

    /// <summary>
    /// Chạy AccessSeeder.SeedAsync thêm hai lần trên DB đã seed sẵn (CmsDbFixture chạy lần đầu) —
    /// không được nhân đôi vai trò/quyền, không ném; user đã tồn tại có email vừa thêm vào
    /// bootstrap thì được gán admin ngay ở lượt seed kế tiếp (nửa "user đã tồn tại" của R-W10).
    /// </summary>
    [DbFact]
    public async Task SeedAsync_ChayNhieuLan_KhongNhanDoiVaBootstrapChoUserDaTonTai()
    {
        var email = $"seed-tuong-lai-{Guid.NewGuid():N}@vidu.com";
        // Giữ NGUYÊN accountId/client — /api/me sau đó phải đọc lại đúng dòng access.users này.
        var (_, userClient) = await ProvisionAsync(email);

        await using (var db = TestDbContextFactory.Create())
        {
            // Lượt 1: chạy lại seed KHÔNG có email này trong bootstrap — không có gì đổi, không ném.
            await AccessSeeder.SeedAsync(db, new CmsAdminOptions(), TimeProvider.System, NullLogger.Instance, CancellationToken.None);
            var roleCountAfterFirstRerun = await db.Roles.CountAsync();
            roleCountAfterFirstRerun.Should().Be(3); // không nhân đôi

            // Lượt 2: bootstrap thêm email này ⇒ gán bù admin cho user ĐÃ TỒN TẠI (R-W10).
            await AccessSeeder.SeedAsync(db, new CmsAdminOptions { BootstrapEmails = [email] }, TimeProvider.System, NullLogger.Instance, CancellationToken.None);
        }

        var me = await (await userClient.GetAsync("/api/me")).Content.ReadFromJsonAsync<MeDto>(JsonDefaults.Options);
        me!.Roles.Should().Contain("admin");
    }

    private sealed record MeDto(Guid Id, string Email, string DisplayName, string TimeZone, string[] Roles, string[] Permissions, DateTime FirstSeenAt);
    private sealed record UserDto(Guid Id, string Email, string DisplayName, List<string> Roles, bool IsBootstrapAdmin, DateTime FirstSeenAt, DateTime LastSeenAt);
    private sealed record PageDto(List<UserDto> Items, int Page, int PageSize, int TotalCount);
    private sealed record RoleDto(string Code, string Name, List<string> Permissions);
    private sealed record ErrorDto(string Error, string Code);
    private sealed record ErrorDetailsDto(List<string> Roles);
    private sealed record ErrorWithDetailsDto(string Error, string Code, ErrorDetailsDto? Details);
}
