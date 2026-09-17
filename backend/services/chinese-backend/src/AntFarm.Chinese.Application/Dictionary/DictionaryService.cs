using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Srs;
using AntFarm.Chinese.Domain.Text;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AntFarm.Chinese.Application.Dictionary;

/// <summary>
/// Tra từ (§5.2.3, §6.1). Kho từ F6 chỉ có 500 mục HSK 3.0 cấp 1 — ĐỦ NHỎ để tải toàn bộ (đã lọc
/// theo <c>hsk</c> nếu có) vào bộ nhớ rồi tính hạng/khớp bằng C# thay vì dịch biểu thức hạng phức
/// tạp (§5.2.3) sang một câu LINQ-to-Entities duy nhất — tránh rủi ro dịch SQL sai mà vẫn cho kết
/// quả giống hệt. GHI CHÚ MỞ RỘNG: khi kho từ lớn hơn nhiều (HSK 2–9, ~11.000 từ — ngoài phạm vi
/// F6), nên đẩy lọc xuống SQL bằng các chỉ mục trgm/pattern đã có sẵn trong migration (§5.1.1) thay
/// vì tải hết vào bộ nhớ.
/// </summary>
public sealed class DictionaryService(IChineseDbContext db, DictionaryQueryParser parser, IMemoryCache cache)
{
    private const string WordsCacheKeyPrefix = "dictionary:words-by-run:";

    public async Task<DictionarySearchResultDto> SearchAsync(DictionaryQuery query, CancellationToken ct)
    {
        // Kiểm content.words rỗng TRƯỚC khi lọc hsk (§6.1, review F6.2) — học liệu chưa nạp xong
        // (hoặc nạp lỗi lúc khởi động) phải báo RÕ 503, không trả 200 rỗng khiến người dùng tưởng
        // "không tìm thấy".
        var allWords = await GetAllWordsCachedAsync(ct);

        var parsed = parser.Parse(query.Q);

        IEnumerable<Word> words = allWords;
        if (query.Hsk is { } hsk)
            words = words.Where(w => w.Hsk3Level == hsk);

        List<(Word Word, int Rank, string MatchKind)> matches;
        if (parsed.Kind == DictionaryQueryKind.Empty)
        {
            matches = [.. words.Select(w => (w, 0, "browse"))];
        }
        else
        {
            matches = [];
            foreach (var word in words)
            {
                var rank = ComputeRank(word, parsed, out var matchKind);
                if (rank >= 0)
                    matches.Add((word, rank, matchKind));
            }
        }

        var ordered = matches
            .OrderBy(m => m.Rank)
            .ThenBy(m => m.Word.Hsk3Level is null)
            .ThenBy(m => m.Word.Hsk3Level)
            .ThenBy(m => m.Word.PathOrder is null)
            .ThenBy(m => m.Word.PathOrder)
            .ThenBy(m => m.Word.FrequencyRank is null)
            .ThenBy(m => m.Word.FrequencyRank)
            .ThenBy(m => m.Word.Simplified, StringComparer.Ordinal)
            .ToList();

        var totalCount = ordered.Count;
        var page = ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(m => ToSearchItem(m.Word, m.MatchKind))
            .ToList();

        return new DictionarySearchResultDto(page, query.Page, query.PageSize, totalCount);
    }

    public async Task<WordDetailDto?> GetWordAsync(Guid id, Guid? userId, CancellationToken ct)
    {
        await EnsureContentLoadedAsync(ct);

        var word = await db.Words.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct);
        if (word is null)
            return null;

        var characters = await LoadWordCharactersAsync(id, ct);
        var srs = userId is null ? null : await LoadWordSrsAsync(userId.Value, id, ct);

        return new WordDetailDto(
            word.Id, word.Simplified, word.Traditional, word.Variants, word.Pinyin,
            word.Hsk3Level, word.Hsk2Level, word.HskExam2026Level, word.OfficialIndex, word.PathOrder, word.FrequencyRank,
            word.Pos, word.UsageNote, word.MeaningsEn, word.MeaningsVi,
            word.MeaningViStatus, word.MeaningViSource, word.HanViet, word.HanVietStatus,
            word.Sources, characters,
            srs);
    }

    /// <summary>F7: khối SRS của NGƯỜI ĐANG GỌI trong chi tiết từ (§6.1/§6.2) — <c>null</c> khi chưa đăng nhập (không nên xảy ra, mọi endpoint yêu cầu <c>study.use</c>) hoặc chưa có thẻ cho từ này.</summary>
    private async Task<WordSrsSummaryDto?> LoadWordSrsAsync(Guid userId, Guid wordId, CancellationToken ct)
    {
        var card = await db.SrsCards.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId && c.WordId == wordId, ct);
        return card is null ? null : new WordSrsSummaryDto(card.Id, SrsStateCodes.ToCode(card.State), card.DueAt, card.IsSuspended);
    }

    public async Task<CharacterDetailDto?> GetCharacterAsync(string hanzi, CancellationToken ct)
    {
        await EnsureContentLoadedAsync(ct);

        var character = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Hanzi == hanzi, ct);
        if (character is null)
            return null;

        var wordIds = await db.WordCharacters.AsNoTracking()
            .Where(wc => wc.CharacterId == character.Id)
            .Select(wc => wc.WordId)
            .ToListAsync(ct);

        var words = await db.Words.AsNoTracking()
            .Where(w => wordIds.Contains(w.Id))
            .ToListAsync(ct);

        var items = words
            .OrderBy(w => w.PathOrder is null)
            .ThenBy(w => w.PathOrder)
            .ThenBy(w => w.Simplified, StringComparer.Ordinal)
            .Take(20)
            .Select(w => ToSearchItem(w, "hanzi"))
            .ToList();

        return new CharacterDetailDto(
            character.Hanzi, character.TraditionalVariants, character.PinyinReadings, character.HanViet,
            character.HanVietByPinyin, character.HanVietStatus, character.StrokeCount, character.Radical, character.RadicalNumber,
            items);
    }

    /// <summary>content.words rỗng ⇒ học liệu chưa nạp xong (hoặc nạp lỗi lúc khởi động) — 503, KHÔNG trả kết quả rỗng như "không tìm thấy" (review F6.2, §6.1).</summary>
    private async Task EnsureContentLoadedAsync(CancellationToken ct)
    {
        if (!await db.Words.AsNoTracking().AnyAsync(ct))
            throw new ServiceUnavailableException("CONTENT_UNAVAILABLE", "Học liệu chưa được nạp — kiểm log khởi động.");
    }

    /// <summary>
    /// Cache trong tiến trình toàn bộ <c>content.words</c> (chỉ ~500 dòng) — khoá theo id lượt nạp
    /// <c>hsk-words</c> THÀNH CÔNG gần nhất (<c>content.import_runs</c>): nạp lại học liệu (khởi
    /// động lại, hoặc F10 sửa xong rồi nạp lại) tạo run mới ⇒ khoá đổi ⇒ tự lấy dữ liệu mới, không
    /// cần thời hạn hết hạn thủ công. Vẫn kiểm rỗng TRƯỚC (xem <see cref="EnsureContentLoadedAsync"/>).
    /// </summary>
    private async Task<IReadOnlyList<Word>> GetAllWordsCachedAsync(CancellationToken ct)
    {
        await EnsureContentLoadedAsync(ct);

        var latestRunId = await db.ImportRuns.AsNoTracking()
            .Where(r => r.Dataset == "hsk-words" && r.Status == "succeeded")
            .OrderByDescending(r => r.StartedAt)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(ct);

        var cacheKey = WordsCacheKeyPrefix + (latestRunId?.ToString() ?? "unknown");

        if (cache.TryGetValue(cacheKey, out IReadOnlyList<Word>? cached) && cached is not null)
            return cached;

        var words = await db.Words.AsNoTracking().ToListAsync(ct);
        // Thời hạn dự phòng (dù khoá đã tự đổi khi có run mới) — tránh giữ mãi nếu vì lý do nào đó
        // không có bản ghi import_runs (vd DB nạp thủ công ngoài ContentImporter lúc phát triển).
        cache.Set(cacheKey, (IReadOnlyList<Word>)words, TimeSpan.FromHours(1));
        return words;
    }

    private async Task<List<WordCharacterSummaryDto>> LoadWordCharactersAsync(Guid wordId, CancellationToken ct)
    {
        var links = await db.WordCharacters.AsNoTracking()
            .Where(wc => wc.WordId == wordId)
            .OrderBy(wc => wc.Position)
            .ToListAsync(ct);

        if (links.Count == 0)
            return [];

        var characterIds = links.Select(l => l.CharacterId).ToList();
        var characters = await db.Characters.AsNoTracking()
            .Where(c => characterIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        return links
            .Select(l => characters.TryGetValue(l.CharacterId, out var c)
                ? new WordCharacterSummaryDto(c.Hanzi, c.PinyinReadings, c.HanViet, c.StrokeCount)
                : null)
            .Where(dto => dto is not null)
            .Select(dto => dto!)
            .ToList();
    }

    private static DictionarySearchItemDto ToSearchItem(Word w, string matchKind) => new(
        w.Id, w.Simplified, w.Traditional, w.Pinyin, w.Hsk3Level, w.Hsk2Level, w.HanViet,
        w.MeaningsVi.Take(3).ToList(), w.MeaningViStatus, matchKind);

    /// <summary>Hạng theo §5.2.3 (0 tốt nhất; -1 = không khớp) — một từ khớp nhiều điều kiện lấy hạng NHỎ NHẤT.</summary>
    private static int ComputeRank(Word w, ParsedDictionaryQuery p, out string matchKind)
    {
        matchKind = "";

        if (p.Kind == DictionaryQueryKind.Hanzi)
        {
            var h = p.HanziText!;
            if (w.Simplified == h || w.Traditional == h || w.Variants.Contains(h, StringComparer.Ordinal))
                return Set(ref matchKind, 0, "hanzi");
            if (w.Simplified.StartsWith(h, StringComparison.Ordinal) || (w.Traditional?.StartsWith(h, StringComparison.Ordinal) ?? false))
                return Set(ref matchKind, 3, "hanzi");
            if (w.Simplified.Contains(h, StringComparison.Ordinal) || (w.Traditional?.Contains(h, StringComparison.Ordinal) ?? false))
                return Set(ref matchKind, 7, "hanzi");

            return -1;
        }

        var best = -1;

        if (p.PinyinCompact is { } c)
        {
            if (w.PinyinCompact == c)
                best = Better(best, 1, ref matchKind, "pinyin");
            else if (c.Length >= 2 && w.PinyinCompact.StartsWith(c, StringComparison.Ordinal))
                best = Better(best, 4, ref matchKind, "pinyin");
        }

        if (p.PinyinToneless is { } t)
        {
            if (w.PinyinSearch == t)
                best = Better(best, 2, ref matchKind, "pinyin");
            else if (t.Length >= 2 && w.PinyinSearch.StartsWith(t, StringComparison.Ordinal))
                best = Better(best, 4, ref matchKind, "pinyin");
        }

        if (p.ViText is not null)
        {
            var v = p.ViHasDiacritics ? p.ViText : p.ViPlain!;
            var col = p.ViHasDiacritics ? w.SearchVi : w.SearchViPlain;
            // So Hán Việt bằng CÙNG hàm chuẩn hoá dùng để dựng search_vi/ViText (NormalizeForSearch)
            // thay vì so thẳng w.HanViet — han_viet lưu "chữ thường, cách đơn" theo quy ước, nhưng
            // so trực tiếp không chịu được khoảng trắng thừa/khác NFC — review F6.2.
            var hv = p.ViHasDiacritics
                ? (w.HanViet is null ? null : VietnameseText.NormalizeForSearch(w.HanViet))
                : w.HanVietPlain;

            if (hv is not null && string.Equals(hv, v, StringComparison.Ordinal))
                best = Better(best, 5, ref matchKind, "han_viet");
            if (col.Contains("| " + v + " |", StringComparison.Ordinal))
                best = Better(best, 6, ref matchKind, "meaning");
            if (v.Length >= 2 && col.Contains(" " + v, StringComparison.Ordinal))
                best = Better(best, 8, ref matchKind, "meaning");
        }

        return best;
    }

    private static int Set(ref string matchKind, int rank, string kind)
    {
        matchKind = kind;
        return rank;
    }

    private static int Better(int currentBest, int candidateRank, ref string matchKind, string candidateMatchKind)
    {
        if (currentBest == -1 || candidateRank < currentBest)
        {
            matchKind = candidateMatchKind;
            return candidateRank;
        }

        return currentBest;
    }
}
