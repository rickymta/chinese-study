using AntFarm.Chinese.Application.Admin.Content.Dtos;
using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Dictionary;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Text;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Chinese.Application.Admin.Content;

/// <summary>
/// Duyệt nghĩa Việt/Hán Việt của từ vựng (§5.2.3 "Duyệt từ", R-CA9/R-CA11). Kho từ chỉ ~500 dòng
/// (F6, HSK 3.0 cấp 1) — lọc theo <c>hsk</c>/trạng thái ở SQL rồi lọc <c>q</c> trong bộ nhớ (cùng lý
/// do <c>DictionaryService</c>, §5.2.3 review). Mọi thao tác ghi thành công đều <c>LogInformation</c>
/// (ai, từ nào, làm gì) — vết audit tối thiểu cho học liệu (review điều phối 17/09/2026, mục 6).
/// </summary>
public sealed class WordReviewService(
    IChineseDbContext db, TimeProvider timeProvider, DictionaryCacheVersion cacheVersion, ILogger<WordReviewService> logger)
{
    public async Task<AdminWordsPageDto> ListAsync(AdminWordsQuery query, CancellationToken ct)
    {
        var wordsQuery = db.Words.AsNoTracking().AsQueryable();

        if (query.MeaningViStatus is { } mvs)
            wordsQuery = wordsQuery.Where(w => w.MeaningViStatus == mvs);
        if (query.HanVietStatus is { } hvs)
            wordsQuery = wordsQuery.Where(w => w.HanVietStatus == hvs);

        // R-CA11/§5.2.3 "Danh sách từ": hsk KHÔNG truyền ⇒ mặc định 1.
        var hsk = query.Hsk ?? 1;
        wordsQuery = wordsQuery.Where(w => w.Hsk3Level == hsk);

        var words = await wordsQuery.ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var qToneless = WordSearchKeys.PinyinToneless(query.Q);
            var qPlain = VietnameseText.RemoveDiacritics(VietnameseText.NormalizeForSearch(query.Q));
            words = [.. words.Where(w =>
                w.Simplified.Contains(query.Q, StringComparison.Ordinal) ||
                (w.Traditional?.Contains(query.Q, StringComparison.Ordinal) ?? false) ||
                w.PinyinSearch.Contains(qToneless, StringComparison.Ordinal) ||
                w.SearchViPlain.Contains(qPlain, StringComparison.Ordinal))];
        }

        // R-CA11: từ sắp học lên trước (path_order tăng dần), NULL (chưa vào lộ trình) xuống cuối.
        var ordered = words
            .OrderBy(w => w.PathOrder is null).ThenBy(w => w.PathOrder)
            .ThenBy(w => w.Simplified, StringComparer.Ordinal)
            .ToList();

        var totalCount = ordered.Count;
        var page = ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();

        var editedByIds = page.Where(w => w.EditedBy is not null).Select(w => w.EditedBy!.Value).Distinct().ToList();
        var namesByUserId = await db.Users.AsNoTracking()
            .Where(u => editedByIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        var items = page.Select(w => ToDto(w, namesByUserId)).ToList();
        return new AdminWordsPageDto(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<AdminWordDto> GetAsync(Guid id, CancellationToken ct)
    {
        var word = await db.Words.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy từ.");
        return await ToDtoAsync(word, ct);
    }

    /// <summary>
    /// R-CA9: chuẩn hoá <c>meaningsVi</c> (trim, bỏ rỗng, bỏ trùng giữ thứ tự) RỒI mới so với DB để
    /// <see cref="Word.ApplyReview"/> quyết <c>meaning_vi_source</c>; luôn tính lại khoá tìm kiếm
    /// (bên trong <see cref="Word.ApplyReview"/>) và làm mới cache từ điển (R-CA9 "cache được làm mới").
    /// </summary>
    public async Task<AdminWordDto> UpdateAsync(Guid id, UpdateWordRequest request, Guid userId, CancellationToken ct)
    {
        var word = await db.Words.FirstOrDefaultAsync(w => w.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy từ.");
        db.SetOriginalVersion(word, request.Version);

        var normalizedMeanings = NormalizeMeanings(request.MeaningsVi);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        word.ApplyReview(normalizedMeanings, request.MeaningViStatus, request.HanViet, request.HanVietStatus, userId, now);

        await SaveWithConcurrencyAsync(ct);
        cacheVersion.Invalidate();

        logger.LogInformation(
            "Admin {UserId} sửa nghĩa/Hán Việt từ {WordId} ('{Simplified}') — meaningViStatus={MeaningViStatus}, hanVietStatus={HanVietStatus}.",
            userId, id, word.Simplified, word.MeaningViStatus, word.HanVietStatus);
        return await ToDtoAsync(word, ct);
    }

    /// <summary>
    /// R-CA9 "Duyệt hàng loạt": chỉ đổi <c>meaning_vi_status</c> sang <c>reviewed</c> (giữ NGUYÊN nội
    /// dung/<c>meaning_vi_source</c>/Hán Việt) — xử lý TỪNG MỤC độc lập (một xung đột không huỷ cả
    /// lô, chấp nhận không nguyên tử theo §5.2.3).
    /// </summary>
    public async Task<BulkReviewResultDto> BulkReviewAsync(BulkReviewWordsRequest request, Guid userId, CancellationToken ct)
    {
        var updated = 0;
        var conflicts = new List<Guid>();
        var notFound = new List<Guid>();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var item in request.Items)
        {
            db.ClearTracking(); // mỗi mục độc lập — bỏ entity/lỗi đồng bộ còn dính từ vòng trước

            var word = await db.Words.FirstOrDefaultAsync(w => w.Id == item.Id, ct);
            if (word is null)
            {
                notFound.Add(item.Id);
                continue;
            }

            db.SetOriginalVersion(word, item.Version);
            word.ApplyReview(word.MeaningsVi, MeaningViStatus.Reviewed, word.HanViet, word.HanVietStatus ?? HanVietStatus.Derived, userId, now);

            try
            {
                await db.SaveChangesAsync(ct);
                updated++;
            }
            catch (DbUpdateConcurrencyException)
            {
                conflicts.Add(item.Id);
            }
        }

        if (updated > 0)
            cacheVersion.Invalidate();

        logger.LogInformation(
            "Admin {UserId} duyệt hàng loạt {ItemCount} từ: updated={Updated}, conflicts={ConflictCount}, notFound={NotFoundCount}.",
            userId, request.Items.Count, updated, conflicts.Count, notFound.Count);
        return new BulkReviewResultDto(updated, conflicts, notFound);
    }

    private async Task SaveWithConcurrencyAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("CONCURRENCY_CONFLICT", "Từ đã bị sửa ở nơi khác — hãy tải lại.");
        }
    }

    private async Task<AdminWordDto> ToDtoAsync(Word word, CancellationToken ct)
    {
        var editedByName = word.EditedBy is { } editorId
            ? await db.Users.AsNoTracking().Where(u => u.Id == editorId).Select(u => (string?)u.DisplayName).FirstOrDefaultAsync(ct)
            : null;
        return ToDto(word, editedByName);
    }

    private static AdminWordDto ToDto(Word word, IReadOnlyDictionary<Guid, string> namesByUserId) =>
        ToDto(word, word.EditedBy is { } id && namesByUserId.TryGetValue(id, out var name) ? name : null);

    private static AdminWordDto ToDto(Word word, string? editedByName) => new(
        word.Id, word.Version, word.Simplified, word.Traditional, word.Pinyin,
        word.Hsk3Level, word.PathOrder, word.Pos, word.MeaningsEn,
        word.MeaningsVi, word.MeaningViStatus, word.MeaningViSource,
        word.HanViet, word.HanVietStatus, word.EditedAt, editedByName);

    /// <summary>R-CA9: trim, bỏ mục rỗng, bỏ trùng NHƯNG GIỮ THỨ TỰ xuất hiện đầu tiên.</summary>
    private static List<string> NormalizeMeanings(IReadOnlyList<string> raw) =>
        [.. raw.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.Ordinal)];
}
