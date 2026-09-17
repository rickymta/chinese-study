using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using AntFarm.Cms.ApiTests.Infrastructure;
using AntFarm.Testing;
using FluentAssertions;
using Xunit;

namespace AntFarm.Cms.ApiTests.Access;

/// <summary>
/// GET /api/admin/ping (§6.1, §5.2.1 W1) — canh gác quyền "users.manage"
/// (RequirePermissionAttribute) + JWT bearer (audience/issuer/hạn dùng) + guard tĩnh chống lỗi
/// "new string Policy" (CLAUDE.md).
/// </summary>
[Collection(CmsApiCollection.Name)]
public class AuthorizationGuardTests(CmsDbApiFactory factory) : IClassFixture<CmsDbApiFactory>
{
    [DbFact]
    public async Task NguoiDungKhongQuyen_GoiPing_TraVe403Json()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.TokenFactory.CreateToken(email: $"khong-quyen-{Guid.NewGuid():N}@vidu.com", audiences: ["af-cms"]));

        var response = await client.GetAsync("/api/admin/ping");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("FORBIDDEN");
    }

    [DbFact]
    public async Task Admin_GoiPing_TraVe200()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.TokenFactory.CreateToken(email: CmsDbApiFactory.BootstrapAdminEmail, audiences: ["af-cms"]));

        var response = await client.GetAsync("/api/admin/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PingDto>(JsonDefaults.Options);
        body!.Ok.Should().BeTrue();
    }

    [DbFact]
    public async Task KhongCoToken_GoiMe_TraVe401Json()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("UNAUTHENTICATED");
    }

    [DbFact]
    public async Task TokenChiCoAudienceKhac_TraVe401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.TokenFactory.CreateToken(audiences: ["af-chinese"]));

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task TokenHetHan_TraVe401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.TokenFactory.CreateToken(audiences: ["af-cms"], expiresAt: DateTime.UtcNow.AddMinutes(-1)));

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task TokenSaiIssuer_TraVe401()
    {
        using var otherIssuerFactory = new TestTokenFactory("https://issuer-khac.test");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", otherIssuerFactory.CreateToken(audiences: ["af-cms"]));

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record ErrorDto(string Error, string Code);
    private sealed record PingDto(bool Ok);
}

/// <summary>
/// Bài học MedDental (CLAUDE.md): <c>public new string Policy</c> ở lớp con che thuộc tính TĨNH
/// nhưng KHÔNG đổi ánh xạ interface IAuthorizeData — mọi [RequirePermission] thoái hoá thành
/// [Authorize] trơn, không log, không lỗi biên dịch. Guard tĩnh chống tái phát.
///
/// ⚠️ KHÔNG gộp vào <see cref="AuthorizationGuardTests"/>: lớp đó dùng
/// <c>IClassFixture&lt;CmsDbApiFactory&gt;</c> — xUnit vẫn DỰNG class fixture (constructor
/// <c>CmsDbApiFactory</c> ném <c>AF_TEST_PG chưa được đặt</c>) cho MỌI test trong lớp kể cả
/// <c>[Fact]</c> không cần DB, làm test này FAIL thay vì chạy được không cần PostgreSQL.
/// </summary>
public class PolicyDeclarationGuardTests
{
    [Fact]
    public void KhongCoLopNaoKhaiNewStringPolicy()
    {
        var repoRoot = FindRepoRoot();
        var backendDir = Path.Combine(repoRoot, "backend");
        // Loại CHÍNH file test này khỏi vùng quét — nó chứa nguyên văn chuỗi tìm kiếm trong một
        // chuỗi literal (không phải khai báo thật) nên sẽ tự báo dương tính giả với chính nó.
        var selfPath = Path.GetFullPath(GetThisFilePath());
        var csFiles = Directory.EnumerateFiles(backendDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !string.Equals(Path.GetFullPath(f), selfPath, StringComparison.OrdinalIgnoreCase));

        // Chỉ xét DÒNG CODE thật (không phải chú thích XML doc "/// ... <c>new string Policy</c>"
        // — cả AntFarm.Auth lẫn mọi controller [RequirePermission] đều CỐ Ý nhắc lại cụm này
        // trong doc-comment để cảnh báo, nên grep thô trên toàn nội dung file sẽ báo dương tính
        // giả trên chính những dòng đang CẢNH BÁO đừng làm vậy).
        var offenders = csFiles
            .Where(f => File.ReadLines(f).Any(line =>
                !line.TrimStart().StartsWith("///", StringComparison.Ordinal)
                && !line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                && global::System.Text.RegularExpressions.Regex.IsMatch(line, @"\bnew\s+string\??\s+Policy\b")))
            .ToList();

        offenders.Should().BeEmpty("RequirePermissionAttribute phải gán Policy trong constructor, không được 'new' che thuộc tính (CLAUDE.md)");
    }

    private static string GetThisFilePath([CallerFilePath] string path = "") => path;

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("Không tìm thấy gốc repo (CLAUDE.md) từ " + AppContext.BaseDirectory);
    }
}
