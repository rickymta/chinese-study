using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Chinese.Infrastructure.Persistence;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Pinyin;

/// <summary>
/// §6.1, §7 F5 — chart/guide (ETag), nộp bài luyện thanh (idempotent, R5-11, local_date theo múi
/// giờ VN), thống kê. Đặt <c>Content__RootPath</c> TUYỆT ĐỐI trỏ <c>content/chinese</c> của repo
/// (tìm bằng cách đi ngược từ <see cref="AppContext.BaseDirectory"/> — RK37) TRƯỚC khi
/// <see cref="ChineseDbApiFactory"/> (dùng chung, <see cref="IClassFixture{TFixture}"/>) build host
/// LẦN ĐẦU — set lại ở đầu MỖI test (constructor của lớp test chạy trước mỗi test method) để không
/// phụ thuộc thứ tự chạy so với test khác cũng dùng biến môi trường process-wide này.
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class PinyinApiTests : IClassFixture<ChineseDbApiFactory>
{
    private readonly ChineseDbApiFactory _factory;

    public PinyinApiTests(ChineseDbApiFactory factory)
    {
        _factory = factory;
        Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());
    }

    [DbFact]
    public async Task GetChart_KhongToken_Tra401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/pinyin/chart");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DbFact]
    public async Task GetChart_NguoiDungBiGoHetVaiTro_Tra403Forbidden()
    {
        var accountId = Guid.NewGuid();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(_factory.TokenFactory.CreateToken(accountId));

        // Provision qua /api/me ([Authorize] thường, KHÔNG qua PermissionResolver) để tránh nạp
        // cache quyền học của user này trước khi gỡ vai trò — PermissionResolver cache 60s theo
        // userId, gỡ vai trò SAU khi cache đã có sẽ khiến assertion dưới flaky theo thời gian chạy.
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var db = TestDbContextFactory.Create())
        {
            var roles = await db.UserRoles.Where(ur => ur.UserId == accountId).ToListAsync();
            db.UserRoles.RemoveRange(roles);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/pinyin/chart");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("FORBIDDEN");
    }

    [DbFact]
    public async Task GetChart_Learner_Tra200ConSyllablesRoiCacheEtagTra304()
    {
        var client = LearnerClient();

        var first = await client.GetAsync("/api/pinyin/chart");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var chart = await first.Content.ReadFromJsonAsync<ChartDto>(JsonDefaults.Options);
        chart!.Syllables.Should().NotBeEmpty();
        first.Headers.ETag.Should().NotBeNull();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/pinyin/chart");
        request.Headers.IfNoneMatch.Add(first.Headers.ETag!);
        var second = await client.SendAsync(request);

        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [DbFact]
    public async Task PostToneDrills_20CauHopLe_Tra201VaTong20()
    {
        var client = LearnerClient();
        var request = BuildValidListenToneRequest(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/pinyin/tone-drills", request, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SubmitResponseDto>(JsonDefaults.Options);
        body!.Total.Should().Be(20);
        body.ByTone.Should().HaveCount(4);
        body.ByTone.Values.Sum(t => t.Total).Should().Be(20);
    }

    [DbFact]
    public async Task PostToneDrills_NopLaiCungClientSessionId_Tra200KhongNhanDoi()
    {
        var client = LearnerClient();
        var clientSessionId = Guid.NewGuid();
        var request = BuildValidListenToneRequest(clientSessionId);

        var first = await client.PostAsJsonAsync("/api/pinyin/tone-drills", request, JsonDefaults.Options);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = await first.Content.ReadFromJsonAsync<SubmitResponseDto>(JsonDefaults.Options);

        var second = await client.PostAsJsonAsync("/api/pinyin/tone-drills", request, JsonDefaults.Options);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<SubmitResponseDto>(JsonDefaults.Options);
        secondBody!.Id.Should().Be(firstBody!.Id);

        await using var db = TestDbContextFactory.Create();
        var sessionCount = await db.ToneDrillSessions.CountAsync(s => s.ClientSessionId == clientSessionId);
        sessionCount.Should().Be(1);
        var studyEventCount = await db.StudyEvents.CountAsync(e => e.RefId == firstBody.Id);
        studyEventCount.Should().Be(1);
    }

    /// <summary>review F5 17/09/2026: hai request cùng nộp một clientSessionId gần như đồng thời (đua thật, không tuần tự) — chỉ MỘT phiên được tạo, cả hai response đều thành công (201 hoặc 200, không request nào 500).</summary>
    [DbFact]
    public async Task PostToneDrills_HaiRequestDuaCungClientSessionId_ChiMotPhienCaHaiThanhCong()
    {
        var client = LearnerClient();
        var clientSessionId = Guid.NewGuid();
        var request = BuildValidListenToneRequest(clientSessionId);

        var task1 = client.PostAsJsonAsync("/api/pinyin/tone-drills", request, JsonDefaults.Options);
        var task2 = client.PostAsJsonAsync("/api/pinyin/tone-drills", request, JsonDefaults.Options);
        var responses = await Task.WhenAll(task1, task2);

        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.OK);
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1, "chỉ một trong hai request được TẠO MỚI phiên");
        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1, "request thua cuộc đua phải đọc lại phiên đã tạo, không nhân đôi");

        var bodies = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<SubmitResponseDto>(JsonDefaults.Options)));
        bodies[0]!.Id.Should().Be(bodies[1]!.Id);

        await using var db = TestDbContextFactory.Create();
        var sessionCount = await db.ToneDrillSessions.CountAsync(s => s.ClientSessionId == clientSessionId);
        sessionCount.Should().Be(1);
        var answerCount = await db.ToneDrillAnswers.CountAsync(a => a.SessionId == bodies[0]!.Id);
        answerCount.Should().Be(20); // không nhân đôi answers
    }

    [DbFact]
    public async Task PostToneDrills_AmTietKhongTonTai_Tra422UnknownSyllable()
    {
        var client = LearnerClient();
        var now = _factory.TimeProvider.GetUtcNow().UtcDateTime;
        var request = new SubmitRequestDto(
            Guid.NewGuid(), "listen_tone", now.AddMinutes(-1), now,
            [new ItemRequestDto([new PartRequestDto("xx", "妈", 1, 1)], 1500, 0)]);

        var response = await client.PostAsJsonAsync("/api/pinyin/tone-drills", request, JsonDefaults.Options);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("UNKNOWN_SYLLABLE");
    }

    [DbFact]
    public async Task PostToneDrills_ThanhKhongCoChuMinhHoa_Tra422ToneNotAvailable()
    {
        var client = LearnerClient();
        var now = _factory.TimeProvider.GetUtcNow().UtcDateTime;
        // "ma" chỉ có chữ minh hoạ cho thanh 1 (妈) và 3 (马) trong học liệu thật — thanh 2 không có.
        var request = new SubmitRequestDto(
            Guid.NewGuid(), "listen_tone", now.AddMinutes(-1), now,
            [new ItemRequestDto([new PartRequestDto("ma", "妈", 2, 2)], 1500, 0)]);

        var response = await client.PostAsJsonAsync("/api/pinyin/tone-drills", request, JsonDefaults.Options);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("TONE_NOT_AVAILABLE");
    }

    [DbFact]
    public async Task PostToneDrills_FinishedAtQuaTuongLai_Tra422InvalidSessionTime()
    {
        var client = LearnerClient();
        var now = _factory.TimeProvider.GetUtcNow().UtcDateTime;
        var request = BuildValidListenToneRequest(Guid.NewGuid()) with
        {
            StartedAt = now.AddMinutes(-1),
            FinishedAt = now.AddMinutes(10) // > now + 5 phút (R5-11)
        };

        var response = await client.PostAsJsonAsync("/api/pinyin/tone-drills", request, JsonDefaults.Options);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("INVALID_SESSION_TIME");
    }

    /// <summary>Đồng hồ giả 06:30 sáng giờ VN (23:30 UTC hôm trước) — tiêu chí HĐG §7 mục 9.</summary>
    [DbFact]
    public async Task PostToneDrills_LucBinhMinhGioVN_LocalDateLaNgayVN()
    {
        _factory.TimeProvider.SetUtcNow(new DateTimeOffset(2026, 9, 16, 23, 35, 0, TimeSpan.Zero));
        var client = LearnerClient(); // mặc định timeZone claim = Asia/Ho_Chi_Minh (TestTokenFactory)
        var finishedAt = new DateTime(2026, 9, 16, 23, 30, 0, DateTimeKind.Utc);
        var request = BuildValidListenToneRequest(Guid.NewGuid()) with
        {
            StartedAt = finishedAt.AddMinutes(-5),
            FinishedAt = finishedAt
        };

        var response = await client.PostAsJsonAsync("/api/pinyin/tone-drills", request, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SubmitResponseDto>(JsonDefaults.Options);
        body!.LocalDate.Should().Be(new DateOnly(2026, 9, 17));

        await using var db = TestDbContextFactory.Create();
        var studyEvent = await db.StudyEvents.SingleAsync(e => e.RefId == body.Id);
        studyEvent.LocalDate.Should().Be(new DateOnly(2026, 9, 17));
    }

    [DbFact]
    public async Task GetToneStats_Sau2Phien_KhopSoDem()
    {
        var client = LearnerClient();
        await client.PostAsJsonAsync("/api/pinyin/tone-drills", BuildValidListenToneRequest(Guid.NewGuid()), JsonDefaults.Options);
        await client.PostAsJsonAsync("/api/pinyin/tone-drills", BuildValidListenToneRequest(Guid.NewGuid()), JsonDefaults.Options);

        var response = await client.GetAsync("/api/pinyin/tone-stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<ToneStatsDto>(JsonDefaults.Options);
        stats!.SessionsCount.Should().Be(2);
        stats.TotalAnswered.Should().Be(40); // 2 phiên × 20 phần (listen_tone: 1 phần/câu)
        stats.ByTone.Values.Sum(t => t.Total).Should().Be(40);
        foreach (var tone in stats.ByTone.Values)
            tone.Total.Should().Be(10); // 5 câu/thanh/phiên × 2 phiên
    }

    [DbFact]
    public async Task GetChart_HocLieuKhongKhaDung_Tra503VaHealthLiveVan200()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), "af-pinyin-empty-" + Guid.NewGuid());
        Directory.CreateDirectory(emptyDir);

        // Program.cs đọc IConfiguration TRƯỚC khi WebApplicationFactory có cơ hội can thiệp qua
        // ConfigureWebHost/UseSetting (top-level statements gọi WebApplication.CreateBuilder(args)
        // rồi Build() ngay) — CHỈ biến môi trường đặt TRƯỚC lúc host build mới có tác dụng (cùng
        // bẫy đã ghi ở ChineseApiFactory/ChineseDbApiFactory, §9.2). Khôi phục NGAY sau khi
        // CreateClient() (build host xong, catalog Singleton đã cache trong host RIÊNG này) để
        // không ảnh hưởng factory dùng chung `_factory` của các test khác (biến process-wide).
        var previousRoot = Environment.GetEnvironmentVariable("Content__RootPath");
        Environment.SetEnvironmentVariable("Content__RootPath", emptyDir);

        await using var badFactory = new ContentUnavailableFactory(_factory.TokenFactory);
        HttpClient client;
        try
        {
            client = badFactory.CreateClient();
        }
        finally
        {
            Environment.SetEnvironmentVariable("Content__RootPath", previousRoot);
        }

        client.DefaultRequestHeaders.Authorization = Bearer(_factory.TokenFactory.CreateToken(Guid.NewGuid()));

        var chartResponse = await client.GetAsync("/api/pinyin/chart");
        chartResponse.StatusCode.Should().Be((HttpStatusCode)503);
        var body = await chartResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("CONTENT_UNAVAILABLE");

        var health = await client.GetAsync("/health/live");
        health.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private HttpClient LearnerClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(_factory.TokenFactory.CreateToken(Guid.NewGuid()));
        return client;
    }

    private SubmitRequestDto BuildValidListenToneRequest(Guid clientSessionId)
    {
        var now = _factory.TimeProvider.GetUtcNow().UtcDateTime;

        // Chữ minh hoạ THẬT từ content/chinese/data/pinyin/syllables.json — 5 câu mỗi thanh (R5-8).
        (string Syllable, string Hanzi, int Tone)[] examples =
        [
            ("an", "安", 1), ("ba", "巴", 1), ("ban", "斑", 1), ("bao", "包", 1), ("bian", "编", 1),
            ("bai", "白", 2), ("bi", "鼻", 2), ("bo", "博", 2), ("cai", "财", 2), ("ceng", "层", 2),
            ("bai", "百", 3), ("bao", "保", 3), ("bei", "北", 3), ("ben", "本", 3), ("biao", "表", 3),
            ("ai", "爱", 4), ("an", "案", 4), ("ao", "奥", 4), ("ba", "爸", 4), ("bai", "败", 4)
        ];

        var items = examples
            .Select(e => new ItemRequestDto([new PartRequestDto(e.Syllable, e.Hanzi, e.Tone, e.Tone)], 1500, 0))
            .ToList();

        return new SubmitRequestDto(clientSessionId, "listen_tone", now.AddMinutes(-5), now, items);
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private static string ResolveRealContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "content", "chinese");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        // RK37: không thấy content/chinese (vd chạy trong ảnh Docker không mang theo học liệu) —
        // giữ nguyên mặc định tương đối của appsettings.json (catalog sẽ IsAvailable=false, các
        // test dựa vào học liệu thật sẽ đỏ RÕ RÀNG thay vì âm thầm bỏ qua).
        return "content/chinese";
    }

    private sealed record PartRequestDto(string Syllable, string Hanzi, int ExpectedTone, int AnsweredTone);
    private sealed record ItemRequestDto(List<PartRequestDto> Parts, int? ResponseMs, int ReplayCount);
    private sealed record SubmitRequestDto(Guid ClientSessionId, string Mode, DateTime StartedAt, DateTime FinishedAt, List<ItemRequestDto> Items);

    private sealed record ToneCountDto(int Total, int Correct);
    private sealed record SubmitResponseDto(Guid Id, Guid ClientSessionId, string Mode, int Total, int Correct, DateOnly LocalDate, Dictionary<string, ToneCountDto> ByTone);

    private sealed record SyllableDto(string Syllable, string Initial, string Final, Dictionary<string, object> Tones);
    private sealed record ChartDto(string Version, List<object> Initials, List<object> Finals, List<SyllableDto> Syllables);

    private sealed record ToneStatDto(int Total, int Correct, double? Accuracy);
    private sealed record ToneStatsDto(
        int TotalAnswered, int SessionsCount, DateTime? LastSessionAt, int WindowSize, double? Accuracy,
        Dictionary<string, ToneStatDto> ByTone, List<object> Confusions, List<int> RecommendedFocus, bool G0Reached);

    private sealed record ErrorDto(string Error, string Code);

    /// <summary>
    /// Factory RIÊNG (không dùng chung <see cref="ChineseDbApiFactory"/>) chỉ để dựng một host MỚI
    /// đọc <c>Content__RootPath</c> hiện tại của biến môi trường (thư mục rỗng, đặt NGAY trước khi
    /// gọi <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}.CreateClient()"/>)
    /// — DB/Auth vẫn dùng các biến môi trường process-wide mà <see cref="ChineseDbApiFactory"/> (dùng
    /// chung, đã chạy constructor trước lớp test này) đã thiết lập.
    /// </summary>
    private sealed class ContentUnavailableFactory(TestTokenFactory tokenFactory) : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
                {
                    var configuration = new OpenIdConnectConfiguration { Issuer = ChineseDbApiFactory.Issuer };
                    configuration.SigningKeys.Add(tokenFactory.PublicKey);
                    o.Configuration = configuration;
                    o.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                });
            });
        }
    }
}
