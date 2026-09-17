using AntFarm.Cms.Application.Common.Abstractions;
using AntFarm.Cms.Application.Common.Audit;
using AntFarm.Cms.Application.Common.Revalidation;
using AntFarm.Cms.Application.Site.Dtos;
using AntFarm.Cms.Domain.Site;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Cms.Application.Site;

/// <summary>
/// Quản trị danh mục ngôn ngữ hiển thị trên website/portal (§5.2.3, §6.2 W3a). Mọi thao tác ghi
/// ghi 1 dòng nhật ký (<see cref="IAuditLogger"/>, cùng giao dịch) rồi báo revalidate tag
/// <c>site</c>, <c>languages</c> (§5.4.3) SAU khi <c>SaveChangesAsync</c> thành công.
/// </summary>
public sealed class LanguageAdminService(ICmsDbContext db, IAuditLogger auditLogger, IRevalidationNotifier revalidation, TimeProvider timeProvider)
{
    private static readonly string[] LanguageTags = ["site", "languages"];

    public async Task<IReadOnlyList<LanguageDto>> ListAsync(CancellationToken ct)
    {
        var languages = await db.Languages.AsNoTracking().OrderBy(l => l.SortOrder).ToListAsync(ct);
        return languages.Select(Map).ToList();
    }

    public async Task<LanguageDto> GetAsync(Guid id, CancellationToken ct)
    {
        var language = await db.Languages.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy ngôn ngữ.");
        return Map(language);
    }

    public async Task<LanguageDto> CreateAsync(Actor actor, CreateLanguageRequest request, CancellationToken ct)
    {
        var code = request.Code.Trim();
        var status = ParseStatus(request.Status);
        var appUrl = NormalizeAppUrl(request.AppUrl);
        EnsureAppUrlPresentWhenOpen(status, appUrl);
        var accentColor = NormalizeAccentColor(request.AccentColor);

        if (await db.Languages.AsNoTracking().AnyAsync(l => l.Code == code, ct))
            throw new ConflictException("CODE_TAKEN", $"Mã ngôn ngữ '{code}' đã được dùng.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var maxSortOrder = await db.Languages.Select(l => (int?)l.SortOrder).MaxAsync(ct) ?? 0;

        var language = Language.Create(
            code, request.Name.Trim(), request.NativeName.Trim(), (request.Tagline ?? "").Trim(),
            (request.DescriptionMarkdown ?? "").Trim(), status, appUrl, accentColor, maxSortOrder + 1, actor.Id, now);

        db.Languages.Add(language);
        auditLogger.Add(actor, "language.create", "language", code, $"Tạo ngôn ngữ {code}");

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsCodeUniqueViolation(ex))
        {
            db.ClearTracking();
            throw new ConflictException("CODE_TAKEN", $"Mã ngôn ngữ '{code}' đã được dùng.");
        }

        await revalidation.NotifyAsync(LanguageTags, ct);
        return Map(language);
    }

    public async Task<LanguageDto> UpdateAsync(Actor actor, Guid id, UpdateLanguageRequest request, CancellationToken ct)
    {
        var version = ParseVersion(request.Version);
        var language = await db.Languages.FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy ngôn ngữ.");
        db.SetOriginalVersion(language, version);

        var status = ParseStatus(request.Status);
        var appUrl = NormalizeAppUrl(request.AppUrl);
        EnsureAppUrlPresentWhenOpen(status, appUrl);
        var accentColor = NormalizeAccentColor(request.AccentColor);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        language.Update(
            request.Name.Trim(), request.NativeName.Trim(), (request.Tagline ?? "").Trim(),
            (request.DescriptionMarkdown ?? "").Trim(), status, appUrl, accentColor, actor.Id, now);

        auditLogger.Add(actor, "language.update", "language", language.Code, $"Sửa ngôn ngữ {language.Code}");

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("CONCURRENCY_CONFLICT", "Ngôn ngữ đã bị sửa ở nơi khác — hãy tải lại.");
        }

        await revalidation.NotifyAsync(LanguageTags, ct);
        return Map(language);
    }

    public async Task DeleteAsync(Actor actor, Guid id, CancellationToken ct)
    {
        var language = await db.Languages.FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy ngôn ngữ.");

        db.Languages.Remove(language);
        auditLogger.Add(actor, "language.delete", "language", language.Code, $"Xoá ngôn ngữ {language.Code}");
        await db.SaveChangesAsync(ct);

        await revalidation.NotifyAsync(LanguageTags, ct);
    }

    /// <summary><paramref name="ids"/> phải ĐÚNG BẰNG tập id hiện có (không thiếu, không thừa, không trùng) — sai ⇒ 422 ORDER_MISMATCH (§5.2.3).</summary>
    public async Task ReorderAsync(Actor actor, IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var languages = await db.Languages.ToListAsync(ct);
        var existingIds = languages.Select(l => l.Id).ToHashSet();
        var requestedIds = ids.ToHashSet();

        if (requestedIds.Count != ids.Count || !requestedIds.SetEquals(existingIds))
            throw new BusinessRuleException("ORDER_MISMATCH", "Danh sách sắp xếp phải khớp đúng tập ngôn ngữ hiện có.");

        var byId = languages.ToDictionary(l => l.Id);
        for (var i = 0; i < ids.Count; i++)
            byId[ids[i]].Reorder(i + 1);

        auditLogger.Add(actor, "language.reorder", "language", null, "Sắp xếp lại ngôn ngữ");
        await db.SaveChangesAsync(ct);

        await revalidation.NotifyAsync(LanguageTags, ct);
    }

    private static LanguageDto Map(Language l) => new(
        l.Id, l.Code, l.Name, l.NativeName, l.Tagline, l.DescriptionMarkdown,
        LanguageStatuses.ToCode(l.Status), l.AppUrl, l.AccentColor, null, l.SortOrder, l.Version.ToString(), l.UpdatedAt);

    private static LanguageStatus ParseStatus(string status) =>
        LanguageStatuses.TryParse(status, out var parsed)
            ? parsed
            : throw new ValidationAppException("status không hợp lệ.", new { status = new[] { "status phải là open, coming_soon hoặc hidden." } });

    private static uint ParseVersion(string version) =>
        uint.TryParse(version, out var parsed)
            ? parsed
            : throw new ValidationAppException("version không hợp lệ.", new { version = new[] { "version phải là chuỗi số." } });

    /// <summary>Rỗng/khoảng trắng ⇒ null (lưu DB NULL) — khớp <c>appUrl|null</c> của schema (§5.1.2).</summary>
    private static string? NormalizeAppUrl(string? appUrl) =>
        string.IsNullOrWhiteSpace(appUrl) ? null : appUrl.Trim();

    private static string? NormalizeAccentColor(string? accentColor) =>
        string.IsNullOrWhiteSpace(accentColor) ? null : accentColor.Trim();

    /// <summary>422 APP_URL_REQUIRED (§5.2.3) — chỉ phát hiện được SAU khi biết <c>status</c> đã parse, không phải lỗi hình dạng (400).</summary>
    private static void EnsureAppUrlPresentWhenOpen(LanguageStatus status, string? appUrl)
    {
        if (status == LanguageStatus.Open && string.IsNullOrEmpty(appUrl))
            throw new BusinessRuleException("APP_URL_REQUIRED", "Ngôn ngữ đang mở (open) phải có appUrl.");
    }

    /// <summary>Tên ràng buộc UNIQUE do CHÍNH ta đặt trong <c>LanguageConfiguration</c> (ổn định, không phụ thuộc kiểu Npgsql cụ thể).</summary>
    private static bool IsCodeUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("ux_languages_code", StringComparison.OrdinalIgnoreCase) == true;
}
