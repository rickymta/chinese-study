using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Pinyin.Dtos;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Pinyin;

/// <summary>GET /api/pinyin/tone-stats (§6.1, R5-13). Bốn truy vấn LINQ (một mỗi thanh) ưu tiên dễ đọc hơn một câu SQL cửa sổ (§5.2.1 cho phép cả hai cách).</summary>
public sealed class ToneStatsService(IChineseDbContext db)
{
    public async Task<ToneStatsResponse> GetAsync(Guid userId, CancellationToken ct)
    {
        var windowRows = new List<ToneAnswerRow>();
        for (var tone = 1; tone <= 4; tone++)
        {
            var rows = await db.ToneDrillAnswers.AsNoTracking()
                .Where(a => a.UserId == userId && a.ExpectedTone == tone)
                .OrderByDescending(a => a.AnsweredAt)
                .ThenByDescending(a => a.PartIndex)
                .Take(ToneStatsCalculator.WindowSize)
                .Select(a => new ToneAnswerRow(a.ExpectedTone, a.AnsweredTone, a.IsCorrect))
                .ToListAsync(ct);

            windowRows.AddRange(rows);
        }

        var totalAnswered = await db.ToneDrillAnswers.AsNoTracking().CountAsync(a => a.UserId == userId, ct);
        var sessionsCount = await db.ToneDrillSessions.AsNoTracking().CountAsync(s => s.UserId == userId, ct);
        var lastSessionAt = await db.ToneDrillSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.FinishedAt)
            .Select(s => (DateTime?)s.FinishedAt)
            .FirstOrDefaultAsync(ct);

        return ToneStatsCalculator.Calculate(windowRows, totalAnswered, sessionsCount, lastSessionAt);
    }
}
