using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Domain.Srs;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Srs;

/// <inheritdoc cref="ISrsCardService"/>
public sealed class SrsCardService(IChineseDbContext db, TimeProvider timeProvider) : ISrsCardService
{
    public async Task<int> EnsureCardsAsync(Guid userId, IReadOnlyList<Guid> wordIds, string source, CancellationToken ct)
    {
        var distinctWordIds = wordIds.Distinct().ToList();
        if (distinctWordIds.Count == 0)
            return 0;

        // Lọc trước bằng MỘT SELECT (rẻ) — trường hợp phổ biến "gọi lại hàng đợi, mọi thẻ đã có"
        // không phải trả giá round-trip INSERT cho từng từ (§5.2.9 test 1: gọi 2 lần không nhân đôi).
        var existingWordIds = await db.SrsCards.AsNoTracking()
            .Where(c => c.UserId == userId && distinctWordIds.Contains(c.WordId))
            .Select(c => c.WordId)
            .ToListAsync(ct);
        var existingSet = existingWordIds.ToHashSet();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var inserted = 0;
        foreach (var wordId in distinctWordIds)
        {
            if (existingSet.Contains(wordId))
                continue;

            var (_, created) = await InsertNewCardIfMissingAsync(userId, wordId, source, now, ct);
            if (created)
                inserted++;
        }

        return inserted;
    }

    public async Task<AddCardsResultDto> AddAsync(Guid userId, AddCardsCommand command, CancellationToken ct)
    {
        var distinctWordIds = command.WordIds.Distinct().ToList();

        var existingWordIds = await db.Words.AsNoTracking()
            .Where(w => distinctWordIds.Contains(w.Id))
            .Select(w => w.Id)
            .ToListAsync(ct);
        var missingWordIds = distinctWordIds.Except(existingWordIds).ToList();
        if (missingWordIds.Count > 0)
            throw new BusinessRuleException("UNKNOWN_WORD", "Một số từ không tồn tại.", new { wordIds = missingWordIds });

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var results = new List<AddCardResultItem>(distinctWordIds.Count);
        var added = 0;

        // Chèn TỪNG TỪ qua INSERT ... ON CONFLICT DO NOTHING (không Add()+SaveChanges gộp cuối) —
        // hai tab cùng bấm "thêm vào ôn tập" cho CÙNG từ gần như đồng thời trước đây ném 23505 ⇒
        // 500 (review F7.1); giờ tab thua tự đọc lại đúng cardId của tab thắng, cả hai vẫn 201.
        foreach (var wordId in distinctWordIds)
        {
            var (cardId, created) = await InsertNewCardIfMissingAsync(userId, wordId, SrsCardSources.Manual, now, ct);
            results.Add(new AddCardResultItem(wordId, cardId, created));
            if (created)
                added++;
        }

        return new AddCardsResultDto(added, results.Count - added, results);
    }

    public async Task<SrsCardDto> SetSuspendedAsync(Guid userId, Guid cardId, bool suspended, CancellationToken ct)
    {
        var card = await db.SrsCards.FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId, ct)
            ?? throw new NotFoundException($"Không tìm thấy thẻ '{cardId}'.");

        card.SetSuspended(suspended, timeProvider.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);

        return ToDto(card);
    }

    /// <summary>
    /// Chèn một thẻ <c>new</c> nếu (user_id, word_id, card_type) CHƯA có — ghi NGAY qua SQL trực
    /// tiếp (không qua ChangeTracker/SaveChangesAsync) để dùng được <c>ON CONFLICT DO NOTHING</c>:
    /// hai request đua nhau tạo CÙNG một thẻ (2 tab GET queue song song cho người mới, hoặc F9 hoàn
    /// thành bài học đua với hàng đợi lộ trình) tự nhường nhau thay vì ném <c>23505</c> ⇒ 500 (bắt
    /// bằng test <c>SrsQueueTests</c> §5.2.9, review F7.1 17/09/2026). Tự tham gia transaction hiện
    /// tại nếu người gọi đã <see cref="IChineseDbContext.BeginTransactionAsync"/> trên CÙNG DbContext
    /// (F9 ghi thẻ + study_events + lesson_progress trong một transaction, K12) — PostgreSQL tự
    /// khoá đúng dòng đang tranh chấp cho tới khi giao dịch kia commit/rollback, không có cửa sổ hở.
    /// Card_type/reps/lapses dùng DEFAULT của cột (§5.1.2) — không liệt kê ở đây.
    /// </summary>
    private async Task<(Guid CardId, bool Created)> InsertNewCardIfMissingAsync(
        Guid userId, Guid wordId, string source, DateTime nowUtc, CancellationToken ct)
    {
        var candidateId = Guid.CreateVersion7();
        var insertedRows = await db.ExecuteSqlAsync($"""
            INSERT INTO learning.srs_cards (id, user_id, word_id, state, due_at, is_suspended, source, created_at, updated_at)
            VALUES ({candidateId}, {userId}, {wordId}, {SrsStateCodes.New}, {nowUtc}, false, {source}, {nowUtc}, {nowUtc})
            ON CONFLICT (user_id, word_id, card_type) DO NOTHING
            """, ct);

        if (insertedRows == 1)
            return (candidateId, true);

        // Thua cuộc đua (0 dòng ảnh hưởng) — đọc lại id THẬT của thẻ người thắng vừa tạo. Postgres
        // đã CHẶN câu INSERT phía trên tới khi giao dịch của người thắng commit/rollback (khoá theo
        // chỉ mục duy nhất) nên tại đây dòng LUÔN tồn tại (trừ khi người thắng rollback — khi đó
        // insertedRows lẽ ra phải là 1 vì hàng không còn tranh chấp; SingleAsync sẽ tự lộ lỗi logic nếu có).
        var existingId = await db.SrsCards.AsNoTracking()
            .Where(c => c.UserId == userId && c.WordId == wordId)
            .Select(c => c.Id)
            .SingleAsync(ct);
        return (existingId, false);
    }

    internal static SrsCardDto ToDto(SrsCard card) => new(
        card.Id, card.State, card.Step, card.DueAt,
        card.Stability, card.Difficulty, card.Reps, card.Lapses, card.LastReviewAt, card.IsSuspended);
}
