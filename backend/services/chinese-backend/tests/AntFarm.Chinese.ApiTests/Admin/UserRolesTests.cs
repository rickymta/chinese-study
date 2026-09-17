using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Admin;

/// <summary>
/// PUT /api/admin/users/{id}/roles (§6.3, R4-7..R4-9) — thay toàn bộ tập vai trò, LAST_ADMIN,
/// hiệu lực NGAY (không chờ cache 60s PermissionResolver). Mỗi test tự PROVISION admin "hạt
/// giống" của RIÊNG mình bằng <see cref="ChineseDbApiFactory.BootstrapAdminEmail"/> với một
/// <c>accountId</c> CỐ ĐỊNH sinh RIÊNG cho test đó (không tra cứu lại qua danh sách/tìm kiếm theo
/// email) — email bootstrap là HẰNG SỐ dùng chung nhiều lớp test chạy song song, tìm theo email sẽ
/// khớp NHẦM sang hàng của lớp test khác.
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class UserRolesTests(ChineseDbApiFactory factory) : IClassFixture<ChineseDbApiFactory>
{
    private HttpClient AdminClient(Guid accountId, string? email = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(
            factory.TokenFactory.CreateToken(accountId, email: email ?? ChineseDbApiFactory.BootstrapAdminEmail));
        return client;
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    /// <summary>Provision + trả về client của một admin "hạt giống" MỚI (email bootstrap, accountId cố định do test tự sinh) — dùng làm actor gọi PUT roles trong từng test.</summary>
    private async Task<(Guid Id, HttpClient Client)> ProvisionSeedAdminAsync()
    {
        var id = Guid.NewGuid();
        var client = AdminClient(id);
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        return (id, client);
    }

    private async Task<(Guid Id, HttpClient Client)> ProvisionLearnerAsync(string? email = null)
    {
        var accountId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(factory.TokenFactory.CreateToken(accountId, email: email ?? $"hv-{Guid.NewGuid():N}@vidu.com"));
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        return (accountId, client);
    }

    private static Task<HttpResponseMessage> PutRolesAsync(HttpClient actingClient, Guid targetId, params string[] roles) =>
        actingClient.PutAsJsonAsync($"/api/admin/users/{targetId}/roles", new { roles }, JsonDefaults.Options);

    /// <summary>
    /// Xoá TRỰC TIẾP qua DB mọi vai trò admin KHÔNG thuộc <paramref name="keepAdminIds"/> — bắt
    /// buộc trước mọi test kiểm LAST_ADMIN đếm được CHÍNH XÁC "đây là admin cuối cùng". Không có
    /// bước này, test dễ FLAKY/SAI vì `ChineseDbFixture` chỉ xoá sạch DB MỘT LẦN cho cả collection
    /// — các test KHÁC chạy trước trong CÙNG phiên (kể cả lớp khác, vd AdminPingTests,
    /// AdminUsersTests) đã tự provision admin "hạt giống" riêng và KHÔNG dọn dẹp, để lại nhiều
    /// admin dư thừa khiến "otherAdminCount" luôn > 0 dù kịch bản test chỉ định nghĩa đúng 1-2 admin.
    /// </summary>
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

    [DbFact]
    public async Task GanRoiGoLearner_TraVe200VaCapNhatDanhSach()
    {
        var (targetId, _) = await ProvisionLearnerAsync();
        var (_, admin) = await ProvisionSeedAdminAsync();

        var response = await PutRolesAsync(admin, targetId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDto>(JsonDefaults.Options);
        body!.Roles.Should().BeEmpty(); // R4-7: rỗng hợp lệ
    }

    [DbFact]
    public async Task VaiTroLa_TraVe422UnknownRole()
    {
        var (targetId, _) = await ProvisionLearnerAsync();
        var (_, admin) = await ProvisionSeedAdminAsync();

        var response = await PutRolesAsync(admin, targetId, "superman");

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await response.Content.ReadFromJsonAsync<ErrorWithDetailsDto>(JsonDefaults.Options);
        error!.Code.Should().Be("UNKNOWN_ROLE");
        error.Details!.Roles.Should().BeEquivalentTo(["superman"]);
    }

    [DbFact]
    public async Task IdKhongTonTai_TraVe404()
    {
        var (_, admin) = await ProvisionSeedAdminAsync();

        var response = await PutRolesAsync(admin, Guid.NewGuid(), "learner");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [DbFact]
    public async Task ThieuRoles_TraVe400Validation()
    {
        var (targetId, _) = await ProvisionLearnerAsync();
        var (_, admin) = await ProvisionSeedAdminAsync();

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{targetId}/roles", new { }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [DbFact]
    public async Task DuyNhatMotAdmin_TuGoAdmin_TraVe422LastAdmin()
    {
        var (seedAdminId, seedAdmin) = await ProvisionSeedAdminAsync();
        var soloAdminEmail = $"solo-admin-{Guid.NewGuid():N}@vidu.com";
        var (soloAdminId, _) = await ProvisionLearnerAsync(soloAdminEmail);

        (await PutRolesAsync(seedAdmin, soloAdminId, "admin", "learner")).StatusCode.Should().Be(HttpStatusCode.OK);
        // Cô lập: chỉ seedAdmin và soloAdmin được coi là admin lúc này (dọn sạch admin dư thừa từ
        // các test khác chạy TRƯỚC trong CÙNG phiên) rồi mới gỡ chính admin "hạt giống" — vẫn còn
        // soloAdmin ⇒ hợp lệ. Sau bước này soloAdmin là ADMIN DUY NHẤT thật sự.
        await IsolateAdminsAsync(seedAdminId, soloAdminId);
        (await PutRolesAsync(seedAdmin, seedAdminId, "learner")).StatusCode.Should().Be(HttpStatusCode.OK);

        var soloAdminClient = AdminClient(soloAdminId, soloAdminEmail);
        var response = await PutRolesAsync(soloAdminClient, soloAdminId, "learner");

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("LAST_ADMIN");
    }

    [DbFact]
    public async Task CoHaiAdmin_GoMot_TraVe200VaNguoiBiGoMatQuyenNgay()
    {
        var (seedAdminId, seedAdmin) = await ProvisionSeedAdminAsync();
        var secondAdminEmail = $"second-admin-{Guid.NewGuid():N}@vidu.com";
        var (secondAdminId, _) = await ProvisionLearnerAsync(secondAdminEmail);

        (await PutRolesAsync(seedAdmin, secondAdminId, "admin", "learner")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Người vừa được cấp admin GỌI ĐƯỢC /api/admin/ping (đủ quyền users.manage).
        var secondAdminClient = AdminClient(secondAdminId, secondAdminEmail);
        (await secondAdminClient.GetAsync("/api/admin/ping")).StatusCode.Should().Be(HttpStatusCode.OK);

        // seedAdmin gỡ vai trò admin của người thứ hai — vẫn còn CHÍNH seedAdmin ⇒ hợp lệ.
        var removeResponse = await PutRolesAsync(seedAdmin, secondAdminId, "learner");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // R4-9: PermissionResolver.Invalidate ngay ⇒ lần gọi KẾ TIẾP (không cần đợi 60 giây) đã mất quyền.
        var pingAfterRemoval = await secondAdminClient.GetAsync("/api/admin/ping");
        pingAfterRemoval.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DbFact]
    public async Task HocVien_GoiPutRoles_TraVe403()
    {
        var (targetId, _) = await ProvisionLearnerAsync();
        var (_, learnerClient) = await ProvisionLearnerAsync();

        var response = await PutRolesAsync(learnerClient, targetId, "learner");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// RK — đua LAST_ADMIN: hai admin CÙNG LÚC tự gỡ vai trò admin của CHÍNH MÌNH (R4-8 cho phép
    /// tự gỡ nếu còn admin khác). Test gỡ "nhau" theo nghĩa mỗi actor chỉ đổi vai trò CỦA CHÍNH
    /// HỌ (không phải của bên kia) — đổi vai trò CỦA BÊN KIA sẽ làm actor đó mất quyền
    /// users.manage GIỮA CHỪNG một cách hợp lệ (không phải bug) khiến bản thân request bị 403 ở
    /// tầng Authorization TRƯỚC KHI chạm business rule, không còn đo được đúng race LAST_ADMIN.
    /// Không khoá dòng access.roles (R4-8) thì cả hai request đều đọc "còn admin kia" và đều
    /// thành công ⇒ 0 admin còn lại (mất quyền quản trị vĩnh viễn). Có khoá FOR UPDATE ⇒ tuần tự
    /// hoá: đúng MỘT request thành công (200), request còn lại thấy admin đã mất trước đó ⇒ 422
    /// LAST_ADMIN — luôn còn đúng 1 admin sau cùng (không phải 0, không phải 2).
    /// </summary>
    [DbFact]
    public async Task HaiAdmin_DongThoiTuGoChinhMinh_ChiMotThanhCongConLaiLastAdmin()
    {
        var (seedAdminId, seedAdmin) = await ProvisionSeedAdminAsync();
        var emailA = $"race-a-{Guid.NewGuid():N}@vidu.com";
        var emailB = $"race-b-{Guid.NewGuid():N}@vidu.com";
        var (idA, _) = await ProvisionLearnerAsync(emailA);
        var (idB, _) = await ProvisionLearnerAsync(emailB);

        (await PutRolesAsync(seedAdmin, idA, "admin", "learner")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await PutRolesAsync(seedAdmin, idB, "admin", "learner")).StatusCode.Should().Be(HttpStatusCode.OK);
        // Cô lập TRƯỚC: chỉ seedAdmin/A/B — dọn admin dư thừa từ test khác — rồi mới gỡ seedAdmin,
        // để lại ĐÚNG 2 admin (A, B) làm nền cho phép thử đua.
        await IsolateAdminsAsync(seedAdminId, idA, idB);
        (await PutRolesAsync(seedAdmin, seedAdminId, "learner")).StatusCode.Should().Be(HttpStatusCode.OK);

        var clientA = AdminClient(idA, emailA);
        var clientB = AdminClient(idB, emailB);

        var removeSelfA = PutRolesAsync(clientA, idA, "learner");
        var removeSelfB = PutRolesAsync(clientB, idB, "learner");
        var results = await Task.WhenAll(removeSelfA, removeSelfB);

        var statusCodes = results.Select(r => r.StatusCode).ToList();
        statusCodes.Should().Contain(HttpStatusCode.OK);
        statusCodes.Should().Contain((HttpStatusCode)422);

        var errorResponse = results.First(r => r.StatusCode == (HttpStatusCode)422);
        var error = await errorResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("LAST_ADMIN");

        // Kiểm KẾT QUẢ CUỐI qua DB trực tiếp (KHÔNG qua API) — người thua cuộc trong đua có thể đã
        // MẤT quyền users.manage nên client của họ không còn gọi được /api/admin/* để tự kiểm tra.
        await using var db = TestDbContextFactory.Create();
        var adminRoleId = await db.Roles.Where(r => r.Code == "admin").Select(r => r.Id).SingleAsync();
        var remainingAdminIds = await db.UserRoles.Where(ur => ur.RoleId == adminRoleId && (ur.UserId == idA || ur.UserId == idB))
            .Select(ur => ur.UserId).ToListAsync();
        remainingAdminIds.Should().HaveCount(1); // đúng 1 trong hai (A hoặc B) còn admin — không phải 0, không phải cả hai.
    }

    private sealed record UserDto(Guid Id, string Email, string DisplayName, List<string> Roles, bool IsBootstrapAdmin, DateTime FirstSeenAt, DateTime LastSeenAt);
    private sealed record ErrorDto(string Error, string Code);
    private sealed record ErrorDetailsDto(List<string> Roles);
    private sealed record ErrorWithDetailsDto(string Error, string Code, ErrorDetailsDto? Details);
}
