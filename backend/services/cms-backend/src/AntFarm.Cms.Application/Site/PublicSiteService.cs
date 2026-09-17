using AntFarm.Cms.Application.Common.Abstractions;
using AntFarm.Cms.Application.Site.Dtos;
using AntFarm.Cms.Domain.Site;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Cms.Application.Site;

/// <summary>
/// Dữ liệu nền website công khai (§5.2.3, §6.2 W3a) — <c>GET /api/public/site</c>, ẩn danh. W3a:
/// <c>faqs</c> luôn rỗng (W3b điền), <c>ogImageUrl</c>/<c>coverUrl</c> luôn null (W4 điền ảnh).
/// </summary>
public sealed class PublicSiteService(ICmsDbContext db)
{
    public async Task<PublicSiteDto> GetAsync(CancellationToken ct)
    {
        var settings = await db.SiteSettings.AsNoTracking().ToListAsync(ct);
        var values = settings
            .Where(s => !string.IsNullOrEmpty(s.Value))
            .ToDictionary(s => s.Key, s => s.Value);

        var languages = await db.Languages.AsNoTracking()
            .Where(l => l.Status != LanguageStatus.Hidden)
            .OrderBy(l => l.SortOrder)
            .ToListAsync(ct);

        var languageDtos = languages
            .Select(l => new PublicLanguageDto(
                l.Code, l.Name, l.NativeName, l.Tagline, l.DescriptionMarkdown,
                LanguageStatuses.ToCode(l.Status), l.AppUrl, l.AccentColor, null))
            .ToList();

        return new PublicSiteDto(values, null, languageDtos, []);
    }
}
