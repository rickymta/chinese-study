using AntFarm.Cms.Application.Common.Abstractions;
using AntFarm.Cms.Application.Common.Audit;
using AntFarm.Cms.Application.Common.Revalidation;
using AntFarm.Cms.Application.Site.Dtos;
using AntFarm.Cms.Application.Site.Validators;
using AntFarm.Cms.Domain.Site;
using AntFarm.Core.Errors;
using Microsoft.EntityFrameworkCore;

namespace AntFarm.Cms.Application.Site;

/// <summary>
/// Quản trị câu hỏi thường gặp (FAQ) hiển thị trên website (§5.2.3, §6.2 W3b). Mọi thao tác ghi
/// ghi 1 dòng nhật ký (<see cref="IAuditLogger"/>, cùng giao dịch) rồi báo revalidate tag
/// <c>site</c>, <c>faqs</c> (§5.4.3) SAU khi <c>SaveChangesAsync</c> thành công.
/// </summary>
public sealed class FaqAdminService(ICmsDbContext db, IAuditLogger auditLogger, IRevalidationNotifier revalidation, TimeProvider timeProvider)
{
    private static readonly string[] FaqTags = ["site", "faqs"];

    public async Task<IReadOnlyList<FaqDto>> ListAsync(CancellationToken ct)
    {
        var faqs = await db.Faqs.AsNoTracking()
            .OrderBy(f => f.GroupKey).ThenBy(f => f.SortOrder)
            .ToListAsync(ct);
        return faqs.Select(Map).ToList();
    }

    public async Task<FaqDto> GetAsync(Guid id, CancellationToken ct)
    {
        var faq = await db.Faqs.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy FAQ.");
        return Map(faq);
    }

    public async Task<FaqDto> CreateAsync(Actor actor, CreateFaqRequest request, CancellationToken ct)
    {
        var groupKey = FaqInputRules.Normalize(request.GroupKey);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var maxSortOrder = await db.Faqs.Where(f => f.GroupKey == groupKey)
            .Select(f => (int?)f.SortOrder).MaxAsync(ct) ?? 0;

        var faq = Faq.Create(
            request.Question.Trim(), request.AnswerMarkdown.Trim(), groupKey, request.IsPublished,
            maxSortOrder + 1, actor.Id, now);

        db.Faqs.Add(faq);
        auditLogger.Add(actor, "faq.create", "faq", faq.Id.ToString(), $"Tạo FAQ: {Truncate(faq.Question)}");

        await db.SaveChangesAsync(ct);
        await revalidation.NotifyAsync(FaqTags, ct);
        return Map(faq);
    }

    public async Task<FaqDto> UpdateAsync(Actor actor, Guid id, UpdateFaqRequest request, CancellationToken ct)
    {
        var version = ParseVersion(request.Version);
        var faq = await db.Faqs.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy FAQ.");
        db.SetOriginalVersion(faq, version);

        var groupKey = FaqInputRules.Normalize(request.GroupKey);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Đổi nhóm ⇒ đưa về CUỐI nhóm mới (§5.2.3); giữ nguyên nếu không đổi nhóm.
        int sortOrder;
        if (groupKey != faq.GroupKey)
        {
            var maxSortOrder = await db.Faqs.AsNoTracking()
                .Where(f => f.GroupKey == groupKey && f.Id != id)
                .Select(f => (int?)f.SortOrder).MaxAsync(ct) ?? 0;
            sortOrder = maxSortOrder + 1;
        }
        else
        {
            sortOrder = faq.SortOrder;
        }

        faq.Update(request.Question.Trim(), request.AnswerMarkdown.Trim(), groupKey, sortOrder, request.IsPublished, actor.Id, now);
        auditLogger.Add(actor, "faq.update", "faq", faq.Id.ToString(), $"Sửa FAQ: {Truncate(faq.Question)}");

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("CONCURRENCY_CONFLICT", "FAQ đã bị sửa ở nơi khác — hãy tải lại.");
        }

        await revalidation.NotifyAsync(FaqTags, ct);
        return Map(faq);
    }

    public async Task DeleteAsync(Actor actor, Guid id, CancellationToken ct)
    {
        var faq = await db.Faqs.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy FAQ.");

        db.Faqs.Remove(faq);
        auditLogger.Add(actor, "faq.delete", "faq", faq.Id.ToString(), $"Xoá FAQ: {Truncate(faq.Question)}");
        await db.SaveChangesAsync(ct);

        await revalidation.NotifyAsync(FaqTags, ct);
    }

    /// <summary><paramref name="ids"/> phải ĐÚNG BẰNG tập FAQ của <paramref name="rawGroupKey"/> (không thiếu, không thừa, không trùng) — sai ⇒ 422 ORDER_MISMATCH (§5.2.3).</summary>
    public async Task ReorderAsync(Actor actor, string? rawGroupKey, IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var groupKey = FaqInputRules.Normalize(rawGroupKey);
        var faqs = await db.Faqs.Where(f => f.GroupKey == groupKey).ToListAsync(ct);
        var existingIds = faqs.Select(f => f.Id).ToHashSet();
        var requestedIds = ids.ToHashSet();

        if (requestedIds.Count != ids.Count || !requestedIds.SetEquals(existingIds))
            throw new BusinessRuleException("ORDER_MISMATCH", "Danh sách sắp xếp phải khớp đúng tập FAQ hiện có của nhóm.");

        var byId = faqs.ToDictionary(f => f.Id);
        for (var i = 0; i < ids.Count; i++)
            byId[ids[i]].Reorder(i + 1);

        auditLogger.Add(actor, "faq.reorder", "faq", null, $"Sắp xếp lại FAQ: {groupKey}");
        await db.SaveChangesAsync(ct);

        await revalidation.NotifyAsync(FaqTags, ct);
    }

    private static FaqDto Map(Faq f) => new(
        f.Id, f.Question, f.AnswerMarkdown, f.GroupKey, f.SortOrder, f.IsPublished, f.Version.ToString(), f.UpdatedAt);

    private static uint ParseVersion(string version) =>
        uint.TryParse(version, out var parsed)
            ? parsed
            : throw new ValidationAppException("version không hợp lệ.", new { version = new[] { "version phải là chuỗi số." } });

    /// <summary>Nhật ký KHÔNG chép Markdown dài — chỉ 60 ký tự đầu câu hỏi (§5.2.3).</summary>
    private static string Truncate(string question) => question.Length > 60 ? question[..60] : question;
}
