using AntFarm.Chinese.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Writing;

/// <summary>
/// Bộ chữ <c>hsk1</c> (R-W6) — chữ xuất hiện trong từ có <c>hsk3_level = 1</c>, thứ tự =
/// <c>min(path_order)</c> của các từ chứa chữ đó rồi <c>hanzi</c>. Dùng CHUNG giữa
/// <see cref="WritingCharacterQueryService"/> (danh sách chữ) và <see cref="WritingService"/>
/// (tổng số chữ trong tóm tắt) — tránh hai câu truy vấn lệch nhau cho cùng một khái niệm.
/// </summary>
internal static class Hsk1CharacterSet
{
    public static async Task<List<string>> GetOrderedHanziAsync(IChineseDbContext db, CancellationToken ct)
    {
        var rows = await (
            from wc in db.WordCharacters.AsNoTracking()
            join w in db.Words.AsNoTracking() on wc.WordId equals w.Id
            join c in db.Characters.AsNoTracking() on wc.CharacterId equals c.Id
            where w.Hsk3Level == 1
            select new { c.Hanzi, w.PathOrder }).ToListAsync(ct);

        return [.. rows
            .GroupBy(r => r.Hanzi, StringComparer.Ordinal)
            .Select(g => (Hanzi: g.Key, MinOrder: g.Min(x => x.PathOrder)))
            .OrderBy(x => x.MinOrder is null)
            .ThenBy(x => x.MinOrder)
            .ThenBy(x => x.Hanzi, StringComparer.Ordinal)
            .Select(x => x.Hanzi)];
    }
}
