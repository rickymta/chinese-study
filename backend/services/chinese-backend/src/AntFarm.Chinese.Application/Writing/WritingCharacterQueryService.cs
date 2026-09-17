using AntFarm.Chinese.Application.Common.Abstractions;
using AntFarm.Chinese.Application.Writing.Dtos;
using AntFarm.Chinese.Domain.Content;
using AntFarm.Chinese.Domain.Learning;
using AntFarm.Chinese.Domain.Lessons;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Chinese.Application.Writing;

/// <summary><c>GET /api/writing/characters</c> + <c>GET /api/writing/characters/{hanzi}</c> (§5.2.2, §6.2, R-W6) — chỉ đọc.</summary>
public sealed class WritingCharacterQueryService(IChineseDbContext db)
{
    /// <summary>Bộ chữ theo <paramref name="set"/> (R-W6) — <c>lesson:&lt;slug&gt;</c> mà bài không tồn tại/không <c>published</c> ⇒ 404 (hình dạng <c>set</c> khác đã bị chặn ở <c>WritingCharactersQueryValidator</c>, 400).</summary>
    public async Task<WritingCharacterListResponseDto> ListAsync(Guid userId, string set, int page, int pageSize, CancellationToken ct)
    {
        var orderedHanzi = set switch
        {
            "hsk1" => await Hsk1CharacterSet.GetOrderedHanziAsync(db, ct),
            "weak" => await GetWeakOrderedHanziAsync(userId, ct),
            "practiced" => await GetPracticedOrderedHanziAsync(userId, ct),
            _ when set.StartsWith("lesson:", StringComparison.Ordinal) => await GetLessonOrderedHanziAsync(set["lesson:".Length..], ct),
            _ => throw new ArgumentException($"set '{set}' không hợp lệ (đã lẽ ra bị chặn ở validator).", nameof(set))
        };

        var totalCount = orderedHanzi.Count;
        var pageHanzi = orderedHanzi.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var characters = await db.Characters.AsNoTracking()
            .Where(c => pageHanzi.Contains(c.Hanzi))
            .ToDictionaryAsync(c => c.Hanzi, ct);
        var statsByHanzi = await db.CharacterWritingStats.AsNoTracking()
            .Where(s => s.UserId == userId && pageHanzi.Contains(s.Hanzi))
            .ToDictionaryAsync(s => s.Hanzi, ct);

        var items = pageHanzi
            .Where(characters.ContainsKey) // an toàn: bỏ qua nếu dữ liệu lệch (không nên xảy ra — mọi hanzi ở trên đều tra ra từ content.characters)
            .Select(h =>
            {
                var character = characters[h];
                var stats = statsByHanzi.GetValueOrDefault(h);
                return new WritingCharacterListItemDto(
                    character.Hanzi, character.PinyinReadings, character.HanViet, character.StrokeCount,
                    stats?.MasteryStatus ?? MasteryStatuses.New, stats?.LastMistakes, stats?.LastPracticedAt);
            })
            .ToList();

        return new WritingCharacterListResponseDto(set, items, page, pageSize, totalCount);
    }

    /// <summary><c>GET /api/writing/characters/{hanzi}</c> — <c>null</c> ⇒ controller trả 404 (cùng quy ước <c>DictionaryService.GetCharacterAsync</c>).</summary>
    public async Task<WritingCharacterDetailDto?> GetAsync(Guid userId, string hanzi, CancellationToken ct)
    {
        var character = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Hanzi == hanzi, ct);
        if (character is null)
            return null;

        var wordIds = await db.WordCharacters.AsNoTracking()
            .Where(wc => wc.CharacterId == character.Id)
            .Select(wc => wc.WordId)
            .ToListAsync(ct);
        var words = await db.Words.AsNoTracking().Where(w => wordIds.Contains(w.Id)).ToListAsync(ct);

        var wordDtos = words
            .OrderBy(w => w.PathOrder is null).ThenBy(w => w.PathOrder).ThenBy(w => w.Simplified, StringComparer.Ordinal)
            .Take(5)
            .Select(w => new WritingCharacterWordDto(w.Id, w.Simplified, w.Pinyin, w.MeaningsVi, w.MeaningViStatus, w.Hsk3Level))
            .ToList();

        var stats = await db.CharacterWritingStats.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Hanzi == hanzi, ct);

        return new WritingCharacterDetailDto(
            character.Hanzi, character.TraditionalVariants, character.PinyinReadings, character.HanViet,
            character.StrokeCount, character.Radical, wordDtos, stats is null ? null : WritingService.ToStatsDto(stats));
    }

    /// <summary>R-W6: chữ trong từ của bài (bài <c>published</c>), theo thứ tự từ trong bài rồi vị trí chữ trong từ, không trùng.</summary>
    private async Task<List<string>> GetLessonOrderedHanziAsync(string slug, CancellationToken ct)
    {
        var lesson = await db.Lessons.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Slug == slug && l.Status == LessonStatuses.Published, ct)
            ?? throw new NotFoundException($"Không tìm thấy bài học '{slug}'.");

        var lessonWords = await db.LessonWords.AsNoTracking()
            .Where(w => w.LessonId == lesson.Id)
            .OrderBy(w => w.OrderIndex)
            .ToListAsync(ct);
        var wordIds = lessonWords.Select(w => w.WordId).ToList();

        var wordCharacters = await db.WordCharacters.AsNoTracking()
            .Where(wc => wordIds.Contains(wc.WordId))
            .ToListAsync(ct);
        var characterIds = wordCharacters.Select(wc => wc.CharacterId).Distinct().ToList();
        var charactersById = await db.Characters.AsNoTracking()
            .Where(c => characterIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();
        foreach (var lessonWord in lessonWords)
        {
            var charsOfWord = wordCharacters.Where(wc => wc.WordId == lessonWord.WordId).OrderBy(wc => wc.Position);
            foreach (var wordCharacter in charsOfWord)
            {
                if (!charactersById.TryGetValue(wordCharacter.CharacterId, out var character))
                    continue;
                if (seen.Add(character.Hanzi))
                    ordered.Add(character.Hanzi);
            }
        }

        return ordered;
    }

    /// <summary>R-W5 "cần luyện" — sắp <c>last_practiced_at</c> TĂNG dần (lâu chưa luyện lên trước), tối đa 50.</summary>
    private async Task<List<string>> GetWeakOrderedHanziAsync(Guid userId, CancellationToken ct)
    {
        var stats = await db.CharacterWritingStats.AsNoTracking().Where(s => s.UserId == userId).ToListAsync(ct);
        return [.. stats.Where(s => s.IsWeak).OrderBy(s => s.LastPracticedAt).Take(50).Select(s => s.Hanzi)];
    }

    /// <summary>R-W6 "đã luyện" — chữ đã viết, sắp <c>last_practiced_at</c> GIẢM dần.</summary>
    private async Task<List<string>> GetPracticedOrderedHanziAsync(Guid userId, CancellationToken ct) =>
        await db.CharacterWritingStats.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastPracticedAt)
            .Select(s => s.Hanzi)
            .ToListAsync(ct);
}
