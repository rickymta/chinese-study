using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Chinese.Domain.Learning;
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

namespace AntFarm.Chinese.ApiTests.Progress;

/// <summary>
/// <c>GET /api/progress/overview</c> (§5.2.4, §6.4, F11) — chuỗi ngày học theo múi giờ NGƯỜI DÙNG
/// (R-T3/R-PG2, không phải UTC), lịch hoạt động 90 ngày MỘT truy vấn, ẩn khối khi học liệu không
/// dùng được (R-PG7). Đặt <c>Content__RootPath</c> TUYỆT ĐỐI trỏ <c>content/chinese</c> của repo
/// TRƯỚC khi <see cref="ChineseDbApiFactory"/> build host (cùng kỹ thuật <c>PinyinApiTests</c>/
/// <c>WritingApiTests</c>/<c>SrsSummaryTests</c>, §9.2).
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class ProgressOverviewApiTests
{
    public ProgressOverviewApiTests() => Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());

    [DbFact]
    public async Task Gio2359VN_HomNayVanLaHomQuaTheoDuLieu_StreakTinhDungVaStudiedTodayTrue()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        // Mốc "bây giờ" ban đầu để provision user (time_zone=Asia/Ho_Chi_Minh mặc định của TestTokenFactory).
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 15, 0, 0, TimeSpan.Zero));
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var db = TestDbContextFactory.Create())
        {
            InsertStudyEvent(db, userId, new DateOnly(2026, 9, 16));
            InsertStudyEvent(db, userId, new DateOnly(2026, 9, 17));
            await db.SaveChangesAsync();
        }

        // 16:59Z = 23:59 giờ VN 17/09 — vẫn còn TRONG ngày 17/09 theo múi giờ người dùng.
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 16, 59, 0, TimeSpan.Zero));
        var overview = await GetOverviewAsync(client);

        overview.LocalDate.Should().Be(new DateOnly(2026, 9, 17));
        overview.Streak.Current.Should().Be(2);
        overview.Streak.Longest.Should().Be(2);
        overview.Streak.StudiedToday.Should().BeTrue();
    }

    [DbFact]
    public async Task Gio0001VN_SangNgayMoi_LocalDateDoiVaStudiedTodayFalse_NhungChuoiChuaDut()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 15, 0, 0, TimeSpan.Zero));
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var db = TestDbContextFactory.Create())
        {
            InsertStudyEvent(db, userId, new DateOnly(2026, 9, 16));
            InsertStudyEvent(db, userId, new DateOnly(2026, 9, 17));
            await db.SaveChangesAsync();
        }

        // 17:01Z = 00:01 giờ VN 18/09 — cắt ngày theo UTC (sai) sẽ vẫn ra 17/09; đúng phải là 18/09
        // (R-T3) — đây là ca bắt lỗi "dùng UTC để cắt ngày" (CLAUDE.md).
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 17, 1, 0, TimeSpan.Zero));
        var overview = await GetOverviewAsync(client);

        overview.LocalDate.Should().Be(new DateOnly(2026, 9, 18));
        overview.Streak.StudiedToday.Should().BeFalse();
        // Hôm nay (18/09) chưa học ⇒ đếm lùi từ hôm qua (17/09, 16/09) — R-PG3: chuỗi CHƯA đứt.
        overview.Streak.Current.Should().Be(2);
    }

    [DbFact]
    public async Task LichHoatDong_Dung90PhanTu_PhanTuCuoiLaHomNay_TongKhopDuLieu()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 15, 0, 0, TimeSpan.Zero));
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        var today = new DateOnly(2026, 9, 17);

        await using (var db = TestDbContextFactory.Create())
        {
            InsertStudyEvent(db, userId, today, quantity: 3); // hôm nay
            InsertStudyEvent(db, userId, today.AddDays(-1), quantity: 2); // hôm qua
            InsertStudyEvent(db, userId, today.AddDays(-89), quantity: 1); // đúng mép cửa sổ 90 ngày
            InsertStudyEvent(db, userId, today.AddDays(-90), quantity: 5); // NGOÀI cửa sổ — không được tính
            await db.SaveChangesAsync();
        }

        var overview = await GetOverviewAsync(client);

        overview.Activity.Should().HaveCount(90);
        overview.Activity[0].Date.Should().Be(today.AddDays(-89));
        overview.Activity[^1].Date.Should().Be(today);
        overview.Activity[^1].Count.Should().Be(3);
        overview.Activity[0].Count.Should().Be(1);
        overview.Activity.Should().BeInAscendingOrder(a => a.Date);
        overview.Activity.Sum(a => a.Count).Should().Be(3 + 2 + 1); // KHÔNG cộng dòng ngoài cửa sổ (-90 ngày)
        overview.Today.ActivityCount.Should().Be(3);
    }

    /// <summary>
    /// Cô lập dữ liệu giữa hai người dùng (review Opus F11) — mọi truy vấn của
    /// <c>ProgressOverviewService</c> đều lọc <c>WHERE user_id = @u</c>, nhưng test này KIỂM CHỨNG
    /// bằng dữ liệu thật thay vì đọc code: B có streak/lịch hoạt động LỚN HƠN A nhiều lần (dễ lộ
    /// nếu có bug quên lọc user_id, vd JOIN/GROUP BY thiếu điều kiện) — A phải KHÔNG thấy số của B
    /// và ngược lại.
    /// </summary>
    [DbFact]
    public async Task ColLapHaiNguoiDung_SuKienNguoiBKhongLotVaoStreakActivityTodayCuaNguoiA()
    {
        using var factory = new ChineseDbApiFactory();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var clientA = ClientFor(factory, userA);
        var clientB = ClientFor(factory, userB);

        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 15, 0, 0, TimeSpan.Zero));
        (await clientA.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await clientB.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        var today = new DateOnly(2026, 9, 17);

        await using (var db = TestDbContextFactory.Create())
        {
            // A: CHỈ một dòng hôm nay ⇒ streak = 1, activity hôm nay = 4.
            InsertStudyEvent(db, userA, today, quantity: 4);

            // B: chuỗi 3 ngày liên tiếp, số lượng LỚN HƠN A nhiều lần — nếu ProgressOverviewService
            // của A vô tình đọc lẫn dữ liệu của B (thiếu lọc user_id) thì streak/activity/today của
            // A sẽ phồng lên bất thường, dễ phát hiện hơn dùng số nhỏ ngẫu nhiên trùng lặp.
            InsertStudyEvent(db, userB, today, quantity: 50);
            InsertStudyEvent(db, userB, today.AddDays(-1), quantity: 40);
            InsertStudyEvent(db, userB, today.AddDays(-2), quantity: 30);

            await db.SaveChangesAsync();
        }

        var overviewA = await GetOverviewAsync(clientA);
        overviewA.Streak.Current.Should().Be(1); // KHÔNG phải 3 (chuỗi của B)
        overviewA.Streak.Longest.Should().Be(1);
        overviewA.Today.ActivityCount.Should().Be(4); // KHÔNG cộng 50 của B
        overviewA.Activity[^1].Count.Should().Be(4);
        overviewA.Activity.Sum(a => a.Count).Should().Be(4); // KHÔNG cộng 50+40+30 của B

        var overviewB = await GetOverviewAsync(clientB);
        overviewB.Streak.Current.Should().Be(3); // KHÔNG bị "kéo xuống" bởi A chỉ có 1 ngày
        overviewB.Streak.Longest.Should().Be(3);
        overviewB.Today.ActivityCount.Should().Be(50); // KHÔNG cộng 4 của A
        overviewB.Activity[^1].Count.Should().Be(50);
        overviewB.Activity.Sum(a => a.Count).Should().Be(50 + 40 + 30); // KHÔNG cộng 4 của A
    }

    [DbFact]
    public async Task NguoiDungMoi_KhongCoDuLieu_Tra200_MoiSoVeKhongVaKhoiKhongNull()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        var overview = await GetOverviewAsync(client);

        overview.Streak.Current.Should().Be(0);
        overview.Streak.Longest.Should().Be(0);
        overview.Streak.StudiedToday.Should().BeFalse();

        overview.Today.SrsReviews.Should().Be(0);
        overview.Today.ToneDrillItems.Should().Be(0);
        overview.Today.WritingAttempts.Should().Be(0);
        overview.Today.Quizzes.Should().Be(0);
        overview.Today.LessonsCompleted.Should().Be(0);
        overview.Today.ActivityCount.Should().Be(0);

        // Khối vẫn HIỆN DIỆN (R-PG7 "0 không phải ẩn") — chỉ số phản ánh KHẢ NĂNG (newAvailableToday,
        // totalInPath, totalChars, published) khác 0 vì lộ trình HSK1 + ít nhất 5 bài seed đã có sẵn
        // (DB test dùng CHUNG cho cả collection — các lớp test khác của F9/F10 có thể đã thêm bài
        // qua quản trị, KHÔNG dọn lại — so "published"/"next" với chính /api/lessons thay vì số cố
        // định, tránh phụ thuộc thứ tự chạy test khác).
        overview.Srs.Should().NotBeNull();
        overview.Srs!.DueToday.Should().Be(0);
        overview.Srs.ReviewedToday.Should().Be(0);
        overview.Srs.NewIntroducedToday.Should().Be(0);
        overview.Srs.NewAvailableToday.Should().BeGreaterThan(0);

        overview.DailyGoal.Should().NotBeNull();
        overview.DailyGoal!.Done.Should().Be(0);
        overview.DailyGoal.Achieved.Should().BeFalse(); // còn thẻ mới khả dụng ⇒ chưa đạt

        overview.Vocabulary.Should().NotBeNull();
        overview.Vocabulary!.Introduced.Should().Be(0);
        overview.Vocabulary.Learning.Should().Be(0);
        overview.Vocabulary.Mature.Should().Be(0);
        overview.Vocabulary.TotalInPath.Should().BeGreaterThan(0);

        // Đối chiếu với /api/lessons (F9, R-LS4) thay vì số cố định — DB test dùng chung cả collection.
        var lessonsList = (await (await client.GetAsync("/api/lessons")).Content.ReadFromJsonAsync<LessonsListDto>(JsonDefaults.Options))!;
        overview.Lessons.Should().NotBeNull();
        overview.Lessons!.Published.Should().Be(lessonsList.Items.Count);
        overview.Lessons.Published.Should().BeGreaterThanOrEqualTo(5); // ít nhất 5 bài seed thật (§5.4.6)
        overview.Lessons.Completed.Should().Be(0);
        overview.Lessons.InProgress.Should().Be(0);
        overview.Lessons.Next.Should().NotBeNull();
        overview.Lessons.Next!.Slug.Should().Be(lessonsList.NextLessonSlug);
        overview.Lessons.LastCompleted.Should().BeNull();

        overview.Writing.Should().NotBeNull();
        overview.Writing!.PracticedChars.Should().Be(0);
        overview.Writing.MasteredChars.Should().Be(0);
        overview.Writing.WeakChars.Should().Be(0);
        overview.Writing.TotalChars.Should().BeGreaterThan(0);

        overview.Tone.Should().NotBeNull();
        overview.Tone!.TotalAnswered.Should().Be(0);
        overview.Tone.Accuracy.Should().BeNull();
        overview.Tone.RecommendedFocus.Should().BeEmpty();

        overview.Activity.Should().HaveCount(90);
        overview.Activity.Should().OnlyContain(a => a.Count == 0);
    }

    [DbFact]
    public async Task HocLieuPinyinKhongDungDuoc_KhoiToneVangNhungCacKhoiKhacVanCo_Http200()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();

        var missingPinyinRoot = Path.Combine(Path.GetTempPath(), "af-progress-no-pinyin-" + Guid.NewGuid());
        Directory.CreateDirectory(missingPinyinRoot); // thư mục KHÔNG có data/pinyin ⇒ IPinyinCatalog.IsAvailable=false

        // Program.cs đọc IConfiguration TRƯỚC khi WebApplicationFactory can thiệp được (top-level
        // statements build host ngay) — biến môi trường chỉ có tác dụng nếu đặt TRƯỚC lúc host mới
        // build (cùng bẫy đã ghi ở PinyinApiTests.GetChart_HocLieuKhongKhaDung_Tra503VaHealthLiveVan200).
        var previousRoot = Environment.GetEnvironmentVariable("Content__RootPath");
        Environment.SetEnvironmentVariable("Content__RootPath", missingPinyinRoot);

        await using var pinyinUnavailableFactory = new AuthOnlyFactory(factory.TokenFactory);
        HttpClient client;
        try
        {
            client = pinyinUnavailableFactory.CreateClient();
        }
        finally
        {
            // Khôi phục NGAY sau khi host (Singleton IPinyinCatalog) đã build xong — không ảnh
            // hưởng các test khác dùng chung biến môi trường process-wide này.
            Environment.SetEnvironmentVariable("Content__RootPath", previousRoot);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenFactory.CreateToken(userId));

        var response = await client.GetAsync("/api/progress/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var overview = (await response.Content.ReadFromJsonAsync<OverviewDto>(JsonDefaults.Options))!;
        overview.Tone.Should().BeNull(); // R-PG7: khối ẩn, không lỗi

        // Các khối KHÔNG phụ thuộc tệp pinyin vẫn hiện diện bình thường.
        overview.Srs.Should().NotBeNull();
        overview.Vocabulary.Should().NotBeNull();
        overview.Lessons.Should().NotBeNull();
        overview.Writing.Should().NotBeNull();
    }

    [DbFact]
    public async Task KhongCoQuyenStudyUse_Tra403()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        (await client.GetAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.OK); // provision trước

        await using (var db = TestDbContextFactory.Create())
        {
            var roles = await db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            db.UserRoles.RemoveRange(roles);
            await db.SaveChangesAsync();
        }

        (await client.GetAsync("/api/progress/overview")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static void InsertStudyEvent(ChineseDbContext db, Guid userId, DateOnly localDate, int quantity = 1)
    {
        // Kind/occurredAt cụ thể không quan trọng cho streak/lịch hoạt động — chỉ cần local_date
        // và quantity > 0 (R-PG1); dùng "writing" làm đại diện trung tính.
        var occurredAt = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        db.StudyEvents.Add(StudyEvent.Create(userId, StudyEventKinds.Writing, occurredAt, localDate, quantity, null, null, occurredAt));
    }

    private static async Task<OverviewDto> GetOverviewAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/progress/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<OverviewDto>(JsonDefaults.Options))!;
    }

    private static HttpClient ClientFor(ChineseDbApiFactory factory, Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenFactory.CreateToken(userId));
        return client;
    }

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

        return "content/chinese";
    }

    /// <summary>Host RIÊNG chỉ để đổi <c>Content__RootPath</c> lúc build (Singleton <c>IPinyinCatalog</c> nạp lười lúc resolve đầu tiên) — dùng CHUNG DB <c>af_chinese_test</c> (biến môi trường connection string đã đặt process-wide bởi <see cref="ChineseDbApiFactory"/> tạo trước đó), cùng kỹ thuật <c>PinyinApiTests.ContentUnavailableFactory</c>.</summary>
    private sealed class AuthOnlyFactory(TestTokenFactory tokenFactory) : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>
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

    private sealed record StreakDto(int Current, int Longest, bool StudiedToday);
    private sealed record TodayDto(int SrsReviews, int NewCards, int ToneDrillItems, int WritingAttempts, int Quizzes, int LessonsCompleted, int ActivityCount);
    private sealed record SrsDto(int DueToday, int DueNow, int NewAvailableToday, int NewIntroducedToday, int ReviewedToday, DateTime? NextDueAt);
    private sealed record DailyGoalDto(int Done, int Total, bool Achieved);
    private sealed record VocabularyDto(int TotalInPath, int Introduced, int Learning, int Mature);
    private sealed record LessonRefDto(string Slug, string Title);
    private sealed record LastCompletedLessonDto(string Slug, string Title, DateTime CompletedAt, int UnpracticedChars);
    private sealed record LessonsDto(int Published, int Completed, int InProgress, LessonRefDto? Next, LastCompletedLessonDto? LastCompleted);
    private sealed record WritingDto(int PracticedChars, int MasteredChars, int WeakChars, int TotalChars);
    private sealed record ToneDto(int TotalAnswered, double? Accuracy, IReadOnlyList<int> RecommendedFocus);
    private sealed record ActivityDayDto(DateOnly Date, int Count);
    private sealed record LessonsListItemDto(string Slug);
    private sealed record LessonsListDto(List<LessonsListItemDto> Items, string? NextLessonSlug);

    private sealed record OverviewDto(
        DateOnly LocalDate, string TimeZone, StreakDto Streak, TodayDto Today,
        SrsDto? Srs, DailyGoalDto? DailyGoal, VocabularyDto? Vocabulary, LessonsDto? Lessons,
        WritingDto? Writing, ToneDto? Tone, IReadOnlyList<ActivityDayDto> Activity);
}
