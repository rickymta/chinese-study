namespace AntFarm.Cms.Application.Site.Dtos;

/// <summary>Hình dạng dùng chung <c>FaqDto</c> (§6.2 W3b) — trả về từ mọi endpoint đọc/ghi.</summary>
public sealed record FaqDto(
    Guid Id, string Question, string AnswerMarkdown, string GroupKey, int SortOrder,
    bool IsPublished, string Version, DateTime UpdatedAt);

/// <summary>Thân <c>POST /api/admin/faqs</c> (§5.2.3 W3b) — <c>groupKey</c> rỗng ⇒ mặc định <c>general</c>.</summary>
public sealed record CreateFaqRequest(string Question, string AnswerMarkdown, string? GroupKey, bool IsPublished);

/// <summary>Thân <c>PUT /api/admin/faqs/{id}</c> — <see cref="Version"/> = <c>FaqDto.Version</c> đọc lần trước (409 CONCURRENCY_CONFLICT nếu lệch).</summary>
public sealed record UpdateFaqRequest(string Question, string AnswerMarkdown, string? GroupKey, bool IsPublished, string Version);

/// <summary>Thân <c>PUT /api/admin/faqs/order</c> — <c>ids</c> phải ĐÚNG BẰNG tập FAQ của <c>groupKey</c> (422 ORDER_MISMATCH nếu không).</summary>
public sealed record ReorderFaqsRequest(string? GroupKey, IReadOnlyList<Guid>? Ids);
