using AntFarm.Chinese.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.ApiTests.Infrastructure;

/// <summary>Tiện ích dựng dữ liệu SRS trực tiếp trên DB cho ApiTests (§5.2.9) — chèn thẳng bằng SQL để đặt tuỳ ý <c>due_at</c>/<c>state</c>/<c>is_suspended</c> mà không phải lách qua máy trạng thái FSRS thật.</summary>
internal static class SrsTestData
{
    /// <summary>500 từ HSK1 thật đã nạp bởi <c>ContentImportRunner</c> — lấy theo <c>path_order</c> tăng dần (R6-9).</summary>
    public static Task<List<Guid>> GetPathWordIdsAsync(ChineseDbContext db, int count, int skip = 0) =>
        db.Words.AsNoTracking()
            .Where(w => w.Hsk3Level == 1 && w.PathOrder != null)
            .OrderBy(w => w.PathOrder)
            .Skip(skip)
            .Take(count)
            .Select(w => w.Id)
            .ToListAsync();

    /// <summary>Chèn thẳng một dòng learning.srs_cards — dùng cho fixture cần trạng thái/mốc hạn cụ thể (state review/learning/relearning, is_suspended...) mà không cần chạy đủ chuỗi chấm thẻ thật.</summary>
    public static async Task<Guid> InsertCardAsync(
        ChineseDbContext db, Guid userId, Guid wordId, string state, DateTime dueAtUtc,
        double? stability = null, double? difficulty = null, bool isSuspended = false,
        string source = "manual", DateTime? createdAtUtc = null, DateOnly? firstReviewedLocalDate = null)
    {
        var id = Guid.CreateVersion7();
        var createdAt = createdAtUtc ?? dueAtUtc;

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO learning.srs_cards
                (id, user_id, word_id, card_type, state, step, due_at, stability, difficulty, reps, lapses,
                 last_review_at, first_reviewed_at, first_reviewed_local_date, is_suspended, source, created_at, updated_at)
            VALUES
                ({id}, {userId}, {wordId}, 'hanzi_to_meaning', {state}, NULL, {dueAtUtc}, {stability}, {difficulty}, 0, 0,
                 NULL, NULL, {firstReviewedLocalDate}, {isSuspended}, {source}, {createdAt}, {createdAt})
            """);

        return id;
    }
}
