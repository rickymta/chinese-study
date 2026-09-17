using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Lessons.Dtos;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Lessons;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Lessons;

/// <summary>Truy vấn CHỈ ĐỌC cho học viên (§5.2.1.2, §6.1) — danh sách/chi tiết bài <c>published</c>, lịch sử lần làm quiz.</summary>
public sealed class LessonQueryService(IChineseDbContext db)
{
    /// <summary><c>GET /api/lessons</c> — không phân trang (R-LS4: số bài nhỏ); <c>nextLessonSlug</c> = bài <c>published</c> có <c>orderIndex</c> nhỏ nhất mà người học chưa <c>completed</c>.</summary>
    public async Task<LessonListResponseDto> ListPublishedAsync(Guid userId, CancellationToken ct)
    {
        var lessons = await db.Lessons.AsNoTracking()
            .Where(l => l.Status == LessonStatuses.Published)
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Title)
            .ToListAsync(ct);

        var lessonIds = lessons.Select(l => l.Id).ToList();

        var wordCounts = await db.LessonWords.AsNoTracking()
            .Where(w => lessonIds.Contains(w.LessonId))
            .GroupBy(w => w.LessonId)
            .Select(g => new { LessonId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LessonId, x => x.Count, ct);

        var questionCounts = await db.QuizQuestions.AsNoTracking()
            .Where(q => lessonIds.Contains(q.LessonId))
            .GroupBy(q => q.LessonId)
            .Select(g => new { LessonId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LessonId, x => x.Count, ct);

        var progresses = await db.LessonProgress.AsNoTracking()
            .Where(p => p.UserId == userId && lessonIds.Contains(p.LessonId))
            .ToDictionaryAsync(p => p.LessonId, ct);

        var items = lessons.Select(l => new LessonListItemDto(
            l.Id, l.Slug, l.Title, l.Topic, l.OrderIndex, l.Summary, l.EstimatedMinutes,
            wordCounts.GetValueOrDefault(l.Id), questionCounts.GetValueOrDefault(l.Id), l.ReviewStatus,
            progresses.TryGetValue(l.Id, out var progress) ? ToProgressDto(progress) : null)).ToList();

        var nextSlug = lessons
            .Where(l => !progresses.TryGetValue(l.Id, out var p) || p.Status != LessonProgressStatuses.Completed)
            .OrderBy(l => l.OrderIndex)
            .Select(l => l.Slug)
            .FirstOrDefault();

        return new LessonListResponseDto(items, nextSlug);
    }

    /// <summary><c>GET /api/lessons/{slug}</c> — 404 nếu bài không tồn tại hoặc không <c>published</c> (R-LS1). KHÔNG BAO GIỜ trả <c>correctOptionId</c>/<c>explanation</c>/<c>key</c> của quiz (R-LS10).</summary>
    public async Task<LessonDetailDto> GetPublishedBySlugAsync(Guid userId, string slug, CancellationToken ct)
    {
        var lesson = await db.Lessons.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Slug == slug && l.Status == LessonStatuses.Published, ct)
            ?? throw new NotFoundException($"Không tìm thấy bài học '{slug}'.");

        var blocks = await db.LessonBlocks.AsNoTracking()
            .Where(b => b.LessonId == lesson.Id)
            .OrderBy(b => b.OrderIndex)
            .ToListAsync(ct);

        var lessonWords = await db.LessonWords.AsNoTracking()
            .Where(w => w.LessonId == lesson.Id)
            .OrderBy(w => w.OrderIndex)
            .ToListAsync(ct);
        var wordIds = lessonWords.Select(w => w.WordId).ToList();
        var wordsById = await db.Words.AsNoTracking()
            .Where(w => wordIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, ct);
        var srsWordIds = (await db.SrsCards.AsNoTracking()
            .Where(c => c.UserId == userId && wordIds.Contains(c.WordId))
            .Select(c => c.WordId)
            .ToListAsync(ct)).ToHashSet();

        var questions = await db.QuizQuestions.AsNoTracking()
            .Where(q => q.LessonId == lesson.Id)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync(ct);

        var progress = await db.LessonProgress.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lesson.Id, ct);

        var glossary = LessonJson.Deserialize<List<GlossaryItem>>(lesson.Glossary) ?? [];

        return new LessonDetailDto(
            lesson.Id, lesson.Slug, lesson.Title, lesson.Topic, lesson.OrderIndex, lesson.Summary,
            lesson.Objectives, lesson.EstimatedMinutes, lesson.ReviewStatus,
            [.. glossary.Select(g => new LessonGlossaryItemDto(g.Hanzi, g.Pinyin, g.Vi))],
            [.. blocks.Select(b => new LessonBlockDto(b.Id, b.Type, LessonJson.ToElement(b.Payload)))],
            [.. lessonWords.Select(lw => ToWordDto(wordsById[lw.WordId], srsWordIds.Contains(lw.WordId)))],
            [.. questions.Select(ToStudentQuestionDto)],
            progress is null ? null : ToProgressDto(progress));
    }

    /// <summary><c>GET /api/lessons/{id}/quiz-attempts?limit=</c> — mới nhất trước, KHÔNG kèm chi tiết từng câu (R-LS10/R-LS11 chỉ hé lộ trong kết quả nộp/chi tiết một lần làm — chưa có endpoint chi tiết một attempt ở F9).</summary>
    public async Task<QuizAttemptListResponseDto> ListAttemptsAsync(Guid userId, Guid lessonId, int limit, CancellationToken ct)
    {
        var attempts = await db.QuizAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.LessonId == lessonId)
            .OrderByDescending(a => a.SubmittedAt)
            .Take(limit)
            .ToListAsync(ct);

        return new QuizAttemptListResponseDto(
            [.. attempts.Select(a => new QuizAttemptSummaryDto(a.Id, a.SubmittedAt, a.Total, a.Correct, a.ScorePercent, a.Passed, a.DurationMs))]);
    }

    internal static LessonProgressDto ToProgressDto(LessonProgress p) =>
        new(p.Status, p.BestScorePercent, p.AttemptsCount, p.StartedAt, p.CompletedAt, p.LastAttemptAt);

    private static LessonWordDto ToWordDto(Word w, bool inSrs) =>
        new(w.Id, w.Simplified, w.Traditional, w.Pinyin, w.HanViet, w.MeaningsVi, w.MeaningViStatus, inSrs);

    private static LessonQuizQuestionDto ToStudentQuestionDto(QuizQuestion q)
    {
        var options = LessonJson.Deserialize<List<QuizOption>>(q.Options) ?? [];
        return new LessonQuizQuestionDto(
            q.Id, q.Type, q.Prompt, q.PromptLang, q.PromptPinyin, q.AudioText,
            [.. options.Select(o => new LessonQuizOptionDto(o.Id, o.Text, o.Lang))]);
    }
}
