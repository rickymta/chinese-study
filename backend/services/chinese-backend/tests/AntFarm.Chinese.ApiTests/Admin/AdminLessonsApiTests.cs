using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using AntFarm.Chinese.ApiTests.Infrastructure;
using AntFarm.Chinese.Domain.Lessons;
using AntFarm.Chinese.Infrastructure.Content;
using AntFarm.Chinese.Infrastructure.Persistence;
using AntFarm.Testing;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AntFarm.Chinese.ApiTests.Admin;

/// <summary>
/// §5.2.3/§6.3, test F10 mục 1–7, 11 — quản trị bài học. Dùng nội dung THẬT (<c>content/chinese</c>)
/// chỉ để LẤY sẵn từ vựng hợp lệ cho <c>PUT .../words</c> — KHÔNG BAO GIỜ đụng tới 5 bài seed thật
/// (archive/sửa/xoá) để không phá <c>RealLessonsTests</c>/<c>RealContentTests</c>: mọi kịch bản
/// "bài seed" ở đây dùng bài GIẢ LẬP chèn thẳng qua <see cref="Lesson.CreateSeed"/>.
/// </summary>
[Collection(ChineseApiCollection.Name)]
public class AdminLessonsApiTests
{
    [DbFact]
    public async Task HocVien_GoiRouteAdmin_Tra403()
    {
        using var factory = new ChineseDbApiFactory();
        var learner = LearnerClient(factory);

        (await learner.GetAsync("/api/admin/lessons")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await learner.PostAsJsonAsync("/api/admin/lessons", new { slug = "x", title = "x", topic = "x" }, JsonDefaults.Options))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DbFact]
    public async Task TaoBai_ThayKhoiTuQuiz_XuatBan_CoWarnings_HocVienThay()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);
        var slug = UniqueSlug("tao-bai");

        var lesson = await CreatePublishedLessonAsync(admin, slug);

        lesson.Status.Should().Be("published");
        lesson.PublishedAt.Should().NotBeNull();
        lesson.Warnings.Should().NotBeEmpty(); // < 5 câu, không có dialogue, 0% câu nghe

        var learner = LearnerClient(factory);
        var listResponse = await learner.GetAsync("/api/lessons");
        var list = await listResponse.Content.ReadFromJsonAsync<LessonListTestDto>(JsonDefaults.Options);
        list!.Items.Should().Contain(i => i.Slug == slug);
    }

    [DbFact]
    public async Task XuatBanBaiChiCoKhoi_422_ProblemsKhongRong()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);
        var slug = UniqueSlug("thieu-dieu-kien");

        var create = await admin.PostAsJsonAsync("/api/admin/lessons", new { slug, title = "Bài thiếu điều kiện", topic = "kiem-thu" }, JsonDefaults.Options);
        var created = (await create.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

        var blocksResponse = await admin.PutAsJsonAsync($"/api/admin/lessons/{created.Id}/blocks", new
        {
            version = created.Version,
            blocks = new[] { new { type = "text", payload = new { paragraphs = new[] { "Nội dung." } } } }
        }, JsonDefaults.Options);
        var afterBlocks = (await blocksResponse.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

        var publishResponse = await admin.PostAsJsonAsync($"/api/admin/lessons/{created.Id}/publish", new { version = afterBlocks.Version }, JsonDefaults.Options);
        publishResponse.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await publishResponse.Content.ReadFromJsonAsync<ErrorWithProblemsDto>(JsonDefaults.Options);
        error!.Code.Should().Be("LESSON_NOT_PUBLISHABLE");
        error.Details!.Problems.Should().NotBeEmpty();
    }

    [DbFact]
    public async Task HaiLanPutMetaCungVersion_Lan2Conflict_PutBlocksLamVersionDoi()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);
        var slug = UniqueSlug("concurrency");

        var create = await admin.PostAsJsonAsync("/api/admin/lessons", new { slug, title = "Bài concurrency", topic = "kiem-thu" }, JsonDefaults.Options);
        var created = (await create.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

        // ChineseDbApiFactory.TimeProvider là đồng hồ giả ĐỨNG YÊN trừ khi chỉnh tay — R-CA2 luôn
        // đặt updated_at/edited_at = "bây giờ", nhưng đồng hồ đứng yên thì giá trị mới TRÙNG giá trị
        // cũ ⇒ EF ChangeTracker không thấy gì đổi ⇒ KHÔNG issue UPDATE ⇒ xmin không đổi (chỉ là hiện
        // tượng của đồng hồ giả, không phải bug — production dùng đồng hồ thật luôn nhích). Phải tự
        // chỉnh tới trước mỗi lần ghi để mô phỏng đúng "có thời gian trôi qua giữa hai request".
        Tick(factory);

        var firstBody = new
        {
            version = created.Version, slug, title = "Đổi tên lần 1", topic = "kiem-thu", orderIndex = created.OrderIndex,
            summary = "Tóm tắt.", objectives = Array.Empty<string>(), estimatedMinutes = 15, glossary = Array.Empty<object>()
        };
        var first = await admin.PutAsJsonAsync($"/api/admin/lessons/{created.Id}", firstBody, JsonDefaults.Options);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterFirst = (await first.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;
        afterFirst.Version.Should().NotBe(created.Version); // xmin đổi sau ghi

        // Gửi lại với version CŨ (đã lạc hậu) — NỘI DUNG khác lần 1 để chắc chắn có thay đổi thật sự
        // được đưa vào SaveChanges (không phải no-op) — vẫn phải bị chặn bởi version sai ⇒ 409.
        Tick(factory);
        var secondBody = new
        {
            version = created.Version, slug, title = "Đổi tên lần 2 (phải bị chặn)", topic = "kiem-thu", orderIndex = created.OrderIndex,
            summary = "Tóm tắt.", objectives = Array.Empty<string>(), estimatedMinutes = 15, glossary = Array.Empty<object>()
        };
        var second = await admin.PutAsJsonAsync($"/api/admin/lessons/{created.Id}", secondBody, JsonDefaults.Options);
        second.StatusCode.Should().Be((HttpStatusCode)409);
        var conflictError = await second.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        conflictError!.Code.Should().Be("CONCURRENCY_CONFLICT");

        var unchanged = (await (await admin.GetAsync($"/api/admin/lessons/{created.Id}")).Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;
        unchanged.Title.Should().Be("Đổi tên lần 1"); // lần 2 KHÔNG được ghi

        // PUT blocks (bảng con) VỚI version ĐÚNG hiện tại ⇒ vẫn làm version bài đổi (R-CA2).
        Tick(factory);
        var blocksResponse = await admin.PutAsJsonAsync($"/api/admin/lessons/{created.Id}/blocks", new
        {
            version = afterFirst.Version,
            blocks = new[] { new { type = "text", payload = new { paragraphs = new[] { "Đoạn văn." } } } }
        }, JsonDefaults.Options);
        blocksResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterBlocks = (await blocksResponse.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;
        afterBlocks.Version.Should().NotBe(afterFirst.Version);
    }

    [DbFact]
    public async Task SlugTrung_409_DoiSlugBaiDaXuatBan_422SlugLocked()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);
        var slugA = UniqueSlug("slug-a");
        var slugB = UniqueSlug("slug-b");

        var published = await CreatePublishedLessonAsync(admin, slugA);

        var createB = await admin.PostAsJsonAsync("/api/admin/lessons", new { slug = slugB, title = "Bài B", topic = "kiem-thu" }, JsonDefaults.Options);
        createB.StatusCode.Should().Be(HttpStatusCode.Created);
        var lessonB = (await createB.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

        var takeSlugResponse = await admin.PutAsJsonAsync($"/api/admin/lessons/{lessonB.Id}", new
        {
            version = lessonB.Version, slug = slugA, title = lessonB.Title, topic = lessonB.Topic, orderIndex = lessonB.OrderIndex,
            summary = "", objectives = Array.Empty<string>(), estimatedMinutes = 15, glossary = Array.Empty<object>()
        }, JsonDefaults.Options);
        takeSlugResponse.StatusCode.Should().Be((HttpStatusCode)409);
        var takenError = await takeSlugResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        takenError!.Code.Should().Be("SLUG_TAKEN");

        // Đổi slug của CHÍNH bài A (đã xuất bản) ⇒ 422 SLUG_LOCKED.
        var lockedResponse = await admin.PutAsJsonAsync($"/api/admin/lessons/{published.Id}", new
        {
            version = published.Version, slug = UniqueSlug("slug-a-moi"), title = published.Title, topic = published.Topic,
            orderIndex = published.OrderIndex, summary = "", objectives = Array.Empty<string>(), estimatedMinutes = 15, glossary = Array.Empty<object>()
        }, JsonDefaults.Options);
        lockedResponse.StatusCode.Should().Be((HttpStatusCode)422);
        var lockedError = await lockedResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        lockedError!.Code.Should().Be("SLUG_LOCKED");
    }

    /// <summary>Review điều phối mục 5 — hai request TẠO bài CÙNG slug gần như đồng thời: <c>EnsureSlugAvailableAsync</c> chỉ kiểm TRƯỚC khi ghi (TOCTOU); ràng buộc <c>ux_lessons_slug</c> ở DB là tuyến phòng thủ cuối — ĐÚNG MỘT request thành công, request còn lại 409 SLUG_TAKEN (KHÔNG BAO GIỜ 500, KHÔNG BAO GIỜ cả hai cùng thành công).</summary>
    [DbFact]
    public async Task TaoBaiDongThoiCungSlug_DungMotThanhCong_ConLaiSlugTaken_KhongNem500()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);
        var slug = UniqueSlug("race-slug");

        var createA = admin.PostAsJsonAsync("/api/admin/lessons", new { slug, title = "Đua A", topic = "kiem-thu" }, JsonDefaults.Options);
        var createB = admin.PostAsJsonAsync("/api/admin/lessons", new { slug, title = "Đua B", topic = "kiem-thu" }, JsonDefaults.Options);
        var results = await Task.WhenAll(createA, createB);

        var statusCodes = results.Select(r => r.StatusCode).ToList();
        statusCodes.Should().Contain(HttpStatusCode.Created);
        statusCodes.Should().Contain((HttpStatusCode)409);
        statusCodes.Should().NotContain(HttpStatusCode.InternalServerError);

        var conflictResponse = results.First(r => r.StatusCode == (HttpStatusCode)409);
        var error = await conflictResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("SLUG_TAKEN");

        await using var db = TestDbContextFactory.Create();
        (await db.Lessons.CountAsync(l => l.Slug == slug)).Should().Be(1); // đúng MỘT bài, không phải 0 hay 2
    }

    /// <summary>Review điều phối mục 1 — id không tồn tại trong <c>content.words</c> ⇒ 422 <c>UNKNOWN_WORD</c> với <c>details.wordIds</c> (§5.2.3 — KHÔNG phải <c>details.ids</c>).</summary>
    [DbFact]
    public async Task ThayTu_IdKhongTonTai_422UnknownWord_DetailsWordIds()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);
        var slug = UniqueSlug("unknown-word");

        var create = await admin.PostAsJsonAsync("/api/admin/lessons", new { slug, title = "Bài kiểm thử", topic = "kiem-thu" }, JsonDefaults.Options);
        var created = (await create.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

        var unknownWordId = Guid.NewGuid();
        var response = await admin.PutAsJsonAsync($"/api/admin/lessons/{created.Id}/words", new
        {
            version = created.Version,
            wordIds = new[] { unknownWordId }
        }, JsonDefaults.Options);

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await response.Content.ReadFromJsonAsync<ErrorWithWordIdsDto>(JsonDefaults.Options);
        error!.Code.Should().Be("UNKNOWN_WORD");
        error.Details!.WordIds.Should().Equal(unknownWordId);
    }

    /// <summary>
    /// Review điều phối mục 5 — bài <c>source='admin'</c> mà ĐÃ có lần làm (kể cả không đua thời gian
    /// thực, chỉ cần tồn tại TRƯỚC lúc gọi xoá) vẫn phải nhất quán với R-CA7 "ngược lại ⇒ archived":
    /// không xoá cứng (tránh vi phạm FK RESTRICT <c>fk_quiz_attempts_lessons_lesson_id</c>), không 500.
    /// </summary>
    [DbFact]
    public async Task XoaBaiAdmin_DaCoLanLam_200Archived_KhongXoaCung_KhongNem500()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);
        var slug = UniqueSlug("admin-has-attempt");

        var published = await CreatePublishedLessonAsync(admin, slug);

        var learner = LearnerClient(factory);
        await using (var db = TestDbContextFactory.Create())
        {
            var questions = await db.QuizQuestions.AsNoTracking().Where(q => q.LessonId == published.Id).ToListAsync();
            var answers = questions.Select(q => new { questionId = q.Id, optionId = q.CorrectOptionId }).ToList();
            var submit = await learner.PostAsJsonAsync($"/api/lessons/{published.Id}/quiz-attempts", new
            {
                clientAttemptId = Guid.NewGuid(),
                startedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                answers
            }, JsonDefaults.Options);
            submit.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var deleteResponse = await admin.DeleteAsync($"/api/admin/lessons/{published.Id}?version={published.Version}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK); // KHÔNG 204 (xoá cứng), KHÔNG 500
        var body = await deleteResponse.Content.ReadFromJsonAsync<DeleteResultTestDto>(JsonDefaults.Options);
        body!.Result.Should().Be("archived");

        await using var finalDb = TestDbContextFactory.Create();
        (await finalDb.Lessons.AsNoTracking().SingleAsync(l => l.Id == published.Id)).Status.Should().Be("archived");
    }

    [DbFact]
    public async Task XoaBaiAdminChuaLamGi_204_XoaBaiSeedGiaLap_200Archived_NapLaiKhongDoi()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);

        // --- Bài admin CHƯA có lần làm ⇒ xoá cứng (204), mất khỏi DB. ---
        var create = await admin.PostAsJsonAsync("/api/admin/lessons", new { slug = UniqueSlug("del-admin"), title = "Xoá thử", topic = "kiem-thu" }, JsonDefaults.Options);
        var created = (await create.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

        var deleteResponse = await admin.DeleteAsync($"/api/admin/lessons/{created.Id}?version={created.Version}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.GetAsync($"/api/admin/lessons/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // --- Bài SEED GIẢ LẬP (không đụng 5 bài seed thật) ⇒ archived, KHÔNG xoá cứng. ---
        var slug = UniqueSlug("del-seed");
        const int orderIndex = 52;
        var fileJson = BuildLessonFileJson(slug, "Bài seed sắp xoá", orderIndex);
        var hash = ComputeHash(fileJson);

        Guid seedId;
        await using (var db = TestDbContextFactory.Create())
        {
            var data = new LessonImportData(slug, "Bài seed sắp xoá", "kiem-thu", "hsk1", orderIndex, "Tóm tắt.", ["Mục tiêu"], 10, "[]", LessonStatuses.Draft);
            var lesson = Lesson.CreateSeed(data, hash, DateTime.UtcNow);
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
            seedId = lesson.Id;
        }

        try
        {
            var seedDto = (await (await admin.GetAsync($"/api/admin/lessons/{seedId}")).Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

            var deleteSeedResponse = await admin.DeleteAsync($"/api/admin/lessons/{seedId}?version={seedDto.Version}");
            deleteSeedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var deleteSeedBody = await deleteSeedResponse.Content.ReadFromJsonAsync<DeleteResultTestDto>(JsonDefaults.Options);
            deleteSeedBody!.Result.Should().Be("archived");
            deleteSeedBody.Lesson!.Status.Should().Be("archived");

            // Nạp lại file KHÔNG ĐỔI (hash khớp) ⇒ Unchanged: không tạo dòng mới, không đổi trạng thái.
            var dir = CreateTempDir();
            try
            {
                await File.WriteAllTextAsync(Path.Combine(dir, $"{orderIndex:D2}-{slug}.json"), fileJson);
                await using var importerDb = TestDbContextFactory.Create();
                var importer = new LessonImporter(importerDb, TimeProvider.System, NullLogger<LessonImporter>.Instance);
                var result = await importer.ImportDirectoryAsync(dir, $"lessons-f10-{Guid.NewGuid():N}", CancellationToken.None);
                result.Status.Should().Be(ImportRunStatus.Succeeded);
                result.Unchanged.Should().Be(1);

                (await importerDb.Lessons.CountAsync(l => l.Slug == slug)).Should().Be(1);
                var afterReimport = await importerDb.Lessons.AsNoTracking().FirstAsync(l => l.Slug == slug);
                afterReimport.Status.Should().Be(LessonStatuses.Archived);
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        finally
        {
            // Dọn bài SEED GIẢ LẬP — RealLessonsTests đếm CHÍNH XÁC 5 bài source='seed' THẬT, để sót
            // lại sẽ làm số đó lệch (không phụ thuộc thứ tự chạy class trong CÙNG collection).
            await DeleteLessonDirectlyAsync(slug);
        }
    }

    [DbFact]
    public async Task SuaBaiSeedGiaLap_PutMeta_RoiNapLaiFileKhac_GiuBanAdmin()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);

        var slug = UniqueSlug("seed-meta");
        const int orderIndex = 53;
        var originalJson = BuildLessonFileJson(slug, "Tiêu đề gốc", orderIndex);
        var originalHash = ComputeHash(originalJson);

        await using (var db = TestDbContextFactory.Create())
        {
            var data = new LessonImportData(slug, "Tiêu đề gốc", "kiem-thu", "hsk1", orderIndex, "Tóm tắt.", ["Mục tiêu"], 10, "[]", LessonStatuses.Draft);
            var lesson = Lesson.CreateSeed(data, originalHash, DateTime.UtcNow);
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
        }

        try
        {
            Guid lessonId;
            await using (var lookup = TestDbContextFactory.Create())
                lessonId = await lookup.Lessons.AsNoTracking().Where(l => l.Slug == slug).Select(l => l.Id).SingleAsync();

            // Nạp lại với file ĐỔI (hash khác) khi CHƯA sửa tay ⇒ cập nhật tại chỗ bình thường.
            await ImportSingleFileAsync(slug, orderIndex, BuildLessonFileJson(slug, "Tiêu đề đã đổi qua file", orderIndex), expectedUpdated: 1);

            await using (var check1 = TestDbContextFactory.Create())
                (await check1.Lessons.AsNoTracking().FirstAsync(l => l.Slug == slug)).Title.Should().Be("Tiêu đề đã đổi qua file");

            // Admin sửa tiêu đề qua API — từ đây importer KHÔNG được ghi đè nữa.
            var current = (await (await admin.GetAsync($"/api/admin/lessons/{lessonId}")).Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;
            var metaResponse = await admin.PutAsJsonAsync($"/api/admin/lessons/{lessonId}", new
            {
                version = current.Version, slug, title = "Tiêu đề admin sửa tay", topic = "kiem-thu", orderIndex,
                summary = "Tóm tắt.", objectives = new[] { "Mục tiêu" }, estimatedMinutes = 10, glossary = Array.Empty<object>()
            }, JsonDefaults.Options);
            metaResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            await ImportSingleFileAsync(slug, orderIndex, BuildLessonFileJson(slug, "SỬA LẦN 2 QUA FILE — KHÔNG ĐƯỢC NẠP", orderIndex), expectedProtected: 1);

            await using var finalDb = TestDbContextFactory.Create();
            (await finalDb.Lessons.AsNoTracking().FirstAsync(l => l.Slug == slug)).Title.Should().Be("Tiêu đề admin sửa tay");
        }
        finally
        {
            // Dọn bài SEED GIẢ LẬP — RealLessonsTests đếm CHÍNH XÁC 5 bài source='seed' THẬT.
            await DeleteLessonDirectlyAsync(slug);
        }
    }

    [DbFact]
    public async Task DuyetBaiSeedGiaLap_RoiNapLaiFileKhac_GiuNguyen()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);

        var slug = UniqueSlug("seed-review");
        const int orderIndex = 54;
        var originalJson = BuildLessonFileJson(slug, "Tiêu đề trước duyệt", orderIndex);
        var originalHash = ComputeHash(originalJson);

        await using (var db = TestDbContextFactory.Create())
        {
            var data = new LessonImportData(slug, "Tiêu đề trước duyệt", "kiem-thu", "hsk1", orderIndex, "Tóm tắt.", ["Mục tiêu"], 10, "[]", LessonStatuses.Draft);
            var lesson = Lesson.CreateSeed(data, originalHash, DateTime.UtcNow);
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
        }

        try
        {
            Guid lessonId;
            await using (var lookup = TestDbContextFactory.Create())
                lessonId = await lookup.Lessons.AsNoTracking().Where(l => l.Slug == slug).Select(l => l.Id).SingleAsync();

            var current = (await (await admin.GetAsync($"/api/admin/lessons/{lessonId}")).Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;
            var reviewResponse = await admin.PostAsJsonAsync($"/api/admin/lessons/{lessonId}/review", new { version = current.Version }, JsonDefaults.Options);
            reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var reviewed = (await reviewResponse.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;
            reviewed.ReviewStatus.Should().Be("reviewed");

            // R-CA6: review cũng đặt edited_at (theo R-CA2) ⇒ importer khoá NGAY CẢ KHI nội dung chưa đổi tay.
            await ImportSingleFileAsync(slug, orderIndex, BuildLessonFileJson(slug, "SỬA QUA FILE SAU KHI DUYỆT — KHÔNG ĐƯỢC NẠP", orderIndex), expectedProtected: 1);

            await using var finalDb = TestDbContextFactory.Create();
            var final = await finalDb.Lessons.AsNoTracking().FirstAsync(l => l.Slug == slug);
            final.Title.Should().Be("Tiêu đề trước duyệt");
            final.ReviewStatus.Should().Be(LessonReviewStatuses.Reviewed);
        }
        finally
        {
            // Dọn bài SEED GIẢ LẬP — RealLessonsTests đếm CHÍNH XÁC 5 bài source='seed' THẬT.
            await DeleteLessonDirectlyAsync(slug);
        }
    }

    [DbFact]
    public async Task AdminSuaQuiz_BaiDangLamDo_HocVienNopTapCauCu_422QuizChanged()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);
        var slug = UniqueSlug("quiz-changed");

        var published = await CreatePublishedLessonAsync(admin, slug);
        var oldAnswers = published.Quiz.Select(q => new { questionId = q.Id, optionId = q.CorrectOptionId }).ToList();

        var replaceResponse = await admin.PutAsJsonAsync($"/api/admin/lessons/{published.Id}/quiz", new
        {
            version = published.Version,
            questions = BuildSimpleQuestions("Câu hỏi mới 1", "Câu hỏi mới 2", "Câu hỏi mới 3")
        }, JsonDefaults.Options);
        replaceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var learner = LearnerClient(factory);
        var submitResponse = await learner.PostAsJsonAsync($"/api/lessons/{published.Id}/quiz-attempts", new
        {
            clientAttemptId = Guid.NewGuid(),
            startedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            answers = oldAnswers
        }, JsonDefaults.Options);

        submitResponse.StatusCode.Should().Be((HttpStatusCode)422);
        var error = await submitResponse.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("QUIZ_CHANGED");
    }

    [DbFact]
    public async Task ThayQuiz_KhongLoiGiai_200_LoiGiaiRong()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);
        var create = await admin.PostAsJsonAsync("/api/admin/lessons", new { slug = UniqueSlug("khong-loi-giai"), title = "Không lời giải", topic = "kiem-thu" }, JsonDefaults.Options);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

        // §6.3: `explanation?` tuỳ chọn ở API admin (khác file seed — §5.4.3 bắt buộc).
        var response = await admin.PutAsJsonAsync($"/api/admin/lessons/{created.Id}/quiz", new
        {
            version = created.Version,
            questions = new object[]
            {
                new { type = "single_choice", prompt = "Không có lời giải", promptLang = "vi", options = new[] { new { text = "Có", lang = "vi" }, new { text = "Không", lang = "vi" } }, correctIndex = 0 },
                new { type = "single_choice", prompt = "Lời giải rỗng", promptLang = "vi", options = new[] { new { text = "Có", lang = "vi" }, new { text = "Không", lang = "vi" } }, correctIndex = 1, explanation = "   " },
            }
        }, JsonDefaults.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        await using var db = TestDbContextFactory.Create();
        (await db.QuizQuestions.AsNoTracking().Where(q => q.LessonId == created.Id).Select(q => q.Explanation).ToListAsync())
            .Should().HaveCount(2).And.OnlyContain(e => e == "");
    }

    [DbFact]
    public async Task HocVienNopQuiz_BaiVuaBiXoaCungGiuaChung_404KhongNem500()
    {
        using var factory = new ChineseDbApiFactory();
        SetRealContentRoot();
        var admin = await AdminClientAsync(factory);
        var slug = UniqueSlug("xoa-giua-chung");
        var published = await CreatePublishedLessonAsync(admin, slug);
        var answers = published.Quiz.Select(q => new { questionId = q.Id, optionId = q.CorrectOptionId }).ToList();

        // Chen giữa: service đã đọc thấy bài published, ngay TRƯỚC lệnh INSERT lesson_progress thì bài bị xoá
        // cứng ở kết nối khác (mô phỏng admin DELETE đua với học viên nộp bài).
        var interceptor = new DeleteLessonBeforeProgressInsertInterceptor(published.Id);
        using var racingFactory = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.ConfigureDbContext<ChineseDbContext>(o => o.AddInterceptors(interceptor))));

        var learner = racingFactory.CreateClient();
        learner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenFactory.CreateToken(Guid.NewGuid()));
        var response = await learner.PostAsJsonAsync($"/api/lessons/{published.Id}/quiz-attempts", new
        {
            clientAttemptId = Guid.NewGuid(),
            startedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            answers
        }, JsonDefaults.Options);

        interceptor.Fired.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>(JsonDefaults.Options);
        error!.Code.Should().Be("NOT_FOUND");

        await using var db = TestDbContextFactory.Create();
        (await db.QuizAttempts.AnyAsync(a => a.LessonId == published.Id)).Should().BeFalse();
    }

    private sealed class DeleteLessonBeforeProgressInsertInterceptor(Guid lessonId) : DbCommandInterceptor
    {
        private int _fired;

        public bool Fired => _fired == 1;

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO learning.lesson_progress", StringComparison.Ordinal)
                && command.Parameters.Cast<DbParameter>().Any(p => p.Value is Guid g && g == lessonId)
                && Interlocked.CompareExchange(ref _fired, 1, 0) == 0)
            {
                await using var other = TestDbContextFactory.Create();
                await other.Database.ExecuteSqlAsync($"DELETE FROM content.lessons WHERE id = {lessonId}", cancellationToken);
            }

            return result;
        }
    }

    // ---- tiện ích dùng chung ----

    private static void SetRealContentRoot() => Environment.SetEnvironmentVariable("Content__RootPath", ResolveRealContentRoot());

    /// <summary>Đồng hồ giả của <see cref="ChineseDbApiFactory"/> ĐỨNG YÊN trừ khi chỉnh tay — nhích tới trước mỗi lần ghi cần thấy <c>edited_at</c>/<c>updated_at</c> ĐỔI THẬT (không trùng giá trị lần ghi trước, R-CA2).</summary>
    private static void Tick(ChineseDbApiFactory factory) => factory.TimeProvider.AdjustTime(factory.TimeProvider.GetUtcNow().AddSeconds(1));

    private static string UniqueSlug(string label)
    {
        var full = $"f10-{label}-{Guid.NewGuid():N}";
        return full.Length <= 40 ? full : full[..40];
    }

    private static async Task<HttpClient> AdminClientAsync(ChineseDbApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", factory.TokenFactory.CreateToken(Guid.NewGuid(), email: ChineseDbApiFactory.BootstrapAdminEmail));
        (await client.GetAsync("/api/me")).EnsureSuccessStatusCode(); // provision + bootstrap admin (content.manage)
        return client;
    }

    private static HttpClient LearnerClient(ChineseDbApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenFactory.CreateToken(Guid.NewGuid()));
        return client;
    }

    private static async Task<List<Guid>> GetSampleWordIdsAsync(int count)
    {
        await using var db = TestDbContextFactory.Create();
        return await db.Words.AsNoTracking().Where(w => w.Hsk3Level == 1)
            .OrderBy(w => w.PathOrder).Take(count).Select(w => w.Id).ToListAsync();
    }

    private static object[] BuildSimpleQuestions(string p1 = "1 cộng 1 bằng mấy?", string p2 = "Thủ đô Việt Nam là gì?", string p3 = "Trời trong xanh màu gì?") =>
    [
        new { type = "single_choice", prompt = p1, promptLang = "vi", options = new[] { new { text = "1", lang = "vi" }, new { text = "2", lang = "vi" } }, correctIndex = 1, explanation = "1 + 1 = 2." },
        new { type = "single_choice", prompt = p2, promptLang = "vi", options = new[] { new { text = "Hà Nội", lang = "vi" }, new { text = "Hồ Chí Minh", lang = "vi" } }, correctIndex = 0, explanation = "Hà Nội là thủ đô." },
        new { type = "single_choice", prompt = p3, promptLang = "vi", options = new[] { new { text = "Xanh", lang = "vi" }, new { text = "Đỏ", lang = "vi" } }, correctIndex = 0, explanation = "Màu xanh." }
    ];

    /// <summary>Tạo bài → thay khối/từ/quiz → xuất bản — trả về <c>AdminLesson</c> đầy đủ SAU xuất bản (dùng chung cho nhiều test).</summary>
    private static async Task<AdminLessonTestDto> CreatePublishedLessonAsync(HttpClient admin, string slug)
    {
        var create = await admin.PostAsJsonAsync("/api/admin/lessons", new { slug, title = "Bài kiểm thử F10", topic = "kiem-thu" }, JsonDefaults.Options);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

        var blocksResponse = await admin.PutAsJsonAsync($"/api/admin/lessons/{created.Id}/blocks", new
        {
            version = created.Version,
            blocks = new[] { new { type = "text", payload = new { paragraphs = new[] { "Nội dung kiểm thử." } } } }
        }, JsonDefaults.Options);
        blocksResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterBlocks = (await blocksResponse.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

        var wordIds = await GetSampleWordIdsAsync(2);
        var wordsResponse = await admin.PutAsJsonAsync($"/api/admin/lessons/{created.Id}/words", new { version = afterBlocks.Version, wordIds }, JsonDefaults.Options);
        wordsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterWords = (await wordsResponse.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

        var quizResponse = await admin.PutAsJsonAsync($"/api/admin/lessons/{created.Id}/quiz", new
        {
            version = afterWords.Version,
            questions = BuildSimpleQuestions()
        }, JsonDefaults.Options);
        quizResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterQuiz = (await quizResponse.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;

        var publishResponse = await admin.PostAsJsonAsync($"/api/admin/lessons/{created.Id}/publish", new { version = afterQuiz.Version }, JsonDefaults.Options);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await publishResponse.Content.ReadFromJsonAsync<AdminLessonTestDto>(JsonDefaults.Options))!;
    }

    /// <summary>Xoá thẳng một bài GIẢ LẬP qua DB (bỏ qua API — R-CA7 chặn xoá cứng bài <c>source='seed'</c> có ý đồ) — dọn dẹp sau các test dựng bài seed giả để không làm lệch số "5 bài seed thật" mà <c>RealLessonsTests</c> đếm.</summary>
    private static async Task DeleteLessonDirectlyAsync(string slug)
    {
        await using var db = TestDbContextFactory.Create();
        var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Slug == slug);
        if (lesson is null)
            return;

        db.Lessons.Remove(lesson);
        await db.SaveChangesAsync();
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"af-admin-lessons-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static async Task ImportSingleFileAsync(string slug, int orderIndex, string json, int expectedUpdated = 0, int expectedProtected = 0)
    {
        var dir = CreateTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, $"{orderIndex:D2}-{slug}.json"), json);
            await using var db = TestDbContextFactory.Create();
            var importer = new LessonImporter(db, TimeProvider.System, NullLogger<LessonImporter>.Instance);
            var result = await importer.ImportDirectoryAsync(dir, $"lessons-f10-{Guid.NewGuid():N}", CancellationToken.None);
            result.Status.Should().Be(ImportRunStatus.Succeeded);
            if (expectedUpdated > 0)
                result.Updated.Should().Be(expectedUpdated);
            if (expectedProtected > 0)
                result.Protected.Should().Be(expectedProtected);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Bài <c>draft</c> tối giản hợp lệ (một khối text, không từ/quiz) — đủ để <c>LessonImporter</c> nạp (không chạm <c>LessonPublishRules</c> vì <c>status=draft</c>).</summary>
    private static string BuildLessonFileJson(string slug, string title, int orderIndex) => $$"""
        {
          "schemaVersion": 1,
          "slug": "{{slug}}",
          "title": "{{title}}",
          "topic": "kiem-thu",
          "level": "hsk1",
          "orderIndex": {{orderIndex}},
          "status": "draft",
          "summary": "Tóm tắt kiểm thử.",
          "objectives": ["Mục tiêu kiểm thử"],
          "estimatedMinutes": 10,
          "sources": ["original"],
          "words": [],
          "glossary": [],
          "blocks": [
            { "type": "text", "payload": { "paragraphs": ["Nội dung kiểm thử."] } }
          ],
          "quiz": []
        }
        """;

    private static string ComputeHash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

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

    // ---- DTO test ----

    private sealed record AdminQuizOptionTestDto(string Id, string Text, string Lang);
    private sealed record AdminQuizQuestionTestDto(Guid Id, string Key, string Type, string Prompt, List<AdminQuizOptionTestDto> Options, string CorrectOptionId);
    private sealed record AdminLessonTestDto(
        Guid Id, uint Version, string Slug, string Title, string Topic, int OrderIndex, string Summary,
        List<string> Objectives, short EstimatedMinutes, string Status, string ReviewStatus, string Source,
        DateTime? PublishedAt, bool HasAttempts, List<AdminQuizQuestionTestDto> Quiz, List<string> Warnings);
    private sealed record DeleteResultTestDto(string Result, AdminLessonTestDto? Lesson);
    private sealed record LessonListItemTestDto(string Slug);
    private sealed record LessonListTestDto(List<LessonListItemTestDto> Items);
    private sealed record ErrorDto(string Error, string Code);
    private sealed record ErrorProblemsDetailsDto(List<string> Problems);
    private sealed record ErrorWithProblemsDto(string Error, string Code, ErrorProblemsDetailsDto? Details);
    private sealed record ErrorWordIdsDetailsDto(List<Guid> WordIds);
    private sealed record ErrorWithWordIdsDto(string Error, string Code, ErrorWordIdsDetailsDto? Details);
}
