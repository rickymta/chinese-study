using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AntFarm.Chinese.Application.Lessons;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Lessons;
using AntFarm.Chinese.Domain.Pinyin;
using AntFarm.Chinese.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Chinese.Infrastructure.Content;

/// <summary>
/// Nạp bài học từ <c>content/chinese/data/lessons/NN-slug.json</c> lúc khởi động (§5.2.1.4, R-LS14) —
/// gọi ở CUỐI <see cref="ContentImportRunner.RunAsync"/>, SAU <c>characters</c>/<c>hsk-words</c> (từ
/// của bài phải tra được trong <c>content.words</c>). Idempotent theo hash tổng hợp các tệp; KHÔNG
/// BAO GIỜ ném ra ngoài (bắt mọi lỗi, log Error, trả <see cref="ImportSummary"/>). Mỗi bài dùng một
/// SAVEPOINT riêng trong một transaction bao ngoài (khoá advisory + ghi <c>import_runs</c> MỘT lần
/// cho cả thư mục) — bài hỏng ROLLBACK TỚI SAVEPOINT của nó, không kéo bài khác đã nạp trong CÙNG lượt
/// (R-LS15 "mỗi bài một transaction riêng" mà vẫn giữ được khoá advisory một lần cho toàn thư mục).
/// </summary>
public sealed partial class LessonImporter(ChineseDbContext db, TimeProvider timeProvider, ILogger<LessonImporter> logger)
{
    /// <summary>Tăng khi đổi LOGIC nạp (không phải khi đổi dữ liệu) — buộc nạp lại dù hash tệp không đổi.</summary>
    public const int Version = 1;

    private const int AdvisoryLockKey = 727008;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    [GeneratedRegex(@"^(\d{2})-([a-z0-9-]+)\.json$")]
    private static partial Regex FileNamePattern();

    public async Task<ImportSummary> ImportDirectoryAsync(string directory, string dataset, CancellationToken ct)
    {
        var startedAt = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            return await ImportDirectoryCoreAsync(directory, dataset, startedAt, ct);
        }
        catch (Exception ex)
        {
            // Lưới an toàn cuối cùng (lỗi hạ tầng ngoài dự kiến, vd mất kết nối DB) — KHÔNG BAO GIỜ
            // để lỗi bay ra ContentImportRunner (R-LS15).
            logger.LogError(ex, "Nạp bài học (dataset {Dataset}) thất bại ngoài dự kiến.", dataset);
            return ImportSummary.Failed(ex.Message);
        }
    }

    private async Task<ImportSummary> ImportDirectoryCoreAsync(string directory, string dataset, DateTime startedAt, CancellationToken ct)
    {
        if (!Directory.Exists(directory))
        {
            logger.LogWarning("Không thấy thư mục bài học '{Directory}' (dataset {Dataset}) — bỏ qua nạp.", directory, dataset);
            return ImportSummary.Skipped();
        }

        var fileNames = Directory.GetFiles(directory, "*.json")
            .Select(Path.GetFileName)
            .Where(f => f is not null && FileNamePattern().IsMatch(f))
            .Select(f => f!)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        if (fileNames.Count == 0)
        {
            logger.LogWarning("Thư mục bài học '{Directory}' không có file khớp mẫu NN-slug.json (dataset {Dataset}) — bỏ qua.", directory, dataset);
            return ImportSummary.Skipped();
        }

        var files = new List<(string FileName, byte[] Bytes, string Hash)>(fileNames.Count);
        foreach (var fileName in fileNames)
        {
            var bytes = await File.ReadAllBytesAsync(Path.Combine(directory, fileName), ct);
            files.Add((fileName, bytes, ComputeHash(bytes)));
        }

        var aggregateHash = ComputeHash(Encoding.UTF8.GetBytes(string.Join("", files.Select(f => f.Hash))));

        if (await ShouldSkipEntireRunAsync(dataset, aggregateHash, files.Count, ct))
        {
            logger.LogInformation("Học liệu bài học (dataset {Dataset}) không đổi — bỏ qua.", dataset);
            return ImportSummary.Skipped();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // R-LS14/§5.2.1.4: hai tiến trình khởi động cùng lúc không nạp chồng (cùng kỹ thuật ContentImporter).
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({AdvisoryLockKey})", ct);

            if (await ShouldSkipEntireRunAsync(dataset, aggregateHash, files.Count, ct))
            {
                await transaction.RollbackAsync(ct);
                logger.LogInformation("Học liệu bài học (dataset {Dataset}) vừa được tiến trình khác nạp xong trong lúc chờ khoá — bỏ qua.", dataset);
                return ImportSummary.Skipped();
            }

            var counts = new ImportCounts();
            var now = timeProvider.GetUtcNow().UtcDateTime;

            for (var i = 0; i < files.Count; i++)
            {
                var (fileName, bytes, hash) = files[i];
                var savepoint = $"lesson_{i}";
                await transaction.CreateSavepointAsync(savepoint, ct);
                try
                {
                    var outcome = await ImportOneFileAsync(fileName, bytes, hash, now, ct);
                    await db.SaveChangesAsync(ct);
                    ApplyOutcome(counts, outcome);
                }
                catch (Exception ex)
                {
                    // R-LS15: file hỏng/thiếu từ ⇒ bỏ qua CẢ BÀI, KHÔNG kéo các bài khác trong CÙNG lượt nạp.
                    await transaction.RollbackToSavepointAsync(savepoint, ct);
                    db.ChangeTracker.Clear();
                    counts.Invalid++;
                    logger.LogError(ex, "Nạp bài học '{FileName}' thất bại — bỏ qua cả bài.", fileName);
                }
            }

            var finishedAt = timeProvider.GetUtcNow().UtcDateTime;
            db.ImportRuns.Add(ImportRun.Succeeded(
                dataset, aggregateHash, Version, counts.Inserted, counts.Updated, counts.Unchanged, counts.Invalid, counts.Protected,
                startedAt, finishedAt));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation(
                "Nạp bài học: thêm {Inserted}, cập nhật {Updated}, bỏ qua {Skipped}, lỗi {Invalid}",
                counts.Inserted, counts.Updated, counts.Unchanged + counts.Protected, counts.Invalid);

            return ImportSummary.Succeeded(counts.Inserted, counts.Updated, counts.Unchanged, counts.Invalid, counts.Protected);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            logger.LogError(ex, "Nạp bài học (dataset {Dataset}) thất bại.", dataset);
            return await WriteFailedRunAsync(dataset, aggregateHash, startedAt, ex.Message, ct);
        }
    }

    /// <summary>Bỏ qua TOÀN BỘ lượt nạp khi hash tổng hợp + phiên bản importer khớp lần <c>succeeded</c> gần nhất VÀ số bài <c>source='seed'</c> hiện có trong DB đã đủ (§5.2.1.4 — DB trống/mới xoá dữ liệu vẫn phải nạp đủ dù hash không đổi).</summary>
    private async Task<bool> ShouldSkipEntireRunAsync(string dataset, string aggregateHash, int fileCount, CancellationToken ct)
    {
        var lastSucceeded = await GetLastSucceededAsync(dataset, ct);
        if (lastSucceeded is null || lastSucceeded.FileHash != aggregateHash || lastSucceeded.ImporterVersion != Version)
            return false;

        var seedCountInDb = await db.Lessons.AsNoTracking().CountAsync(l => l.Source == LessonSources.Seed, ct);
        return seedCountInDb >= fileCount;
    }

    private Task<ImportRun?> GetLastSucceededAsync(string dataset, CancellationToken ct) =>
        db.ImportRuns.AsNoTracking()
            .Where(r => r.Dataset == dataset && r.Status == "succeeded")
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync(ct);

    private async Task<ImportSummary> WriteFailedRunAsync(string dataset, string hash, DateTime startedAt, string error, CancellationToken ct)
    {
        try
        {
            db.ChangeTracker.Clear();
            var finishedAt = timeProvider.GetUtcNow().UtcDateTime;
            db.ImportRuns.Add(ImportRun.Failed(dataset, hash, Version, Truncate(error, 4000), startedAt, finishedAt));
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ghi content.import_runs (dataset {Dataset}, trạng thái failed) thất bại.", dataset);
        }

        return ImportSummary.Failed(error);
    }

    // ---- một tệp bài học ----

    private enum LessonImportOutcome { Inserted, Updated, Unchanged, Protected }

    private async Task<LessonImportOutcome> ImportOneFileAsync(string fileName, byte[] bytes, string hash, DateTime nowUtc, CancellationToken ct)
    {
        var nameMatch = FileNamePattern().Match(fileName);
        if (!nameMatch.Success)
            throw new InvalidDataException($"'{fileName}': tên file không khớp mẫu NN-slug.json.");

        var orderFromName = int.Parse(nameMatch.Groups[1].Value);
        var slugFromName = nameMatch.Groups[2].Value;

        LessonFile? file;
        try
        {
            file = JsonSerializer.Deserialize<LessonFile>(bytes, JsonOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"'{fileName}': JSON không hợp lệ — {ex.Message}", ex);
        }

        if (file is null)
            throw new InvalidDataException($"'{fileName}': nội dung rỗng hoặc không parse được.");
        if (file.SchemaVersion != 1)
            throw new InvalidDataException($"'{fileName}': schemaVersion phải là 1.");
        if (!string.Equals(file.Slug, slugFromName, StringComparison.Ordinal))
            throw new InvalidDataException($"'{fileName}': slug '{file.Slug}' khác phần tên file '{slugFromName}'.");
        if (file.OrderIndex != orderFromName)
            throw new InvalidDataException($"'{fileName}': orderIndex {file.OrderIndex} khác số thứ tự tên file '{orderFromName}'.");
        if (file.Status != LessonStatuses.Draft && file.Status != LessonStatuses.Published)
            throw new InvalidDataException($"'{fileName}': status '{file.Status}' không hợp lệ (draft|published).");

        var problems = ValidateFile(file);
        if (problems.Count > 0)
            throw new InvalidDataException($"'{fileName}': {problems.Count} lỗi nội dung — {string.Join(" | ", problems.Take(5).Select(p => $"{p.Path}: {p.Message}"))}");

        // Tra từ theo (simplified, pinyin CHUẨN HOÁ) — R-LS15: thiếu MỘT từ ⇒ bỏ qua CẢ BÀI.
        var wordIds = new List<Guid>();
        var missingWords = new List<string>();
        foreach (var w in file.Words ?? [])
        {
            var normalizedPinyin = PinyinText.NormalizeNumbered(w.Pinyin) ?? w.Pinyin;
            var word = await db.Words.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Simplified == w.Simplified && x.Pinyin == normalizedPinyin, ct);
            if (word is null)
                missingWords.Add($"{w.Simplified} {w.Pinyin}");
            else
                wordIds.Add(word.Id);
        }
        if (missingWords.Count > 0)
            throw new InvalidDataException($"'{fileName}': thiếu từ trong content.words — {string.Join(", ", missingWords)}");

        if (file.Status == LessonStatuses.Published)
        {
            var (publishProblems, _) = LessonPublishRules.Check(BuildPublishCheckInput(file, wordIds.Count));
            if (publishProblems.Count > 0)
                throw new InvalidDataException($"'{fileName}': không đủ điều kiện xuất bản — {string.Join(" | ", publishProblems)}");
        }

        var importData = BuildImportData(file);
        var existing = await db.Lessons.FirstOrDefaultAsync(l => l.Slug == file.Slug, ct);

        Lesson lesson;
        LessonImportOutcome outcome;
        if (existing is null)
        {
            lesson = Lesson.CreateSeed(importData, hash, nowUtc);
            db.Lessons.Add(lesson);
            outcome = LessonImportOutcome.Inserted;
        }
        else if (existing.SourceHash == hash)
        {
            return LessonImportOutcome.Unchanged;
        }
        else if (!existing.IsSeedImportable)
        {
            logger.LogInformation("Bài '{Slug}' đã sửa/duyệt tay — không nạp đè.", existing.Slug);
            return LessonImportOutcome.Protected;
        }
        else
        {
            existing.ApplySeedUpdate(importData, hash, nowUtc);
            lesson = existing;
            outcome = LessonImportOutcome.Updated;
        }

        await ReplaceBlocksAsync(lesson.Id, file.Blocks ?? [], ct);
        await DiffLessonWordsAsync(lesson.Id, wordIds, ct);
        await UpsertQuizQuestionsAsync(lesson.Id, file.Quiz ?? [], ct);

        return outcome;
    }

    private static List<ValidationProblem> ValidateFile(LessonFile file)
    {
        var problems = new List<ValidationProblem>();

        var blocks = file.Blocks ?? [];
        for (var i = 0; i < blocks.Count; i++)
            problems.AddRange(LessonContentValidator.ValidateBlock(blocks[i].Type, blocks[i].Payload, $"blocks[{i}].payload"));

        var glossary = (file.Glossary ?? []).Select(g => new GlossaryItem(g.Hanzi, g.Pinyin, g.Vi)).ToList();
        problems.AddRange(LessonContentValidator.ValidateGlossary(glossary));

        var quiz = file.Quiz ?? [];
        for (var i = 0; i < quiz.Count; i++)
        {
            var q = quiz[i];
            var options = (q.Options ?? []).Select(o => new QuizOption(o.Id, o.Text, o.Lang)).ToList();
            var input = new QuestionValidationInput(q.Type, q.Prompt, q.PromptLang, q.PromptPinyin, q.AudioText, options, q.CorrectOptionId, q.Explanation ?? "");
            problems.AddRange(LessonContentValidator.ValidateQuestion(input, $"quiz[{i}]"));
        }

        return problems;
    }

    private static PublishCheckInput BuildPublishCheckInput(LessonFile file, int wordCount)
    {
        var blocks = file.Blocks ?? [];
        var hasDialogue = blocks.Any(b => b.Type == LessonBlockTypes.Dialogue);
        var questions = (file.Quiz ?? []).Select(q => new PublishCheckQuestion(
            q.Type, (q.Options ?? []).Count, (q.Options ?? []).Any(o => o.Id == q.CorrectOptionId), q.AudioText)).ToList();

        return new PublishCheckInput(file.Title, blocks.Count, hasDialogue, wordCount, questions);
    }

    private static LessonImportData BuildImportData(LessonFile file)
    {
        var glossary = (file.Glossary ?? []).Select(g => new GlossaryItem(g.Hanzi, g.Pinyin, g.Vi)).ToList();
        return new LessonImportData(
            file.Slug, file.Title, file.Topic, file.Level, file.OrderIndex, file.Summary,
            file.Objectives ?? [], (short)file.EstimatedMinutes, LessonJson.Serialize(glossary), file.Status);
    }

    private void AddBlocks(Guid lessonId, IReadOnlyList<LessonFileBlock> blocks)
    {
        for (var i = 0; i < blocks.Count; i++)
            db.LessonBlocks.Add(LessonBlock.Create(lessonId, (short)i, blocks[i].Type, blocks[i].Payload.GetRawText()));
    }

    /// <summary>§5.2.1.4: <c>lesson_blocks</c> XOÁ HẾT rồi thêm (khoá là uuid mới, không xung đột) — đơn giản hơn diff vì thứ tự/nội dung khối đổi tuỳ tiện giữa các lần soạn.</summary>
    private async Task ReplaceBlocksAsync(Guid lessonId, IReadOnlyList<LessonFileBlock> blocks, CancellationToken ct)
    {
        var existing = await db.LessonBlocks.Where(b => b.LessonId == lessonId).ToListAsync(ct);
        if (existing.Count > 0)
            db.LessonBlocks.RemoveRange(existing);

        AddBlocks(lessonId, blocks);
    }

    /// <summary>§5.2.1.4: DIFF (không xoá-chèn) — xoá từ không còn, cập nhật <c>order_index</c> từ đổi vị trí, thêm từ mới; tránh xung đột khoá chính trong CÙNG <c>SaveChanges</c>.</summary>
    private async Task DiffLessonWordsAsync(Guid lessonId, IReadOnlyList<Guid> wordIds, CancellationToken ct)
    {
        var existing = await db.LessonWords.Where(w => w.LessonId == lessonId).ToListAsync(ct);
        var existingByWordId = existing.ToDictionary(w => w.WordId);
        var expectedWordIds = wordIds.ToHashSet();

        var toRemove = existing.Where(w => !expectedWordIds.Contains(w.WordId)).ToList();
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

    /// <summary>§5.2.1.4: upsert theo <c>key</c> (GIỮ NGUYÊN <c>id</c> câu cũ — lịch sử <c>quiz_attempts.answers</c> tham chiếu <c>questionId</c> cũ vẫn còn ý nghĩa), xoá câu có <c>key</c> không còn trong file. Dùng CHUNG cho cả bài MỚI (existing rỗng ⇒ mọi câu là "thêm mới") lẫn bài CẬP NHẬT.</summary>
    private async Task UpsertQuizQuestionsAsync(Guid lessonId, IReadOnlyList<LessonFileQuestion> questions, CancellationToken ct)
    {
        var existing = await db.QuizQuestions.Where(q => q.LessonId == lessonId).ToListAsync(ct);
        var existingByKey = existing.ToDictionary(q => q.Key, StringComparer.Ordinal);
        var expectedKeys = questions.Select(q => q.Key).ToHashSet(StringComparer.Ordinal);

        var toRemove = existing.Where(q => !expectedKeys.Contains(q.Key)).ToList();
        if (toRemove.Count > 0)
            db.QuizQuestions.RemoveRange(toRemove);

        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            var options = (q.Options ?? []).Select(o => new QuizOption(o.Id, o.Text, o.Lang)).ToList();
            var optionsJson = LessonJson.Serialize(options);
            var explanation = q.Explanation ?? "";

            if (existingByKey.TryGetValue(q.Key, out var existingQuestion))
                existingQuestion.Apply((short)i, q.Type, q.Prompt, q.PromptLang, q.PromptPinyin, q.AudioText, optionsJson, q.CorrectOptionId, explanation);
            else
                db.QuizQuestions.Add(QuizQuestion.Create(lessonId, q.Key, (short)i, q.Type, q.Prompt, q.PromptLang, q.PromptPinyin, q.AudioText, optionsJson, q.CorrectOptionId, explanation));
        }
    }

    private static void ApplyOutcome(ImportCounts counts, LessonImportOutcome outcome)
    {
        switch (outcome)
        {
            case LessonImportOutcome.Inserted: counts.Inserted++; break;
            case LessonImportOutcome.Updated: counts.Updated++; break;
            case LessonImportOutcome.Protected: counts.Protected++; break;
            case LessonImportOutcome.Unchanged:
            default: counts.Unchanged++; break;
        }
    }

    private static string ComputeHash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    private sealed class ImportCounts
    {
        public int Inserted;
        public int Updated;
        public int Unchanged;
        public int Invalid;
        public int Protected;
    }
}
