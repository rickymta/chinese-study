using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Writing;

/// <summary>§5.2.2 test — API luyện viết trên kho học liệu THẬT (300 chữ HSK1, 5 bài seed, K10).</summary>
[Collection(ChineseApiCollection.Name)]
public class WritingApiTests
{
    private const string KnownHanzi = "你"; // có trong characters.json và trong từ của bài chao-hoi
    private const string UnknownHanzi = "龘"; // KHÔNG có trong characters.json

    // U+F967 là CHỮ TƯƠNG THÍCH (CJK Compatibility Ideograph) có phân rã CHÍNH TẮC (không đánh dấu
    // <compat>) về U+4E0D (不) — string.Normalize(FormC) của .NET đổi hẳn thành "不" (đã kiểm chứng
    // thủ công 17/09/2026). RecordWritingAttemptValidator/CjkCharacterValidation CHẤP NHẬN chuỗi này
    // (tự chuẩn hoá để kiểm khối CJK) nhưng nếu WritingService không chuẩn hoá trước khi tra/ghi thì
    // sẽ so trực tiếp với content.characters.hanzi (lưu dạng NFC = "不") và nhầm ra 422
    // UNKNOWN_CHARACTER dù về bản chất là CÙNG một chữ (review F8, bổ sung theo yêu cầu Opus).
    private const string CompatibilityHanzi = "不";
    private const string CompatibilityHanziNfc = "不";

    public WritingApiTests() => Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());

    [DbFact]
    public async Task GhiMotLanViet_Tra201_GhiStudyEvent()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        var response = await RecordAsync(client, Guid.NewGuid(), KnownHanzi, "recall", mistakes: 0, hints: 0);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = (await response.Content.ReadFromJsonAsync<RecordResponseTestDto>(JsonDefaults.Options))!;
        result.IsClean.Should().BeTrue();
        result.Stats.Attempts.Should().Be(1);
        result.Stats.MasteryStatus.Should().Be("practicing"); // 1 ngày sạch chưa đủ 2 ngày (R-W5)

        await using var db = TestDbContextFactory.Create();
        var events = await db.StudyEvents.AsNoTracking()
            .Where(e => e.UserId == userId && e.Kind == StudyEventKinds.Writing)
            .ToListAsync();
        events.Should().ContainSingle();
        events[0].Quantity.Should().Be(1);
        events[0].Correct.Should().Be(1); // recall sạch ⇒ 1 (R-W4)
    }

    [DbFact]
    public async Task GuiVoiModeGuided_CorrectLaNull()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        (await RecordAsync(client, Guid.NewGuid(), KnownHanzi, "guided", mistakes: 0, hints: 0))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        await using var db = TestDbContextFactory.Create();
        var writingEvent = await db.StudyEvents.AsNoTracking()
            .SingleAsync(e => e.UserId == userId && e.Kind == StudyEventKinds.Writing);
        writingEvent.Correct.Should().BeNull(); // R-W4: guided luôn NULL
    }

    [DbFact]
    public async Task GuiLaiDungClientAttemptId_Tra200_KhongThemDong()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());
        var clientAttemptId = Guid.NewGuid();

        var first = await RecordAsync(client, clientAttemptId, KnownHanzi, "recall", 0, 0);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstResult = (await first.Content.ReadFromJsonAsync<RecordResponseTestDto>(JsonDefaults.Options))!;

        await using var dbBefore = TestDbContextFactory.Create();
        var countBefore = await dbBefore.WritingAttempts.CountAsync();

        var replay = await RecordAsync(client, clientAttemptId, KnownHanzi, "recall", 0, 0);
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        var replayResult = (await replay.Content.ReadFromJsonAsync<RecordResponseTestDto>(JsonDefaults.Options))!;
        replayResult.AttemptId.Should().Be(firstResult.AttemptId);

        await using var dbAfter = TestDbContextFactory.Create();
        (await dbAfter.WritingAttempts.CountAsync()).Should().Be(countBefore);
    }

    [DbFact]
    public async Task ClientAttemptIdTrungNguoiDungKhac_Tra409()
    {
        using var factory = new ChineseDbApiFactory();
        var clientA = ClientFor(factory, Guid.NewGuid());
        var clientB = ClientFor(factory, Guid.NewGuid());
        var clientAttemptId = Guid.NewGuid();

        (await RecordAsync(clientA, clientAttemptId, KnownHanzi, "recall", 0, 0)).StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await RecordAsync(clientB, clientAttemptId, KnownHanzi, "recall", 0, 0);
        response.StatusCode.Should().Be((HttpStatusCode)409);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("DUPLICATE_ATTEMPT_ID");
    }

    [DbFact]
    public async Task HanziKhongTonTai_Tra422()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());

        var response = await RecordAsync(client, Guid.NewGuid(), UnknownHanzi, "recall", 0, 0);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("UNKNOWN_CHARACTER");
    }

    /// <summary>Review Opus 17/09/2026 — chữ tương thích (compatibility ideograph) chuẩn hoá NFC về đúng một chữ đã có trong kho ⇒ 201, KHÔNG 422 UNKNOWN_CHARACTER; dữ liệu lưu dưới dạng NFC (khớp content.characters.hanzi), không lưu nguyên chuỗi client gửi lên.</summary>
    [DbFact]
    public async Task HanziTuongThich_ChuanHoaNfc_KhopChuDaCo_Tra201()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        var response = await RecordAsync(client, Guid.NewGuid(), CompatibilityHanzi, "recall", 0, 0);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = (await response.Content.ReadFromJsonAsync<RecordResponseTestDto>(JsonDefaults.Options))!;
        result.Stats.Hanzi.Should().Be(CompatibilityHanziNfc);

        await using var db = TestDbContextFactory.Create();
        var attempt = await db.WritingAttempts.AsNoTracking().SingleAsync(a => a.UserId == userId);
        attempt.Hanzi.Should().Be(CompatibilityHanziNfc); // lưu dạng NFC, không lưu nguyên U+F967
    }

    [DbFact]
    public async Task DanhSachHsk1_SapTheoPathOrder()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());

        // Kho HSK1 có đủ 300 chữ (= characters.json) — tối đa mỗi trang là 200 (R-W6/§6.2) nên gộp
        // 2 trang để kiểm THỨ TỰ trên TOÀN BỘ danh sách, không chỉ "có mặt".
        var page1 = await GetCharacterListAsync(client, "hsk1", page: 1, pageSize: 200);
        var page2 = await GetCharacterListAsync(client, "hsk1", page: 2, pageSize: 200);
        var allHanzi = page1.Items.Concat(page2.Items).Select(i => i.Hanzi).ToList();

        page1.Set.Should().Be("hsk1");
        page1.TotalCount.Should().Be(300);
        allHanzi.Should().HaveCount(300);
        page1.Items.Should().AllSatisfy(i => i.MasteryStatus.Should().Be("new"));

        // 你 thuộc từ có path_order=5 (bài chào hỏi); 年/半 thuộc từ path_order ~497 (bài thời gian) —
        // phải đứng SAU 你 rất xa nếu sắp đúng theo min(path_order) rồi hanzi (R-W6).
        allHanzi.IndexOf(KnownHanzi).Should().BeLessThan(allHanzi.IndexOf("年"));
        allHanzi.IndexOf(KnownHanzi).Should().BeLessThan(allHanzi.IndexOf("半"));
    }

    [DbFact]
    public async Task DanhSachTheoBai_ChiChuCuaBai_KhongTrung()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());

        var response = await client.GetAsync("/api/writing/characters?set=lesson:chao-hoi&pageSize=200");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = (await response.Content.ReadFromJsonAsync<ListResponseTestDto>(JsonDefaults.Options))!;

        list.Items.Should().NotBeEmpty();
        list.Items.Select(i => i.Hanzi).Should().OnlyHaveUniqueItems();
        list.Items.Select(i => i.Hanzi).Should().Contain(KnownHanzi); // 你 thuộc từ 你 của bài chào hỏi
    }

    [DbFact]
    public async Task BaiKhongTonTai_Tra404()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());

        var response = await client.GetAsync("/api/writing/characters?set=lesson:khong-ton-tai");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [DbFact]
    public async Task SetSai_Tra400()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());

        var response = await client.GetAsync("/api/writing/characters?set=abc");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("VALIDATION");
    }

    [DbFact]
    public async Task DanhSachCanLuyen_ChiChuThoaRW5()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        // Chữ 1: gõ tệ (2 lỗi) ⇒ weak. Chữ 2: sạch ⇒ không weak.
        (await RecordAsync(client, Guid.NewGuid(), "你", "recall", mistakes: 2, hints: 0)).EnsureSuccessStatusCode();
        (await RecordAsync(client, Guid.NewGuid(), "好", "recall", mistakes: 0, hints: 0)).EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/writing/characters?set=weak");
        var list = (await response.Content.ReadFromJsonAsync<ListResponseTestDto>(JsonDefaults.Options))!;

        list.Items.Select(i => i.Hanzi).Should().Contain("你");
        list.Items.Select(i => i.Hanzi).Should().NotContain("好");
    }

    [DbFact]
    public async Task ChiTietChu_TraThongTinVaTuChua()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());

        var response = await client.GetAsync($"/api/writing/characters/{Uri.EscapeDataString(KnownHanzi)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = (await response.Content.ReadFromJsonAsync<DetailResponseTestDto>(JsonDefaults.Options))!;

        detail.Hanzi.Should().Be(KnownHanzi);
        detail.Words.Should().NotBeEmpty();
        detail.Stats.Should().BeNull(); // chưa từng viết
    }

    [DbFact]
    public async Task TomTat_DemDungSoLieu()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);

        (await RecordAsync(client, Guid.NewGuid(), KnownHanzi, "recall", 0, 0)).EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/writing/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = (await response.Content.ReadFromJsonAsync<SummaryResponseTestDto>(JsonDefaults.Options))!;

        summary.PracticedChars.Should().Be(1);
        summary.AttemptsToday.Should().Be(1);
        summary.TotalChars.Should().BeGreaterThan(0);
    }

    /// <summary>R-W5 — hai lần TỰ VIẾT sạch ở hai ngày lịch KHÁC NHAU theo múi giờ Việt Nam (23:50 17/09 và 00:10 18/09 giờ VN, cùng khoảng UTC gần nhau) ⇒ <c>mastered</c>. Cắt theo UTC sẽ ra SAI (cùng một ngày UTC).</summary>
    [DbFact]
    public async Task HaiLanRecallSach_QuaNuaDemGioVietNam_ThanhMastered()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId); // TestTokenFactory mặc định timeZone=Asia/Ho_Chi_Minh

        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 16, 50, 0, TimeSpan.Zero)); // 23:50 giờ VN 17/09
        (await RecordAsync(client, Guid.NewGuid(), KnownHanzi, "recall", 0, 0)).StatusCode.Should().Be(HttpStatusCode.Created);

        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 17, 10, 0, TimeSpan.Zero)); // 00:10 giờ VN 18/09
        var second = await RecordAsync(client, Guid.NewGuid(), KnownHanzi, "recall", 0, 0);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = (await second.Content.ReadFromJsonAsync<RecordResponseTestDto>(JsonDefaults.Options))!;

        result.Stats.MasteryStatus.Should().Be("mastered");
        result.BecameMastered.Should().BeTrue();
    }

    [DbFact]
    public async Task NguoiDungKhongCoQuyen_Tra403()
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

        (await client.GetAsync("/api/writing/summary")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- tiện ích ----

    private static HttpClient ClientFor(ChineseDbApiFactory factory, Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenFactory.CreateToken(userId));
        return client;
    }

    private static async Task<ListResponseTestDto> GetCharacterListAsync(HttpClient client, string set, int page, int pageSize)
    {
        var response = await client.GetAsync($"/api/writing/characters?set={set}&page={page}&pageSize={pageSize}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ListResponseTestDto>(JsonDefaults.Options))!;
    }

    private static Task<HttpResponseMessage> RecordAsync(
        HttpClient client, Guid clientAttemptId, string hanzi, string mode, int mistakes, int hints) =>
        client.PostAsJsonAsync(
            "/api/writing/attempts",
            new RecordRequestTestDto(clientAttemptId, hanzi, mode, 10, mistakes, hints, 5000),
            JsonDefaults.Options);

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

    private sealed record RecordRequestTestDto(Guid ClientAttemptId, string Hanzi, string Mode, int TotalStrokes, int TotalMistakes, int HintsUsed, int? DurationMs);
    private sealed record StatsTestDto(string Hanzi, int Attempts, string MasteryStatus, bool IsWeak);
    private sealed record RecordResponseTestDto(Guid AttemptId, DateTime CompletedAt, bool IsClean, StatsTestDto Stats, bool BecameMastered);
    private sealed record ListItemTestDto(string Hanzi, string MasteryStatus);
    private sealed record ListResponseTestDto(string Set, List<ListItemTestDto> Items, int Page, int PageSize, int TotalCount);
    private sealed record DetailWordTestDto(Guid Id, string Simplified);
    private sealed record DetailResponseTestDto(string Hanzi, List<DetailWordTestDto> Words, StatsTestDto? Stats);
    private sealed record SummaryResponseTestDto(int PracticedChars, int MasteredChars, int WeakChars, int AttemptsToday, int TotalChars);
    private sealed record ErrorDto(string Error, string Code);
}
