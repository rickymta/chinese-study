using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Infrastructure.Content.Files;
using AntFarm.Chinese.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Chinese.Infrastructure.Content;

/// <summary>
/// Nạp học liệu <c>content/chinese/data/{characters,vocabulary}</c> vào schema <c>content</c> lúc
/// khởi động (§5.2.4, R6-11) — idempotent theo <c>(SHA-256 tệp, Version)</c> qua bảng
/// <c>content.import_runs</c>; KHÔNG BAO GIỜ ném ra ngoài <c>ContentImportRunner</c> (bắt mọi lỗi,
/// ghi <c>import_runs.status='failed'</c>, log Error, trả <see cref="ImportSummary"/>).
/// </summary>
public sealed partial class ContentImporter(ChineseDbContext db, TimeProvider timeProvider, ILogger<ContentImporter> logger)
{
    /// <summary>Tăng khi đổi LOGIC nạp (không phải khi đổi dữ liệu) — buộc nạp lại dù hash tệp không đổi.</summary>
    public const int Version = 1;

    private const int AdvisoryLockKey = 727006;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    [GeneratedRegex("^[a-z]+[1-5]$")]
    private static partial Regex SyllableKeyPattern();

    [GeneratedRegex(@"^[A-Za-z]+[1-5]( [A-Za-z]+[1-5])*$")]
    private static partial Regex PinyinFullPattern();

    [GeneratedRegex(@"^(\p{IsCJKUnifiedIdeographs}|\p{IsCJKUnifiedIdeographsExtensionA}|〇)+$")]
    private static partial Regex HanziRunPattern();

    public Task<ImportSummary> ImportCharactersAsync(string path, string dataset, CancellationToken ct) =>
        RunAsync<CharactersFile>(path, dataset, ct, ImportCharactersCoreAsync);

    public Task<ImportSummary> ImportWordsAsync(string path, string dataset, CancellationToken ct) =>
        RunAsync<HskWordsFile>(path, dataset, ct, ImportWordsCoreAsync);

    private async Task<ImportSummary> RunAsync<TFile>(
        string path, string dataset, CancellationToken ct, Func<TFile, CancellationToken, Task<ImportCounts>> importAsync)
        where TFile : class
    {
        var startedAt = timeProvider.GetUtcNow().UtcDateTime;

        if (!File.Exists(path))
        {
            logger.LogWarning("Không thấy tệp học liệu '{Path}' (dataset {Dataset}) — bỏ qua nạp.", path, dataset);
            return ImportSummary.Skipped();
        }

        byte[] bytes;
        try
        {
            bytes = await File.ReadAllBytesAsync(path, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Đọc tệp học liệu '{Path}' (dataset {Dataset}) thất bại.", path, dataset);
            return ImportSummary.Failed(ex.Message);
        }

        var hash = ComputeHash(bytes);

        // Bọc try/catch RIÊNG (không để lỗi bay thẳng ra ContentImportRunner): lỗi đọc
        // content.import_runs của DATASET NÀY (vd characters) không được chặn dataset KHÁC
        // (hsk-words) chạy tiếp — ContentImportRunner gọi characters RỒI hsk-words, nếu ngoại lệ ở
        // đây thoát khỏi RunAsync thì lệnh await ImportWordsAsync() phía sau không bao giờ chạy
        // (review F6.2, §5.2.4 "characters lỗi vẫn thử hsk-words").
        ImportRun? lastSucceeded;
        try
        {
            lastSucceeded = await GetLastSucceededAsync(dataset, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Đọc content.import_runs (dataset {Dataset}) thất bại.", dataset);
            return await WriteFailedRunAsync(dataset, hash, startedAt, ex.Message, ct);
        }

        if (IsUpToDate(lastSucceeded, hash))
        {
            logger.LogInformation("Học liệu '{Dataset}' không đổi (hash khớp lượt nạp gần nhất) — bỏ qua.", dataset);
            return ImportSummary.Skipped();
        }

        TFile parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<TFile>(bytes, JsonOptions)
                ?? throw new InvalidDataException($"{dataset}: nội dung rỗng hoặc không parse được.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Nạp '{Dataset}' thất bại — JSON không hợp lệ.", dataset);
            return await WriteFailedRunAsync(dataset, hash, startedAt, ex.Message, ct);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // R-C8/§5.2.4: hai tiến trình khởi động cùng lúc không nạp chồng.
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({AdvisoryLockKey})", ct);

            var lastAfterLock = await GetLastSucceededAsync(dataset, ct);
            if (IsUpToDate(lastAfterLock, hash))
            {
                await transaction.RollbackAsync(ct);
                logger.LogInformation("Học liệu '{Dataset}' vừa được tiến trình khác nạp xong trong lúc chờ khoá — bỏ qua.", dataset);
                return ImportSummary.Skipped();
            }

            var counts = await importAsync(parsed, ct);
            var finishedAt = timeProvider.GetUtcNow().UtcDateTime;

            db.ImportRuns.Add(ImportRun.Succeeded(
                dataset, hash, Version, counts.Inserted, counts.Updated, counts.Unchanged, counts.Invalid, counts.Protected,
                startedAt, finishedAt));

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            logger.LogInformation(
                "Nạp {Dataset}: +{Inserted} ~{Updated} ={Unchanged} !{Invalid} khoá{Protected} ({ElapsedMs} ms)",
                dataset, counts.Inserted, counts.Updated, counts.Unchanged, counts.Invalid, counts.Protected,
                (finishedAt - startedAt).TotalMilliseconds);

            return ImportSummary.Succeeded(counts.Inserted, counts.Updated, counts.Unchanged, counts.Invalid, counts.Protected);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            logger.LogError(ex, "Nạp '{Dataset}' thất bại.", dataset);
            return await WriteFailedRunAsync(dataset, hash, startedAt, ex.Message, ct);
        }
    }

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
            // Ghi nhật ký thất bại cũng thất bại (vd DB mất kết nối) — vẫn KHÔNG ném, chỉ log thêm.
            logger.LogError(ex, "Ghi content.import_runs (dataset {Dataset}, trạng thái failed) thất bại.", dataset);
        }

        return ImportSummary.Failed(error);
    }

    private Task<ImportRun?> GetLastSucceededAsync(string dataset, CancellationToken ct) =>
        db.ImportRuns.AsNoTracking()
            .Where(r => r.Dataset == dataset && r.Status == "succeeded")
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync(ct);

    private bool IsUpToDate(ImportRun? last, string hash) => last is not null && last.FileHash == hash && last.ImporterVersion == Version;

    private static string ComputeHash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    // ---- characters ----

    private async Task<ImportCounts> ImportCharactersCoreAsync(CharactersFile file, CancellationToken ct)
    {
        var counts = new ImportCounts();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var existing = await db.Characters.ToDictionaryAsync(c => c.Hanzi, StringComparer.Ordinal, ct);
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in file.Characters ?? [])
        {
            if (!TryBuildCharacterImportData(entry, out var data, out var error))
            {
                counts.Invalid++;
                logger.LogWarning("characters › '{Hanzi}': {Error}", entry.Hanzi, error);
                continue;
            }

            seenKeys.Add(data!.Hanzi);

            if (existing.TryGetValue(data.Hanzi, out var character))
            {
                ApplyOutcome(counts, character.ApplyImport(data, now));
            }
            else
            {
                var created = Character.CreateFromImport(data, now);
                db.Characters.Add(created);
                existing[data.Hanzi] = created;
                counts.Inserted++;
            }
        }

        LogMissingKeys("characters", existing.Keys.Except(seenKeys, StringComparer.Ordinal));

        return counts;
    }

    private static bool TryBuildCharacterImportData(CharacterEntry entry, out CharacterImportData? data, out string? error)
    {
        data = null;

        if (string.IsNullOrEmpty(entry.Hanzi) || CountRunes(entry.Hanzi) != 1)
        {
            error = "hanzi phải đúng MỘT code point.";
            return false;
        }

        var readings = entry.PinyinReadings ?? [];
        if (readings.Count == 0 || readings.Any(r => !SyllableKeyPattern().IsMatch(r)))
        {
            error = "pinyinReadings rỗng hoặc có phần tử sai định dạng (^[a-z]+[1-5]$).";
            return false;
        }

        if (entry.HanVietStatus != HanVietStatus.Derived && entry.HanVietStatus != HanVietStatus.Reviewed)
        {
            error = $"hanVietStatus '{entry.HanVietStatus}' không hợp lệ.";
            return false;
        }

        if (entry.HanVietByPinyin is not null && entry.HanVietByPinyin.Keys.Any(k => !SyllableKeyPattern().IsMatch(k)))
        {
            error = "hanVietByPinyin có khoá sai định dạng.";
            return false;
        }

        if (entry.StrokeCount is < 1 or > 64)
        {
            error = "strokeCount ngoài khoảng 1..64.";
            return false;
        }

        if (entry.RadicalNumber is < 1 or > 214)
        {
            error = "radicalNumber ngoài khoảng 1..214.";
            return false;
        }

        data = new CharacterImportData(
            entry.Hanzi,
            entry.TraditionalVariants ?? [],
            readings,
            entry.HanViet ?? [],
            entry.HanVietByPinyin,
            entry.HanVietStatus,
            (short?)entry.StrokeCount,
            entry.Radical,
            (short?)entry.RadicalNumber,
            entry.Sources ?? []);
        error = null;
        return true;
    }

    // ---- hsk-words ----

    private async Task<ImportCounts> ImportWordsCoreAsync(HskWordsFile file, CancellationToken ct)
    {
        var counts = new ImportCounts();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var existingWords = await db.Words.ToDictionaryAsync(w => (w.Simplified, w.Pinyin), ct);
        var characterIds = await db.Characters.ToDictionaryAsync(c => c.Hanzi, c => c.Id, StringComparer.Ordinal, ct);
        var existingLinks = await db.WordCharacters.ToListAsync(ct);
        var linksByWord = existingLinks
            .GroupBy(l => l.WordId)
            .ToDictionary(g => g.Key, g => g.OrderBy(l => l.Position).ToList());

        var seenKeys = new HashSet<(string Simplified, string Pinyin)>();

        foreach (var entry in file.Words ?? [])
        {
            if (!TryBuildWordImportData(entry, out var data, out var error))
            {
                counts.Invalid++;
                logger.LogWarning("hsk-words › '{Simplified}' '{Pinyin}': {Error}", entry.Simplified, entry.Pinyin, error);
                continue;
            }

            var runes = EnumerateRuneStrings(data!.Simplified);
            var missingChar = runes.FirstOrDefault(r => !characterIds.ContainsKey(r));
            if (missingChar is not null)
            {
                counts.Invalid++;
                logger.LogWarning(
                    "hsk-words › '{Simplified}' '{Pinyin}': thiếu chữ '{Char}' trong content.characters — bỏ cả từ.",
                    data.Simplified, data.Pinyin, missingChar);
                continue;
            }

            var key = (data.Simplified, data.Pinyin);
            seenKeys.Add(key);

            Word word;
            if (existingWords.TryGetValue(key, out var found))
            {
                word = found;
                ApplyOutcome(counts, word.ApplyImport(data, now));
            }
            else
            {
                word = Word.CreateFromImport(data, now);
                db.Words.Add(word);
                existingWords[key] = word;
                counts.Inserted++;
            }

            SyncWordCharacters(word, runes, characterIds, linksByWord);
        }

        LogMissingKeys("hsk-words", existingWords.Keys.Except(seenKeys).Select(k => $"{k.Simplified} {k.Pinyin}"));

        return counts;
    }

    private void SyncWordCharacters(
        Word word, IReadOnlyList<string> runes, Dictionary<string, Guid> characterIds, Dictionary<Guid, List<WordCharacter>> linksByWord)
    {
        var expected = runes
            .Select((hanzi, index) => WordCharacter.Create(word.Id, (short)index, characterIds[hanzi]))
            .ToList();

        var current = linksByWord.TryGetValue(word.Id, out var list) ? list : [];

        var same = current.Count == expected.Count
            && current.Zip(expected).All(pair => pair.First.Position == pair.Second.Position && pair.First.CharacterId == pair.Second.CharacterId);
        if (same)
            return;

        if (current.Count > 0)
            db.WordCharacters.RemoveRange(current);

        db.WordCharacters.AddRange(expected);
        linksByWord[word.Id] = expected;
    }

    private static bool TryBuildWordImportData(HskWordEntry entry, out WordImportData? data, out string? error)
    {
        data = null;

        if (string.IsNullOrEmpty(entry.Simplified) || entry.Simplified.Length > 32 || !HanziRunPattern().IsMatch(entry.Simplified))
        {
            error = "simplified rỗng, quá dài, hoặc có ký tự không phải chữ Hán.";
            return false;
        }

        if (entry.Traditional is not null && (entry.Traditional.Length == 0 || entry.Traditional.Length > 32 || entry.Traditional == entry.Simplified))
        {
            error = "traditional không hợp lệ (rỗng, quá dài, hoặc trùng simplified).";
            return false;
        }

        if (string.IsNullOrEmpty(entry.Pinyin) || entry.Pinyin.Length > 128 || !PinyinFullPattern().IsMatch(entry.Pinyin))
        {
            error = "pinyin rỗng, quá dài, hoặc sai định dạng.";
            return false;
        }

        if (entry.Hsk3Level is < 1 or > 7) { error = "hsk3Level ngoài khoảng 1..7."; return false; }
        if (entry.Hsk2Level is < 1 or > 6) { error = "hsk2Level ngoài khoảng 1..6."; return false; }
        if (entry.HskExam2026Level is < 1 or > 7) { error = "hskExam2026Level ngoài khoảng 1..7."; return false; }

        var meaningsVi = entry.MeaningsVi ?? [];
        if (meaningsVi.Count == 0)
        {
            error = "meaningsVi rỗng.";
            return false;
        }

        if (entry.MeaningViStatus != MeaningViStatus.Machine && entry.MeaningViStatus != MeaningViStatus.Reviewed)
        {
            error = $"meaningViStatus '{entry.MeaningViStatus}' không hợp lệ.";
            return false;
        }

        if (entry.MeaningViSource != MeaningViSource.Cvdict && entry.MeaningViSource != MeaningViSource.Machine && entry.MeaningViSource != MeaningViSource.Manual)
        {
            error = $"meaningViSource '{entry.MeaningViSource}' không hợp lệ.";
            return false;
        }

        if ((entry.HanViet is null) != (entry.HanVietStatus is null))
        {
            error = "hanViet/hanVietStatus phải cùng null hoặc cùng có giá trị.";
            return false;
        }

        if (entry.HanVietStatus is not null && entry.HanVietStatus != HanVietStatus.Derived && entry.HanVietStatus != HanVietStatus.Reviewed)
        {
            error = $"hanVietStatus '{entry.HanVietStatus}' không hợp lệ.";
            return false;
        }

        data = new WordImportData(
            entry.Simplified,
            entry.Traditional,
            entry.Variants ?? [],
            entry.Pinyin,
            (short?)entry.Hsk3Level,
            (short?)entry.Hsk2Level,
            (short?)entry.HskExam2026Level,
            (short?)entry.OfficialIndex,
            entry.PathOrder,
            entry.FrequencyRank,
            entry.Pos ?? [],
            entry.UsageNote,
            entry.MeaningsEn ?? [],
            meaningsVi,
            entry.MeaningViStatus,
            entry.MeaningViSource,
            entry.HanViet,
            entry.HanVietStatus,
            entry.Sources ?? []);
        error = null;
        return true;
    }

    private static void ApplyOutcome(ImportCounts counts, ImportOutcome outcome)
    {
        switch (outcome)
        {
            case ImportOutcome.UpdatedProtected:
                counts.Updated++;
                counts.Protected++;
                break;
            case ImportOutcome.Updated:
                counts.Updated++;
                break;
            case ImportOutcome.Unchanged:
            case ImportOutcome.Inserted: // không xảy ra ở đây — ApplyImport chỉ gọi trên dòng đã tồn tại
            default:
                counts.Unchanged++;
                break;
        }
    }

    private void LogMissingKeys(string dataset, IEnumerable<string> missingKeys)
    {
        var all = missingKeys.ToList();
        if (all.Count == 0)
            return;

        logger.LogWarning(
            "Nạp {Dataset}: {Count} dòng có trong DB nhưng KHÔNG còn trong tệp (giữ nguyên, không xoá — có thể đang có thẻ SRS) — vd: {Keys}",
            dataset, all.Count, string.Join(", ", all.Take(20)));
    }

    private static int CountRunes(string s) => s.EnumerateRunes().Count();

    private static List<string> EnumerateRuneStrings(string s) => [.. s.EnumerateRunes().Select(r => r.ToString())];

    private sealed class ImportCounts
    {
        public int Inserted;
        public int Updated;
        public int Unchanged;
        public int Invalid;
        public int Protected;
    }
}
