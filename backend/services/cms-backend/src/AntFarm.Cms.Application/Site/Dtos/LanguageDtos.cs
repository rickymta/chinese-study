namespace AntFarm.Cms.Application.Site.Dtos;

/// <summary>
/// Hình dạng dùng chung <c>LanguageDto</c> (§6.2) — trả về từ mọi endpoint đọc/ghi.
/// <see cref="Version"/> = <c>xmin</c> đọc được DẠNG CHUỖI SỐ (client gửi lại nguyên văn ở lần ghi
/// kế tiếp, R-CA3). <see cref="CoverMedia"/> luôn null ở W3a (bảng media tạo ở W4, kiểu MediaDto
/// thật sẽ thay <c>object?</c> lúc đó).
/// </summary>
public sealed record LanguageDto(
    Guid Id, string Code, string Name, string NativeName, string Tagline, string DescriptionMarkdown,
    string Status, string? AppUrl, string? AccentColor, object? CoverMedia, int SortOrder, string Version, DateTime UpdatedAt);

/// <summary>Thân <c>POST /api/admin/languages</c> — <c>code</c> bất biến sau khi tạo (§5.2.3).</summary>
public sealed record CreateLanguageRequest(
    string Code, string Name, string NativeName, string? Tagline, string? DescriptionMarkdown,
    string Status, string? AppUrl, string? AccentColor);

/// <summary>Thân <c>PUT /api/admin/languages/{id}</c> — KHÔNG có <c>code</c>; <see cref="Version"/> = <c>LanguageDto.Version</c> đọc lần trước (409 CONCURRENCY_CONFLICT nếu lệch).</summary>
public sealed record UpdateLanguageRequest(
    string Name, string NativeName, string? Tagline, string? DescriptionMarkdown,
    string Status, string? AppUrl, string? AccentColor, string Version);

/// <summary>Thân <c>PUT /api/admin/languages/order</c> — <c>ids</c> phải ĐÚNG BẰNG tập id hiện có (422 ORDER_MISMATCH nếu không).</summary>
public sealed record ReorderLanguagesRequest(IReadOnlyList<Guid>? Ids);
