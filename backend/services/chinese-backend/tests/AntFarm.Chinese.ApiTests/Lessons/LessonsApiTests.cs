using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Chinese.Application.Lessons;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Lessons;
using AntFarm.Chinese.Domain.Srs;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Lessons;

/// <summary>§5.2.1.6 test 3–10 — API học viên trên bài seed thật <c>chao-hoi</c> (14 từ, 7 câu quiz).</summary>
[Collection(ChineseApiCollection.Name)]
public class LessonsApiTests
{
    private const string LessonSlug = "chao-hoi";

    public LessonsApiTests() => Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());

    [DbFact]
    public async Task DanhSachVaChiTiet_ChiThayPublished_KhongLoDapAn()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());

        var draftSlug = await InsertDraftLessonAsync();

        var listResponse = await client.GetAsync("/api/lessons");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<LessonListResponseTestDto>(JsonDefaults.Options);
        list!.Items.Should().Contain(i => i.Slug == LessonSlug);
        list.Items.Should().NotContain(i => i.Slug == draftSlug);

        (await client.GetAsync($"/api/lessons/{draftSlug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var detailResponse = await client.GetAsync($"/api/lessons/{LessonSlug}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rawBody = await detailResponse.Content.ReadAsStringAsync();
        rawBody.Should().NotContain("correctOptionId"); // R-LS10 (unambiguous — không trùng tên trường nào khác)

        // R-LS10: "explanation"/"key" của quiz KHÔNG được lộ — kiểm TRONG mảng "quiz" cụ thể (không
        // dùng substring trên cả body vì "explanation" cũng là tên trường HỢP LỆ của khối grammar).
        using (var doc = JsonDocument.Parse(rawBody))
        {
            foreach (var question in doc.RootElement.GetProperty("quiz").EnumerateArray())
            {
                question.TryGetProperty("correctOptionId", out _).Should().BeFalse();
                question.TryGetProperty("explanation", out _).Should().BeFalse();
                question.TryGetProperty("key", out _).Should().BeFalse();
            }
        }

        var detail = await detailResponse.Content.ReadFromJsonAsync<LessonDetailTestDto>(JsonDefaults.Options);
        detail!.Words.Should().HaveCount(14);
        detail.Quiz.Should().HaveCount(7);
    }

    [DbFact]
    public async Task BatDauBai_Idempotent()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());
        var lessonId = await GetLessonIdAsync(client, LessonSlug);

        var first = await client.PostAsync($"/api/lessons/{lessonId}/start", content: null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstDto = await first.Content.ReadFromJsonAsync<ProgressTestDto>(JsonDefaults.Options);
        firstDto!.Status.Should().Be("in_progress");

        var second = await client.PostAsync($"/api/lessons/{lessonId}/start", content: null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondDto = await second.Content.ReadFromJsonAsync<ProgressTestDto>(JsonDefaults.Options);
        secondDto!.StartedAt.Should().Be(firstDto.StartedAt); // idempotent — không tạo dòng mới
    }

    [DbFact]
    public async Task NopDungHet_HoanThanhLanDau_ThemTheSrs_GhiStudyEvents()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        var lessonId = await GetLessonIdAsync(client, LessonSlug);
        var wordIds = await GetLessonWordIdsAsync(lessonId);
        var answers = await GetFullCorrectAnswersAsync(lessonId);

        var response = await SubmitAsync(client, lessonId, Guid.NewGuid(), answers);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = (await response.Content.ReadFromJsonAsync<SubmitResponseTestDto>(JsonDefaults.Options))!;
        result.Total.Should().Be(answers.Count);
        result.Correct.Should().Be(answers.Count);
        result.ScorePercent.Should().Be(100);
        result.Passed.Should().BeTrue();
        result.PassThresholdPercent.Should().Be(80);
        result.FirstCompletion.Should().BeTrue();
        result.SrsCardsAdded.Should().Be(wordIds.Count);
        result.Progress.Status.Should().Be("completed");

        await using var db = TestDbContextFactory.Create();
        var cardSources = await db.SrsCards.AsNoTracking()
            .Where(c => c.UserId == userId && wordIds.Contains(c.WordId))
            .Select(c => c.Source)
            .ToListAsync();
        cardSources.Should().HaveCount(wordIds.Count);
        cardSources.Should().AllSatisfy(s => s.Should().Be(SrsCardSources.Lesson));

        var quizSubmitEvents = await db.StudyEvents.AsNoTracking().Where(e => e.UserId == userId && e.Kind == StudyEventKinds.QuizSubmit).ToListAsync();
        quizSubmitEvents.Should().ContainSingle();
        quizSubmitEvents[0].Quantity.Should().Be(answers.Count);
        quizSubmitEvents[0].Correct.Should().Be(answers.Count);

        (await db.StudyEvents.AsNoTracking().CountAsync(e => e.UserId == userId && e.Kind == StudyEventKinds.LessonComplete)).Should().Be(1);
    }

    [DbFact]
    public async Task NopLaiVoiClientAttemptIdMoi_KhongTinhFirstCompletionNua_KhongThemThe()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        var lessonId = await GetLessonIdAsync(client, LessonSlug);
        var answers = await GetFullCorrectAnswersAsync(lessonId);

        (await SubmitAsync(client, lessonId, Guid.NewGuid(), answers)).StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await SubmitAsync(client, lessonId, Guid.NewGuid(), answers);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = (await second.Content.ReadFromJsonAsync<SubmitResponseTestDto>(JsonDefaults.Options))!;
        result.FirstCompletion.Should().BeFalse();
        result.SrsCardsAdded.Should().Be(0);

        await using var db = TestDbContextFactory.Create();
        (await db.StudyEvents.CountAsync(e => e.UserId == userId && e.Kind == StudyEventKinds.LessonComplete)).Should().Be(1);
        (await db.StudyEvents.CountAsync(e => e.UserId == userId && e.Kind == StudyEventKinds.QuizSubmit)).Should().Be(2);
    }

    [DbFact]
    public async Task GuiLaiDungClientAttemptId_TraLaiKetQuaCu_KhongGhiThem()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        var lessonId = await GetLessonIdAsync(client, LessonSlug);
        var answers = await GetFullCorrectAnswersAsync(lessonId);
        var clientAttemptId = Guid.NewGuid();

        var first = await SubmitAsync(client, lessonId, clientAttemptId, answers);
        var firstResult = (await first.Content.ReadFromJsonAsync<SubmitResponseTestDto>(JsonDefaults.Options))!;

        await using var dbBefore = TestDbContextFactory.Create();
        var countBefore = await dbBefore.QuizAttempts.CountAsync();

        var replay = await SubmitAsync(client, lessonId, clientAttemptId, answers);
        replay.StatusCode.Should().Be(HttpStatusCode.OK); // KHÔNG phải 201 lần hai
        var replayResult = (await replay.Content.ReadFromJsonAsync<SubmitResponseTestDto>(JsonDefaults.Options))!;
        replayResult.AttemptId.Should().Be(firstResult.AttemptId);

        await using var dbAfter = TestDbContextFactory.Create();
        (await dbAfter.QuizAttempts.CountAsync()).Should().Be(countBefore);
    }

    [DbFact]
    public async Task ThieuMotCau_QuizChanged_OptionIdSai_400()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());
        var lessonId = await GetLessonIdAsync(client, LessonSlug);
        var answers = await GetFullCorrectAnswersAsync(lessonId);

        var missingOne = answers.Skip(1).ToList();
        var missingResponse = await SubmitAsync(client, lessonId, Guid.NewGuid(), missingOne);
        missingResponse.StatusCode.Should().Be((HttpStatusCode)422);
        var missingBody = await missingResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        missingBody!.Code.Should().Be("QUIZ_CHANGED");

        var badOption = answers.Select((a, i) => i == 0 ? a with { OptionId = "z" } : a).ToList();
        var badOptionResponse = await SubmitAsync(client, lessonId, Guid.NewGuid(), badOption);
        badOptionResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var badOptionBody = await badOptionResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        badOptionBody!.Code.Should().Be("VALIDATION");
    }

    /// <summary>Review F9 — 'd' đúng định dạng <c>^[a-d]$</c> (qua được FluentValidation) nhưng câu này chỉ có 3 lựa chọn (a,b,c) ⇒ vẫn phải 400 <c>VALIDATION</c> ở tầng Application (kiểm optionId thuộc ĐÚNG câu, không chỉ đúng hình dạng chung).</summary>
    [DbFact]
    public async Task OptionIdDungHinhDangNhungKhongThuocCau_Tra400()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());
        var lessonId = await GetLessonIdAsync(client, LessonSlug);
        var answers = await GetFullCorrectAnswersAsync(lessonId);

        await using var db = TestDbContextFactory.Create();
        var questions = await db.QuizQuestions.AsNoTracking().Where(q => q.LessonId == lessonId).ToListAsync();
        var threeOptionQuestionId = questions
            .Select(q => (q.Id, Options: LessonJson.Deserialize<List<QuizOption>>(q.Options) ?? []))
            .First(x => x.Options.Count == 3).Id;

        var badAnswers = answers.Select(a => a.QuestionId == threeOptionQuestionId ? a with { OptionId = "d" } : a).ToList();

        var response = await SubmitAsync(client, lessonId, Guid.NewGuid(), badAnswers);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("VALIDATION");
    }

    /// <summary>Review F9 — <c>clientAttemptId</c> trùng nhưng người dùng KHÁC ⇒ 409, KHÔNG âm thầm trả kết quả của người kia.</summary>
    [DbFact]
    public async Task ClientAttemptIdTrungNguoiDungKhac_Tra409()
    {
        using var factory = new ChineseDbApiFactory();
        var clientA = ClientFor(factory, Guid.NewGuid());
        var clientB = ClientFor(factory, Guid.NewGuid());
        var lessonId = await GetLessonIdAsync(clientA, LessonSlug);
        var answers = await GetFullCorrectAnswersAsync(lessonId);
        var clientAttemptId = Guid.NewGuid();

        (await SubmitAsync(clientA, lessonId, clientAttemptId, answers)).StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await SubmitAsync(clientB, lessonId, clientAttemptId, answers);
        response.StatusCode.Should().Be((HttpStatusCode)409);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("DUPLICATE_ATTEMPT_ID");
    }

    /// <summary>Review F9 — <c>clientAttemptId</c> trùng nhưng gửi lên URL BÀI KHÁC ⇒ 409 (KHÔNG âm thầm trả kết quả của bài kia).</summary>
    [DbFact]
    public async Task ClientAttemptIdTrungBaiKhac_Tra409()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());
        var lessonId1 = await GetLessonIdAsync(client, LessonSlug);
        var lessonId2 = await GetLessonIdAsync(client, "ban-than");
        var answers1 = await GetFullCorrectAnswersAsync(lessonId1);
        var answers2 = await GetFullCorrectAnswersAsync(lessonId2);
        var clientAttemptId = Guid.NewGuid();

        (await SubmitAsync(client, lessonId1, clientAttemptId, answers1)).StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await SubmitAsync(client, lessonId2, clientAttemptId, answers2);
        response.StatusCode.Should().Be((HttpStatusCode)409);
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        body!.Code.Should().Be("DUPLICATE_ATTEMPT_ID");
    }

    /// <summary>CHẶN (review F9) — <c>startedAt</c> với offset khác "Z" (đồng hồ máy khách giờ Việt Nam, +07:00) KHÔNG được ném 500 (Kind=Local trước đây Npgsql từ chối) — phải 201 bình thường.</summary>
    [DbFact]
    public async Task NopVoiStartedAtOffsetDuong7_KhongNem500_Tra201()
    {
        using var factory = new ChineseDbApiFactory();
        var client = ClientFor(factory, Guid.NewGuid());
        var lessonId = await GetLessonIdAsync(client, LessonSlug);
        var answers = await GetFullCorrectAnswersAsync(lessonId);

        // Cùng một thời điểm UTC, chỉ đổi cách BIỂU DIỄN sang offset +07:00 (không phải "Z") — đúng
        // dạng đồng hồ máy khách ở Việt Nam gửi lên qua System.Text.Json.
        var startedAt = new DateTimeOffset(DateTime.UtcNow.AddMinutes(-5)).ToOffset(TimeSpan.FromHours(7));

        var response = await SubmitAsync(client, lessonId, Guid.NewGuid(), answers, startedAt);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [DbFact]
    public async Task TuDaCoTheManual_SauHoanThanh_TheVanManual_SrsAddedTruDi1()
    {
        using var factory = new ChineseDbApiFactory();
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId);
        var lessonId = await GetLessonIdAsync(client, LessonSlug);
        var wordIds = await GetLessonWordIdsAsync(lessonId);
        var answers = await GetFullCorrectAnswersAsync(lessonId);

        var manualWordId = wordIds[0];
        var addManual = await client.PostAsJsonAsync("/api/srs/cards", new { wordIds = new[] { manualWordId } }, JsonDefaults.Options);
        addManual.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await SubmitAsync(client, lessonId, Guid.NewGuid(), answers);
        var result = (await response.Content.ReadFromJsonAsync<SubmitResponseTestDto>(JsonDefaults.Options))!;
        result.SrsCardsAdded.Should().Be(wordIds.Count - 1);

        await using var db = TestDbContextFactory.Create();
        var manualCardSource = await db.SrsCards.AsNoTracking()
            .Where(c => c.UserId == userId && c.WordId == manualWordId)
            .Select(c => c.Source)
            .SingleAsync();
        manualCardSource.Should().Be(SrsCardSources.Manual); // KHÔNG bị đổi thành lesson
    }

    [DbFact]
    public async Task NopQuaNuaDemGioVietNam_LocalDateTinhTheoMuiGioNguoiDung()
    {
        using var factory = new ChineseDbApiFactory();
        factory.TimeProvider.AdjustTime(new DateTimeOffset(2026, 9, 17, 17, 30, 0, TimeSpan.Zero)); // 00:30 ngày 18/09 giờ VN
        var userId = Guid.NewGuid();
        var client = ClientFor(factory, userId); // TestTokenFactory mặc định timeZone=Asia/Ho_Chi_Minh
        var lessonId = await GetLessonIdAsync(client, LessonSlug);
        var answers = await GetFullCorrectAnswersAsync(lessonId);

        (await SubmitAsync(client, lessonId, Guid.NewGuid(), answers)).StatusCode.Should().Be(HttpStatusCode.Created);

        await using var db = TestDbContextFactory.Create();
        var quizSubmitEvent = await db.StudyEvents.AsNoTracking()
            .SingleAsync(e => e.UserId == userId && e.Kind == StudyEventKinds.QuizSubmit);
        quizSubmitEvent.LocalDate.Should().Be(new DateOnly(2026, 9, 18));
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

        (await client.GetAsync("/api/lessons")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- tiện ích ----

    private static HttpClient ClientFor(ChineseDbApiFactory factory, Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenFactory.CreateToken(userId));
        return client;
    }

    private static async Task<Guid> GetLessonIdAsync(HttpClient client, string slug)
    {
        var response = await client.GetAsync($"/api/lessons/{slug}");
        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<LessonDetailTestDto>(JsonDefaults.Options);
        return detail!.Id;
    }

    private static async Task<List<Guid>> GetLessonWordIdsAsync(Guid lessonId)
    {
        await using var db = TestDbContextFactory.Create();
        return await db.LessonWords.AsNoTracking().Where(w => w.LessonId == lessonId).Select(w => w.WordId).ToListAsync();
    }

    private static async Task<List<SubmitAnswerTestDto>> GetFullCorrectAnswersAsync(Guid lessonId)
    {
        await using var db = TestDbContextFactory.Create();
        var questions = await db.QuizQuestions.AsNoTracking()
            .Where(q => q.LessonId == lessonId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
        return [.. questions.Select(q => new SubmitAnswerTestDto(q.Id, q.CorrectOptionId))];
    }

    private static Task<HttpResponseMessage> SubmitAsync(
        HttpClient client, Guid lessonId, Guid clientAttemptId, List<SubmitAnswerTestDto> answers, DateTimeOffset? startedAt = null) =>
        client.PostAsJsonAsync(
            $"/api/lessons/{lessonId}/quiz-attempts",
            new SubmitRequestTestDto(clientAttemptId, startedAt ?? DateTimeOffset.UtcNow.AddMinutes(-2), answers),
            JsonDefaults.Options);

    /// <summary>Chèn thẳng một bài <c>draft</c> (KHÔNG qua file/importer) để kiểm "học viên không thấy bài chưa xuất bản" mà không đụng slug của <c>LessonImporterTests</c>.</summary>
    private static async Task<string> InsertDraftLessonAsync()
    {
        var slug = $"kiem-thu-nhap-{Guid.NewGuid():N}"[..24];
        await using var db = TestDbContextFactory.Create();
        var now = DateTime.UtcNow;
        var data = new LessonImportData(slug, "Bài nháp", "kiem-thu", "hsk1", 9000, "Bài nháp cho kiểm thử.", ["Kiểm thử"], 5, "[]", LessonStatuses.Draft);
        var lesson = Lesson.CreateSeed(data, new string('0', 64), now);
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();
        return slug;
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

    private sealed record ProgressTestDto(string Status, int? BestScorePercent, int AttemptsCount, DateTime StartedAt, DateTime? CompletedAt);
    private sealed record LessonListItemTestDto(Guid Id, string Slug, string Title, int WordCount, int QuestionCount, ProgressTestDto? Progress);
    private sealed record LessonListResponseTestDto(List<LessonListItemTestDto> Items, string? NextLessonSlug);
    private sealed record LessonWordTestDto(Guid Id, string Simplified, string Pinyin);
    private sealed record QuizOptionTestDto(string Id, string Text, string Lang);
    private sealed record QuizQuestionTestDto(Guid Id, string Type, string Prompt, List<QuizOptionTestDto> Options);
    private sealed record LessonDetailTestDto(Guid Id, string Slug, string Title, List<LessonWordTestDto> Words, List<QuizQuestionTestDto> Quiz, ProgressTestDto? Progress);
    private sealed record SubmitAnswerTestDto(Guid QuestionId, string OptionId);
    private sealed record SubmitRequestTestDto(Guid ClientAttemptId, DateTimeOffset? StartedAt, List<SubmitAnswerTestDto> Answers);
    private sealed record QuizResultItemTestDto(Guid QuestionId, string OptionId, bool Correct, string CorrectOptionId, string Explanation);
    private sealed record SubmitResponseTestDto(
        Guid AttemptId, DateTime SubmittedAt, int Total, int Correct, int ScorePercent, bool Passed,
        int PassThresholdPercent, bool FirstCompletion, int SrsCardsAdded, List<QuizResultItemTestDto> Results, ProgressTestDto Progress);
    private sealed record ErrorDto(string Error, string Code);
}
