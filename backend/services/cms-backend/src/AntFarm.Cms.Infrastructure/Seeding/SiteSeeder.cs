using AntFarm.Cms.Application.Common.Options;
using AntFarm.Cms.Domain.Site;
using AntFarm.Cms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AntFarm.Cms.Infrastructure.Seeding;

/// <summary>
/// Chèn bù cấu hình site/SEO còn thiếu + gieo danh mục ngôn ngữ (§5.2.3, R-W25) lúc KHỞI ĐỘNG,
/// gọi từ Program.cs SAU <see cref="AccessSeeder"/> (audit_logs của W3a cần <c>access.users</c> đã
/// có nếu ghi nhật ký seed — hiện chưa ghi, phòng hờ thứ tự cho tương lai). BẮT MỌI exception, log
/// Error, KHÔNG ném (seed hỏng không được làm service chết, CLAUDE.md mục "Seed").
/// </summary>
public static class SiteSeeder
{
    private static readonly CmsSeedOptions.LanguageSeedItem[] DefaultLanguages =
    [
        new() { Code = "chinese", Name = "Tiếng Trung", NativeName = "中文", Status = "open", AppUrl = "https://chinese.antfarms.xyz", SortOrder = 1 },
        new() { Code = "english", Name = "Tiếng Anh", NativeName = "English", Status = "coming_soon", SortOrder = 2 },
        new() { Code = "japanese", Name = "Tiếng Nhật", NativeName = "日本語", Status = "coming_soon", SortOrder = 3 }
    ];

    public static async Task SeedAsync(CmsDbContext db, CmsSeedOptions seedOptions, TimeProvider timeProvider, ILogger logger, CancellationToken ct)
    {
        try
        {
            await SeedSettingsAsync(db, timeProvider, ct);
            await SeedLanguagesAsync(db, seedOptions, timeProvider, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seed site.* thất bại — service vẫn khởi động, cấu hình/ngôn ngữ có thể thiếu tới lần seed kế tiếp.");
        }
    }

    /// <summary>Chỉ chèn bù khoá THIẾU — KHÔNG ghi đè giá trị đã có (settings là danh mục hệ thống, không xoá được).</summary>
    private static async Task SeedSettingsAsync(CmsDbContext db, TimeProvider timeProvider, CancellationToken ct)
    {
        var existingKeys = (await db.SiteSettings.Select(s => s.Key).ToListAsync(ct)).ToHashSet();
        var missing = SiteSettingKeys.All.Where(d => !existingKeys.Contains(d.Key)).ToList();
        if (missing.Count == 0)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var definition in missing)
            db.SiteSettings.Add(SiteSetting.Create(definition.Key, definition.DefaultValue, now));

        await db.SaveChangesAsync(ct);
    }

    /// <summary>R-W25: CHỈ gieo khi bảng site.languages RỖNG — chạy lại (hoặc sau khi admin xoá) không mọc lại.</summary>
    private static async Task SeedLanguagesAsync(CmsDbContext db, CmsSeedOptions seedOptions, TimeProvider timeProvider, CancellationToken ct)
    {
        if (await db.Languages.AnyAsync(ct))
            return;

        var items = seedOptions.Languages.Length > 0 ? seedOptions.Languages : DefaultLanguages;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var item in items)
        {
            var status = LanguageStatuses.TryParse(item.Status, out var parsed) ? parsed : LanguageStatus.ComingSoon;
            db.Languages.Add(Language.Create(
                item.Code, item.Name, item.NativeName, item.Tagline, "", status, item.AppUrl, null, item.SortOrder,
                actorId: null, now));
        }

        await db.SaveChangesAsync(ct);
    }
}
