using System.Security.Cryptography;
using AntFarm.Chinese.Application.Admin.Content.Dtos;
using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Lessons;
using AntFarm.Chinese.Application.Lessons.Dtos;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Lessons;
using AntFarm.Chinese.Domain.Text;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Chinese.Application.Admin.Content;

/// <summary>
/// Quản trị bài học + quiz (§5.2.3, §6.3, R-CA1..R-CA8) — mọi thao tác GHI đặt <c>edited_at</c>/
/// <c>edited_by</c> VÀ chạm dòng <c>content.lessons</c> (<see cref="Lesson.TouchEdited"/>) để
/// <c>xmin</c> đổi kể cả khi chỉ sửa bảng con (R-CA2), kèm kiểm <c>version</c> gửi lên khớp
/// <c>xmin</c> đọc lần trước (R-CA3, <see cref="IChineseDbContext.SetOriginalVersion"/>). Mọi thao
/// tác ghi thành công đều <c>LogInformation</c> (ai, bài nào, làm gì) — vết audit tối thiểu cho nội
/// dung học liệu (review điều phối 17/09/2026, mục 6).
/// </summary>
public sealed class LessonAdminService(IChineseDbContext db, TimeProvider timeProvider, ILogger<LessonAdminService> logger)
{
    public async Task<AdminLessonListResponseDto> ListAsync(AdminLessonsQuery query, CancellationToken ct)
    {
        var lessonsQuery = db.Lessons.AsNoTracking().AsQueryable();
        lessonsQuery = query.Status is { } status
            ? lessonsQuery.Where(l => l.Status == status)
            // §5.2.3 "Danh sách admin": mặc định ẨN archived trừ khi lọc tường minh status=archived.
            : lessonsQuery.Where(l => l.Status != LessonStatuses.Archived);

        var lessons = await lessonsQuery.ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var qPlain = VietnameseText.RemoveDiacritics(query.Q).ToLowerInvariant();
            lessons = [.. lessons.Where(l =>
                VietnameseText.RemoveDiacritics(l.Title).ToLowerInvariant().Contains(qPlain, StringComparison.Ordinal) ||
                VietnameseText.RemoveDiacritics(l.Slug).ToLowerInvariant().Contains(qPlain, StringComparison.Ordinal))];
        }

        var ordered = lessons.OrderBy(l => l.OrderIndex).ThenBy(l => l.Title, StringComparer.Ordinal).ToList();
        var totalCount = ordered.Count;
        var page = ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();
        var pageIds = page.Select(l => l.Id).ToList();

        var wordCounts = await db.LessonWords.AsNoTracking()
            .Where(w => pageIds.Contains(w.LessonId))
            .GroupBy(w => w.LessonId)
            .Select(g => new { LessonId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LessonId, x => x.Count, ct);

        var questionCounts = await db.QuizQuestions.AsNoTracking()
            .Where(q => pageIds.Contains(q.LessonId))
            .GroupBy(q => q.LessonId)
            .Select(g => new { LessonId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LessonId, x => x.Count, ct);

        var items = page.Select(l => new AdminLessonListItemDto(
            l.Id, l.Slug, l.Title, l.OrderIndex, l.Status, l.ReviewStatus, l.Source,
            wordCounts.GetValueOrDefault(l.Id), questionCounts.GetValueOrDefault(l.Id), l.EditedAt, l.PublishedAt)).ToList();

        return new AdminLessonListResponseDto(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<AdminLessonDto> GetAsync(Guid id, CancellationToken ct)
    {
        var lesson = await db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy bài học.");
        return await BuildDtoAsync(lesson, ct);
    }

    /// <summary>§5.2.3 "Tạo bài": <c>status=draft</c>, <c>source='admin'</c>, <c>review_status='machine'</c>; <c>orderIndex</c> bỏ trống ⇒ <c>max+1</c>.</summary>
    public async Task<AdminLessonDto> CreateAsync(CreateLessonRequest request, Guid userId, CancellationToken ct)
    {
        await EnsureSlugAvailableAsync(request.Slug, null, ct);

        var orderIndex = request.OrderIndex ?? (await db.Lessons.MaxAsync(l => (int?)l.OrderIndex, ct) ?? 0) + 1;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lesson = Lesson.CreateDraft(request.Slug, request.Title, request.Topic, orderIndex, request.Summary ?? "", userId, now);

        db.Lessons.Add(lesson);
        // Dùng CHUNG SaveWithConcurrencyAsync để bắt cả đua slug (mục 5 review) — EnsureSlugAvailableAsync
        // ở trên chỉ kiểm TRƯỚC khi ghi (TOCTOU); ux_lessons_slug (DB) là tuyến phòng thủ cuối.
        await SaveWithConcurrencyAsync(ct);

        logger.LogInformation("Admin {UserId} tạo bài {LessonId} (slug '{Slug}').", userId, lesson.Id, lesson.Slug);
        return await BuildDtoAsync(lesson, ct);
    }

    public async Task<AdminLessonDto> UpdateMetaAsync(Guid id, UpdateLessonMetaRequest request, Guid userId, CancellationToken ct)
    {
        var lesson = await LoadForWriteAsync(id, request.Version, ct);
        EnsureNotArchived(lesson);

        if (!string.Equals(lesson.Slug, request.Slug, StringComparison.Ordinal))
        {
            // R-CA8: đổi slug chỉ được khi bài CHƯA TỪNG xuất bản (published_at IS NULL) — tránh gãy
            // link/bộ chữ luyện viết "lesson:<slug>" (F8) đã chia sẻ ra ngoài.
            if (lesson.PublishedAt is not null)
                throw new BusinessRuleException("SLUG_LOCKED", "Bài đã từng xuất bản — không đổi được slug.");
            await EnsureSlugAvailableAsync(request.Slug, id, ct);
        }

        var glossaryItems = request.Glossary.Select(g => new GlossaryItem(g.Hanzi, g.Pinyin, g.Vi)).ToList();
        ThrowIfInvalid(LessonContentValidator.ValidateGlossary(glossaryItems));

        lesson.UpdateMeta(
            request.Slug, request.Title, request.Topic, request.OrderIndex, request.Summary,
            request.Objectives, request.EstimatedMinutes, LessonJson.Serialize(glossaryItems));

        var dto = await TouchSaveAndBuildAsync(lesson, userId, ct);
        logger.LogInformation("Admin {UserId} sửa thông tin chung bài {LessonId} (slug '{Slug}').", userId, id, lesson.Slug);
        return dto;
    }

    /// <summary>§5.2.3 "Thay từ"/importer §5.2.1.4 — XOÁ HẾT rồi thêm (khoá là uuid mới, không xung đột với CÙNG <c>SaveChanges</c>).</summary>
    public async Task<AdminLessonDto> ReplaceBlocksAsync(Guid id, ReplaceLessonBlocksRequest request, Guid userId, CancellationToken ct)
    {
        var lesson = await LoadForWriteAsync(id, request.Version, ct);
        EnsureNotArchived(lesson);

        var problems = new List<ValidationProblem>();
        for (var i = 0; i < request.Blocks.Count; i++)
            problems.AddRange(LessonContentValidator.ValidateBlock(request.Blocks[i].Type, request.Blocks[i].Payload, $"blocks[{i}].payload"));
        ThrowIfInvalid(problems);

        var existing = await db.LessonBlocks.Where(b => b.LessonId == id).ToListAsync(ct);
        if (existing.Count > 0)
            db.LessonBlocks.RemoveRange(existing);

        for (var i = 0; i < request.Blocks.Count; i++)
            db.LessonBlocks.Add(LessonBlock.Create(id, (short)i, request.Blocks[i].Type, request.Blocks[i].Payload.GetRawText()));

        var dto = await TouchSaveAndBuildAsync(lesson, userId, ct);
        logger.LogInformation("Admin {UserId} thay khối nội dung bài {LessonId} ({Count} khối).", userId, id, request.Blocks.Count);
        return dto;
    }

    /// <summary>§5.2.3 "Thay từ": id không tồn tại ⇒ 422 <c>UNKNOWN_WORD</c> (<c>details.wordIds</c>); DIFF (không xoá-chèn) giữ nguyên thẻ SRS/tiến độ liên quan không bị ảnh hưởng bởi khoá dòng <c>lesson_words</c>.</summary>
    public async Task<AdminLessonDto> ReplaceWordsAsync(Guid id, ReplaceLessonWordsRequest request, Guid userId, CancellationToken ct)
    {
        var lesson = await LoadForWriteAsync(id, request.Version, ct);
        EnsureNotArchived(lesson);

        var existingWordIds = await db.Words.AsNoTracking()
            .Where(w => request.WordIds.Contains(w.Id))
            .Select(w => w.Id)
            .ToListAsync(ct);
        var missing = request.WordIds.Except(existingWordIds).ToList();
        if (missing.Count > 0)
            throw new BusinessRuleException("UNKNOWN_WORD", "Một số từ không tồn tại.", new { wordIds = missing });

        await DiffLessonWordsAsync(id, request.WordIds, ct);

        var dto = await TouchSaveAndBuildAsync(lesson, userId, ct);
        logger.LogInformation("Admin {UserId} thay danh sách từ bài {LessonId} ({Count} từ).", userId, id, request.WordIds.Count);
        return dto;
    }

    /// <summary>§5.2.3 "Thay quiz": mục có <c>id</c> phải thuộc bài (422 <c>UNKNOWN_QUESTION</c>) ⇒ cập nhật, giữ <c>key</c>; mục không <c>id</c> ⇒ tạo <c>key = "m-" + 8 hex</c>; câu cũ vắng mặt ⇒ xoá. <c>options</c> gán id <c>a..d</c> theo VỊ TRÍ, <c>correctIndex</c> 0-based đổi thành <c>correctOptionId</c>.</summary>
    public async Task<AdminLessonDto> ReplaceQuizAsync(Guid id, ReplaceLessonQuizRequest request, Guid userId, CancellationToken ct)
    {
        var lesson = await LoadForWriteAsync(id, request.Version, ct);
        EnsureNotArchived(lesson);

        var existing = await db.QuizQuestions.Where(q => q.LessonId == id).ToListAsync(ct);
        var existingById = existing.ToDictionary(q => q.Id);

        var unknownIds = request.Questions
            .Where(q => q.Id is { } qid && !existingById.ContainsKey(qid))
            .Select(q => q.Id!.Value)
            .ToList();
        if (unknownIds.Count > 0)
            throw new BusinessRuleException("UNKNOWN_QUESTION", "Một số câu hỏi không thuộc bài.", new { ids = unknownIds });

        var problems = new List<ValidationProblem>();
        var prepared = new List<PreparedQuestion>();

        for (var i = 0; i < request.Questions.Count; i++)
        {
            var q = request.Questions[i];
            var path = $"questions[{i}]";

            if (q.CorrectIndex < 0 || q.CorrectIndex >= q.Options.Count)
            {
                problems.Add(new ValidationProblem($"{path}.correctIndex", "correctIndex phải là chỉ số hợp lệ trong options."));
                continue;
            }

            var options = q.Options.Select((o, j) => new QuizOption(OptionIdAt(j), o.Text, o.Lang)).ToList();
            var correctOptionId = options[q.CorrectIndex].Id;
            var explanation = q.Explanation?.Trim() ?? "";

            var input = new QuestionValidationInput(q.Type, q.Prompt, q.PromptLang, q.PromptPinyin, q.AudioText, options, correctOptionId, explanation);
            problems.AddRange(LessonContentValidator.ValidateQuestion(input, path, requireExplanation: false)); // §6.3: explanation? tuỳ chọn

            prepared.Add(new PreparedQuestion(q.Id, (short)i, q.Type, q.Prompt, q.PromptLang, q.PromptPinyin, q.AudioText, LessonJson.Serialize(options), correctOptionId, explanation));
        }
        ThrowIfInvalid(problems);

        var keptIds = prepared.Where(p => p.Id is not null).Select(p => p.Id!.Value).ToHashSet();
        var toRemove = existing.Where(q => !keptIds.Contains(q.Id)).ToList();
        if (toRemove.Count > 0)
            db.QuizQuestions.RemoveRange(toRemove);

        foreach (var p in prepared)
        {
            if (p.Id is { } existingId)
                existingById[existingId].Apply(p.OrderIndex, p.Type, p.Prompt, p.PromptLang, p.PromptPinyin, p.AudioText, p.OptionsJson, p.CorrectOptionId, p.Explanation);
            else
                db.QuizQuestions.Add(QuizQuestion.Create(id, GenerateQuestionKey(), p.OrderIndex, p.Type, p.Prompt, p.PromptLang, p.PromptPinyin, p.AudioText, p.OptionsJson, p.CorrectOptionId, p.Explanation));
        }

        var dto = await TouchSaveAndBuildAsync(lesson, userId, ct);
        logger.LogInformation("Admin {UserId} thay danh sách quiz bài {LessonId} ({Count} câu).", userId, id, request.Questions.Count);
        return dto;
    }

    public async Task<AdminLessonDto> PublishAsync(Guid id, LessonVersionRequest request, Guid userId, CancellationToken ct)
    {
        var lesson = await LoadForWriteAsync(id, request.Version, ct);
        EnsureNotArchived(lesson);

        var (blocks, wordCount, questions) = await LoadPublishInputsAsync(id, ct);
        var (problems, _) = ComputePublishCheck(lesson, blocks, wordCount, questions);
        if (problems.Count > 0)
            throw new BusinessRuleException("LESSON_NOT_PUBLISHABLE", "Bài chưa đủ điều kiện xuất bản.", new { problems });

        var now = timeProvider.GetUtcNow().UtcDateTime;
        lesson.Publish(now);

        var dto = await TouchSaveAndBuildAsync(lesson, userId, ct);
        logger.LogInformation("Admin {UserId} xuất bản bài {LessonId} (slug '{Slug}').", userId, id, lesson.Slug);
        return dto;
    }

    public async Task<AdminLessonDto> UnpublishAsync(Guid id, LessonVersionRequest request, Guid userId, CancellationToken ct)
    {
        var lesson = await LoadForWriteAsync(id, request.Version, ct);
        EnsureNotArchived(lesson);

        lesson.Unpublish();

        var dto = await TouchSaveAndBuildAsync(lesson, userId, ct);
        logger.LogInformation("Admin {UserId} gỡ xuất bản bài {LessonId} (slug '{Slug}').", userId, id, lesson.Slug);
        return dto;
    }

    /// <summary>R-CA6: duyệt bài — cho phép ở MỌI trạng thái (kể cả <c>draft</c>/<c>archived</c>), không kiểm <c>LESSON_ARCHIVED</c>.</summary>
    public async Task<AdminLessonDto> ReviewAsync(Guid id, LessonVersionRequest request, Guid userId, CancellationToken ct)
    {
        var lesson = await LoadForWriteAsync(id, request.Version, ct);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        lesson.Review(userId, now);

        var dto = await TouchSaveAndBuildAsync(lesson, userId, ct);
        logger.LogInformation("Admin {UserId} duyệt bài {LessonId} (slug '{Slug}').", userId, id, lesson.Slug);
        return dto;
    }

    public async Task<AdminLessonDto> RestoreAsync(Guid id, LessonVersionRequest request, Guid userId, CancellationToken ct)
    {
        var lesson = await LoadForWriteAsync(id, request.Version, ct);
        if (lesson.Status != LessonStatuses.Archived)
            throw new BusinessRuleException("LESSON_NOT_ARCHIVED", "Bài không ở trạng thái lưu trữ.");

        lesson.RestoreFromArchive();

        var dto = await TouchSaveAndBuildAsync(lesson, userId, ct);
        logger.LogInformation("Admin {UserId} khôi phục bài {LessonId} (slug '{Slug}') từ lưu trữ.", userId, id, lesson.Slug);
        return dto;
    }

    /// <summary>
    /// R-CA7: bài <c>source='admin'</c> VÀ chưa có <c>quiz_attempts</c> ⇒ xoá cứng; ngược lại ⇒
    /// chuyển <c>archived</c>. Đua hiếm (mục 5 review): học viên nộp quiz GIỮA lúc kiểm
    /// <c>hasAttempts</c> và lúc <c>DELETE</c> thật sự ⇒ ràng buộc <c>fk_quiz_attempts_lessons_lesson_id</c>
    /// (RESTRICT) chặn — bắt <see cref="DbUpdateException"/> tương ứng rồi XỬ LÝ NHẤT QUÁN với R-CA7
    /// (coi như "đã có lần làm" ⇒ chuyển lưu trữ) thay vì để lộ 500.
    /// </summary>
    public async Task<DeleteLessonResult> DeleteAsync(Guid id, uint version, Guid userId, CancellationToken ct)
    {
        var lesson = await LoadForWriteAsync(id, version, ct);
        var hasAttempts = await db.QuizAttempts.AsNoTracking().AnyAsync(a => a.LessonId == id, ct);

        if (lesson.Source == LessonSources.Admin && !hasAttempts)
        {
            db.Lessons.Remove(lesson);
            try
            {
                await SaveWithConcurrencyAsync(ct);
                logger.LogInformation("Admin {UserId} xoá cứng bài {LessonId} (source=admin, chưa có lần làm).", userId, id);
                return new DeleteLessonResult(true, null);
            }
            catch (DbUpdateException ex) when (IsQuizAttemptsRestrictViolation(ex))
            {
                db.ClearTracking();
                logger.LogInformation(
                    "Admin {UserId} xoá cứng bài {LessonId} vấp lần làm quiz vừa nộp giữa chừng — chuyển sang lưu trữ.",
                    userId, id);
                return await ArchiveInsteadOfDeleteAsync(id, version, userId, ct);
            }
        }

        lesson.Archive();
        var dto = await TouchSaveAndBuildAsync(lesson, userId, ct);
        logger.LogInformation("Admin {UserId} lưu trữ (archive) bài {LessonId} (slug '{Slug}').", userId, id, lesson.Slug);
        return new DeleteLessonResult(false, dto);
    }

    private async Task<DeleteLessonResult> ArchiveInsteadOfDeleteAsync(Guid id, uint version, Guid userId, CancellationToken ct)
    {
        // Nạp lại — SaveChangesAsync thất bại (DbUpdateException) đã tự ROLLBACK giao dịch ngầm của
        // EF, dòng lessons vẫn NGUYÊN VẸN với version gốc nên dùng lại đúng version người gọi gửi lên.
        var lesson = await LoadForWriteAsync(id, version, ct);
        lesson.Archive();
        var dto = await TouchSaveAndBuildAsync(lesson, userId, ct);
        return new DeleteLessonResult(false, dto);
    }

    // ---- tiện ích dùng chung ----

    private async Task<Lesson> LoadForWriteAsync(Guid id, uint version, CancellationToken ct)
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy bài học.");
        db.SetOriginalVersion(lesson, version);
        return lesson;
    }

    private static void EnsureNotArchived(Lesson lesson)
    {
        if (lesson.Status == LessonStatuses.Archived)
            throw new BusinessRuleException("LESSON_ARCHIVED", "Bài đang lưu trữ — khôi phục trước khi sửa.");
    }

    private async Task EnsureSlugAvailableAsync(string slug, Guid? excludeId, CancellationToken ct)
    {
        var query = db.Lessons.AsNoTracking().Where(l => l.Slug == slug);
        if (excludeId is { } id)
            query = query.Where(l => l.Id != id);

        if (await query.AnyAsync(ct))
            throw new ConflictException("SLUG_TAKEN", $"Slug '{slug}' đã được dùng.");
    }

    /// <summary>Tên ràng buộc UNIQUE do CHÍNH ta đặt trong <c>LessonConfiguration</c> (ổn định, không phụ thuộc kiểu Npgsql cụ thể — cùng kỹ thuật <c>QuizSubmissionService.IsClientAttemptIdUniqueViolation</c>).</summary>
    private static bool IsSlugUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("ux_lessons_slug", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Tên ràng buộc FK RESTRICT do CHÍNH ta đặt trong <c>QuizAttemptConfiguration</c> (R-CA7 — bài có lần làm không xoá cứng được).</summary>
    private static bool IsQuizAttemptsRestrictViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("fk_quiz_attempts_lessons_lesson_id", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>R-CA2: MỌI thao tác ghi chạm dòng <c>content.lessons</c> (dù chỉ sửa bảng con) rồi mới <c>SaveChanges</c> — <c>xmin</c> luôn đổi.</summary>
    private async Task<AdminLessonDto> TouchSaveAndBuildAsync(Lesson lesson, Guid userId, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        lesson.TouchEdited(userId, now);
        await SaveWithConcurrencyAsync(ct);
        return await BuildDtoAsync(lesson, ct);
    }

    /// <summary>
    /// Dùng CHUNG cho MỌI lần ghi (tạo/sửa) — bắt hai loại đua (mục 5 review):
    /// (1) <see cref="DbUpdateConcurrencyException"/> — <c>version</c> gửi lên lệch <c>xmin</c> hiện
    /// tại (R-CA3, 409 <c>CONCURRENCY_CONFLICT</c>); (2) vi phạm <c>ux_lessons_slug</c> — hai admin
    /// cùng lúc tạo/đổi CÙNG slug đều qua được <see cref="EnsureSlugAvailableAsync"/> (chỉ kiểm
    /// TRƯỚC khi ghi, TOCTOU) rồi đụng nhau ở ràng buộc DB (409 <c>SLUG_TAKEN</c>, KHÔNG để lộ 500) —
    /// <c>ClearTracking</c> vì entity vừa Add/Modified còn ở trạng thái lỗi trong ChangeTracker.
    /// </summary>
    private async Task SaveWithConcurrencyAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("CONCURRENCY_CONFLICT", "Bài đã bị sửa ở nơi khác — hãy tải lại.");
        }
        catch (DbUpdateException ex) when (IsSlugUniqueViolation(ex))
        {
            db.ClearTracking();
            throw new ConflictException("SLUG_TAKEN", "Slug đã được dùng.");
        }
    }

    /// <summary>Cùng thuật toán <c>LessonImporter.DiffLessonWordsAsync</c> (§5.2.1.4) — trùng lặp có chủ đích: importer (Infrastructure) không phụ thuộc Application, dùng transaction/savepoint riêng nên không tái dùng trực tiếp được.</summary>
    private async Task DiffLessonWordsAsync(Guid lessonId, IReadOnlyList<Guid> wordIds, CancellationToken ct)
    {
        var existing = await db.LessonWords.Where(w => w.LessonId == lessonId).ToListAsync(ct);
        var existingByWordId = existing.ToDictionary(w => w.WordId);
        var expected = wordIds.ToHashSet();

        var toRemove = existing.Where(w => !expected.Contains(w.WordId)).ToList();
        if (toRemove.Count > 0)
            db.LessonWords.RemoveRange(toRemove);

        for (var i = 0; i < wordIds.Count; i++)
        {
            if (existingByWordId.TryGetValue(wordIds[i], out var lessonWord))
                lessonWord.SetOrderIndex((short)i);
            else
                db.LessonWords.Add(LessonWord.Create(lessonId, wordIds[i], (short)i));
        }
    }

    private async Task<(List<LessonBlock> Blocks, int WordCount, List<QuizQuestion> Questions)> LoadPublishInputsAsync(Guid lessonId, CancellationToken ct)
    {
        var blocks = await db.LessonBlocks.AsNoTracking().Where(b => b.LessonId == lessonId).ToListAsync(ct);
        var wordCount = await db.LessonWords.AsNoTracking().CountAsync(w => w.LessonId == lessonId, ct);
        var questions = await db.QuizQuestions.AsNoTracking().Where(q => q.LessonId == lessonId).ToListAsync(ct);
        return (blocks, wordCount, questions);
    }

    private static (IReadOnlyList<string> Problems, IReadOnlyList<string> Warnings) ComputePublishCheck(
        Lesson lesson, IReadOnlyList<LessonBlock> blocks, int wordCount, IReadOnlyList<QuizQuestion> questions)
    {
        var hasDialogue = blocks.Any(b => b.Type == LessonBlockTypes.Dialogue);
        var pcQuestions = questions.Select(q =>
        {
            var options = LessonJson.Deserialize<List<QuizOption>>(q.Options) ?? [];
            return new PublishCheckQuestion(q.Type, options.Count, options.Any(o => o.Id == q.CorrectOptionId), q.AudioText);
        }).ToList();

        return LessonPublishRules.Check(new PublishCheckInput(lesson.Title, blocks.Count, hasDialogue, wordCount, pcQuestions));
    }

    private async Task<AdminLessonDto> BuildDtoAsync(Lesson lesson, CancellationToken ct)
    {
        var blocks = await db.LessonBlocks.AsNoTracking().Where(b => b.LessonId == lesson.Id).OrderBy(b => b.OrderIndex).ToListAsync(ct);
        var lessonWords = await db.LessonWords.AsNoTracking().Where(w => w.LessonId == lesson.Id).OrderBy(w => w.OrderIndex).ToListAsync(ct);
        var wordIds = lessonWords.Select(w => w.WordId).ToList();
        var wordsById = await db.Words.AsNoTracking().Where(w => wordIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, ct);
        var questions = await db.QuizQuestions.AsNoTracking().Where(q => q.LessonId == lesson.Id).OrderBy(q => q.OrderIndex).ToListAsync(ct);
        var hasAttempts = await db.QuizAttempts.AsNoTracking().AnyAsync(a => a.LessonId == lesson.Id, ct);
        var editedByName = lesson.EditedBy is { } editorId
            ? await db.Users.AsNoTracking().Where(u => u.Id == editorId).Select(u => (string?)u.DisplayName).FirstOrDefaultAsync(ct)
            : null;

        var glossary = LessonJson.Deserialize<List<GlossaryItem>>(lesson.Glossary) ?? [];
        var (_, warnings) = ComputePublishCheck(lesson, blocks, lessonWords.Count, questions);

        return new AdminLessonDto(
            lesson.Id, lesson.Version, lesson.Slug, lesson.Title, lesson.Topic, lesson.Level, lesson.OrderIndex,
            lesson.Summary, lesson.Objectives, lesson.EstimatedMinutes,
            [.. glossary.Select(g => new LessonGlossaryItemDto(g.Hanzi, g.Pinyin, g.Vi))],
            lesson.Status, lesson.ReviewStatus, lesson.Source,
            lesson.PublishedAt, lesson.ReviewedAt, lesson.EditedAt, editedByName, lesson.CreatedAt, lesson.UpdatedAt,
            hasAttempts,
            [.. blocks.Select(b => new AdminLessonBlockDto(b.Id, b.Type, LessonJson.ToElement(b.Payload)))],
            [.. lessonWords.Select(lw => ToAdminWordDto(wordsById[lw.WordId]))],
            [.. questions.Select(ToAdminQuestionDto)],
            warnings);
    }

    private static AdminLessonWordDto ToAdminWordDto(Word w) => new(w.Id, w.Simplified, w.Pinyin, w.MeaningsVi, w.MeaningViStatus);

    private static AdminQuizQuestionDto ToAdminQuestionDto(QuizQuestion q)
    {
        var options = LessonJson.Deserialize<List<QuizOption>>(q.Options) ?? [];
        return new AdminQuizQuestionDto(
            q.Id, q.Key, q.Type, q.Prompt, q.PromptLang, q.PromptPinyin, q.AudioText,
            [.. options.Select(o => new AdminQuizOptionDto(o.Id, o.Text, o.Lang))],
            q.CorrectOptionId, q.Explanation);
    }

    private static void ThrowIfInvalid(IReadOnlyList<ValidationProblem> problems)
    {
        if (problems.Count == 0)
            return;

        var details = problems
            .GroupBy(p => p.Path)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Message).ToArray());
        throw new ValidationAppException("Nội dung không hợp lệ.", details);
    }

    private static string OptionIdAt(int index) => ((char)('a' + index)).ToString();

    private static string GenerateQuestionKey() => "m-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();

    private sealed record PreparedQuestion(
        Guid? Id, short OrderIndex, string Type, string Prompt, string PromptLang, string? PromptPinyin,
        string? AudioText, string OptionsJson, string CorrectOptionId, string Explanation);
}
