using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Learning;
using AntFarm.Chinese.Application.Lessons.Dtos;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Lessons;
using AntFarm.Chinese.Domain.Srs;
using AntFarm.Chinese.Application.Srs;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Lessons;

/// <summary>Ảnh chụp một câu đã trả lời (R-LS11, §5.1.1) — lưu trong <c>quiz_attempts.answers</c> (jsonb) để lịch sử vẫn đọc được khi admin sửa/xoá câu hỏi sau đó. KHÔNG lưu <c>explanation</c> — kết quả (lần đầu lẫn phát lại) tra <c>explanation</c> SỐNG từ <c>quiz_questions</c> hiện tại (rỗng nếu câu đã bị xoá).</summary>
public sealed record QuizAnswerSnapshot(
    Guid QuestionId, string Key, string Type, string Prompt, string? AudioText,
    string OptionId, string OptionText, string CorrectOptionId, string CorrectOptionText, bool Correct);

/// <summary><c>POST /api/lessons/{id}/quiz-attempts</c> (§5.2.1.2, §6.1) — chấm ở SERVER, idempotent theo <c>clientAttemptId</c> (R-LS9), hoàn thành lần đầu ⇒ thêm thẻ SRS + ghi <c>study_events</c> trong CÙNG transaction (R-LS5, K12).</summary>
public sealed class QuizSubmissionService(
    IChineseDbContext db,
    TimeProvider timeProvider,
    IStudyActivityRecorder studyActivityRecorder,
    ISrsCardService srsCardService)
{
    public async Task<(SubmitQuizResponseDto Response, bool IsReplay)> SubmitAsync(
        Guid userId, Guid lessonId, SubmitQuizRequest request, CancellationToken ct)
    {
        var existingAttempt = await db.QuizAttempts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ClientAttemptId == request.ClientAttemptId, ct);
        if (existingAttempt is not null)
        {
            EnsureMatchesRequest(existingAttempt, userId, lessonId);
            return (await BuildReplayResponseAsync(existingAttempt, ct), true);
        }

        try
        {
            return (await SubmitNewAsync(userId, lessonId, request, ct), false);
        }
        catch (DbUpdateException ex) when (IsClientAttemptIdUniqueViolation(ex))
        {
            // Đua hiếm (R-LS9): hai request cùng clientAttemptId gần như đồng thời — transaction của
            // ta tự ROLLBACK khi ném (chưa Commit); gỡ ChangeTracker rồi đọc lại kết quả người thắng
            // (cùng kỹ thuật SrsReviewService.ReviewAsync, F7).
            db.ClearTracking();

            var winner = await db.QuizAttempts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.ClientAttemptId == request.ClientAttemptId, ct)
                ?? throw new InvalidOperationException("Vi phạm duy nhất client_attempt_id nhưng không tìm thấy attempt tương ứng — không nên xảy ra.");

            EnsureMatchesRequest(winner, userId, lessonId);
            return (await BuildReplayResponseAsync(winner, ct), true);
        }
        catch (Exception ex) when (IsLessonForeignKeyViolation(ex))
        {
            // Đua hiếm (F10, R-CA7): admin xoá cứng bài NGAY SAU khi ta đọc thấy bài published — lần
            // ghi lesson_progress/quiz_attempts vấp FK tới content.lessons. Transaction đã ROLLBACK
            // khi dispose; với học viên bài coi như không còn ⇒ 404 thay vì 500.
            db.ClearTracking();
            throw new NotFoundException($"Không tìm thấy bài học '{lessonId}'.");
        }
    }

    private async Task<SubmitQuizResponseDto> SubmitNewAsync(Guid userId, Guid lessonId, SubmitQuizRequest request, CancellationToken ct)
    {
        var lesson = await db.Lessons.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.Status == LessonStatuses.Published, ct)
            ?? throw new NotFoundException($"Không tìm thấy bài học '{lessonId}'.");

        var questions = await db.QuizQuestions.AsNoTracking()
            .Where(q => q.LessonId == lessonId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync(ct);
        if (questions.Count == 0)
            throw new BusinessRuleException("QUIZ_EMPTY", "Bài chưa có câu hỏi.");

        var currentQuestionIds = questions.Select(q => q.Id).ToHashSet();
        var submittedQuestionIds = request.Answers.Select(a => a.QuestionId).ToHashSet();
        if (!currentQuestionIds.SetEquals(submittedQuestionIds))
            throw new BusinessRuleException("QUIZ_CHANGED", "Tập câu hỏi của bài đã thay đổi — tải lại bài rồi làm lại.");

        var optionsByQuestion = questions.ToDictionary(q => q.Id, q => LessonJson.Deserialize<List<QuizOption>>(q.Options) ?? []);
        var answerByQuestion = request.Answers.ToDictionary(a => a.QuestionId, a => a.OptionId);

        for (var i = 0; i < request.Answers.Count; i++)
        {
            var answer = request.Answers[i];
            if (!optionsByQuestion[answer.QuestionId].Any(o => o.Id == answer.OptionId))
            {
                var details = new Dictionary<string, string[]> { [$"answers[{i}].optionId"] = ["optionId không thuộc lựa chọn của câu hỏi này."] };
                throw new ValidationAppException("Dữ liệu gửi lên không hợp lệ.", details);
            }
        }

        var snapshots = questions.Select(q => new LessonQuizQuestionSnapshot(q.Id, q.CorrectOptionId)).ToList();
        var grade = QuizGrader.Grade(snapshots, answerByQuestion);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // Review F9 (CHẶN): request.StartedAt là DateTimeOffset (offset tuỳ ý, vd +07:00 từ đồng hồ
        // máy khách) — chuẩn hoá MỘT LẦN ở đây bằng .UtcDateTime (luôn Kind=Utc) trước khi dùng ở BẤT
        // KỲ nơi nào ghi timestamptz (Npgsql chỉ nhận Kind=Utc) hoặc trừ với nowUtc.
        var startedAtUtc = request.StartedAt?.UtcDateTime;
        var durationMs = startedAtUtc is null ? (int?)null : (int)Math.Max(0, (nowUtc - startedAtUtc.Value).TotalMilliseconds);

        await using var transaction = await db.BeginTransactionAsync(ct);

        // Tạo lesson_progress nếu chưa có (học viên có thể nộp quiz mà chưa gọi POST .../start) — ghi
        // NGAY qua SQL (ON CONFLICT DO NOTHING, không qua ChangeTracker) rồi SELECT ... FOR UPDATE để
        // khoá đúng dòng, cùng kỹ thuật SrsCardService.InsertNewCardIfMissingAsync (K12). started_at
        // = startedAtUtc đã chuẩn hoá ?? nowUtc (§5.2.1.2 bước 7: "started_at = req.StartedAt ?? now").
        await db.ExecuteSqlAsync($"""
            INSERT INTO learning.lesson_progress (user_id, lesson_id, status, started_at, attempts_count, updated_at)
            VALUES ({userId}, {lessonId}, {LessonProgressStatuses.InProgress}, {startedAtUtc ?? nowUtc}, 0, {nowUtc})
            ON CONFLICT (user_id, lesson_id) DO NOTHING
            """, ct);

        var progress = await db.LessonProgress
            .FromSqlInterpolated($"SELECT * FROM learning.lesson_progress WHERE user_id = {userId} AND lesson_id = {lessonId} FOR UPDATE")
            .FirstAsync(ct);

        var firstCompletion = progress.ApplyAttempt(grade.ScorePercent, grade.Passed, nowUtc);

        var snapshot = BuildSnapshot(questions, optionsByQuestion, answerByQuestion, grade);
        var attempt = QuizAttempt.Create(
            request.ClientAttemptId, userId, lessonId, startedAtUtc, nowUtc, durationMs, grade, LessonJson.Serialize(snapshot));
        db.QuizAttempts.Add(attempt);

        // R-LS7: MỌI lần nộp (đạt hay không) ghi quiz_submit ⇒ tính là "ngày có học" (R-PG1).
        await studyActivityRecorder.RecordAsync(userId, StudyEventKinds.QuizSubmit, nowUtc, grade.Total, grade.Correct, attempt.Id, ct);

        var srsCardsAdded = 0;
        if (firstCompletion)
        {
            // R-LS5/K12: lần ĐẠT ĐẦU TIÊN ⇒ thêm thẻ SRS cho từ CHƯA có thẻ (bất kỳ nguồn) + ghi
            // lesson_complete, TRONG CÙNG transaction — EnsureCardsAsync tự tham gia transaction hiện
            // tại (ghi qua SQL trực tiếp trên CÙNG DbContext, xem ISrsCardService).
            var wordIds = await db.LessonWords.AsNoTracking()
                .Where(w => w.LessonId == lessonId)
                .Select(w => w.WordId)
                .ToListAsync(ct);
            srsCardsAdded = await srsCardService.EnsureCardsAsync(userId, wordIds, SrsCardSources.Lesson, ct);

            await studyActivityRecorder.RecordAsync(userId, StudyEventKinds.LessonComplete, nowUtc, 1, null, lessonId, ct);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var explanationsById = questions.ToDictionary(q => q.Id, q => q.Explanation);
        var results = grade.Items.Select(item =>
            new QuizResultItemDto(item.QuestionId, item.OptionId, item.Correct, item.CorrectOptionId, explanationsById.GetValueOrDefault(item.QuestionId, "")))
            .ToList();

        return new SubmitQuizResponseDto(
            attempt.Id, attempt.SubmittedAt, grade.Total, grade.Correct, grade.ScorePercent, grade.Passed,
            QuizGrader.PassThresholdPercent, firstCompletion, srsCardsAdded, results, LessonQueryService.ToProgressDto(progress));
    }

    /// <summary>Gửi lại đúng <c>clientAttemptId</c> (R-LS9) — dựng lại kết quả từ bản ghi đã lưu, KHÔNG ghi thêm gì. <c>firstCompletion</c> so <c>lesson_progress.completed_at</c> với <c>attempt.submitted_at</c> (§6.1) — CHỈ đúng cho lần đạt đầu tiên, vì service luôn đặt hai mốc này BẰNG NHAU khi đó.</summary>
    private async Task<SubmitQuizResponseDto> BuildReplayResponseAsync(QuizAttempt attempt, CancellationToken ct)
    {
        var progress = await db.LessonProgress.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == attempt.UserId && p.LessonId == attempt.LessonId, ct)
            ?? throw new InvalidOperationException("Có quiz_attempts nhưng thiếu lesson_progress tương ứng — không nên xảy ra.");

        var snapshot = LessonJson.Deserialize<List<QuizAnswerSnapshot>>(attempt.Answers) ?? [];
        var questionIds = snapshot.Select(s => s.QuestionId).ToList();
        var explanationsById = await db.QuizQuestions.AsNoTracking()
            .Where(q => questionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, q => q.Explanation, ct);

        var results = snapshot.Select(s =>
            new QuizResultItemDto(s.QuestionId, s.OptionId, s.Correct, s.CorrectOptionId, explanationsById.GetValueOrDefault(s.QuestionId, "")))
            .ToList();

        var firstCompletion = progress.CompletedAt == attempt.SubmittedAt;

        return new SubmitQuizResponseDto(
            attempt.Id, attempt.SubmittedAt, attempt.Total, attempt.Correct, attempt.ScorePercent, attempt.Passed,
            QuizGrader.PassThresholdPercent, firstCompletion, 0, results, LessonQueryService.ToProgressDto(progress));
    }

    /// <summary>Review F9: <c>clientAttemptId</c> trùng nhưng KHÁC người dùng HOẶC KHÁC bài (URL bài khác gửi nhầm lại đúng mã của lần nộp trước) đều là dùng sai mã — 409 <c>DUPLICATE_ATTEMPT_ID</c> thay vì âm thầm trả kết quả của lần nộp KHÁC.</summary>
    private static void EnsureMatchesRequest(QuizAttempt existing, Guid callerId, Guid lessonId)
    {
        if (existing.UserId != callerId || existing.LessonId != lessonId)
            throw new ConflictException("DUPLICATE_ATTEMPT_ID", "Mã lần nộp (clientAttemptId) đã được dùng cho người dùng hoặc bài học khác.");
    }

    private static List<QuizAnswerSnapshot> BuildSnapshot(
        IReadOnlyList<QuizQuestion> questions,
        IReadOnlyDictionary<Guid, List<QuizOption>> optionsByQuestion,
        IReadOnlyDictionary<Guid, string> answerByQuestion,
        QuizGrade grade)
    {
        var gradedByQuestion = grade.Items.ToDictionary(i => i.QuestionId);
        var snapshot = new List<QuizAnswerSnapshot>(questions.Count);

        foreach (var question in questions)
        {
            var optionId = answerByQuestion[question.Id];
            var options = optionsByQuestion[question.Id];
            var chosen = options.First(o => o.Id == optionId);
            var correctOption = options.First(o => o.Id == question.CorrectOptionId);
            var graded = gradedByQuestion[question.Id];

            snapshot.Add(new QuizAnswerSnapshot(
                question.Id, question.Key, question.Type, question.Prompt, question.AudioText,
                optionId, chosen.Text, question.CorrectOptionId, correctOption.Text, graded.Correct));
        }

        return snapshot;
    }

    /// <summary>Tên ràng buộc do CHÍNH ta đặt trong <c>QuizAttemptConfiguration</c> — ổn định, không phụ thuộc kiểu Npgsql cụ thể (cùng kỹ thuật <c>SrsReviewService.IsClientReviewIdUniqueViolation</c>).</summary>
    /// <summary>Vi phạm FK tới <c>content.lessons</c> (tên do EF đặt theo quy ước snake_case — ổn định). <c>ExecuteSqlAsync</c> ném thẳng lỗi Postgres (không bọc <c>DbUpdateException</c>) nên duyệt cả chuỗi <c>InnerException</c>.</summary>
    private static bool IsLessonForeignKeyViolation(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("fk_lesson_progress_lessons_lesson_id", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("fk_quiz_attempts_lessons_lesson_id", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsClientAttemptIdUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("ux_quiz_attempts_client_attempt_id", StringComparison.OrdinalIgnoreCase) == true;
}
