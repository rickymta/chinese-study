namespace AntFarm.Cms.Application.Site.Dtos;

/// <summary>Một ngôn ngữ trong <c>GET /api/public/site</c> (§6.2) — chỉ trường phục vụ hiển thị công khai, KHÔNG có id/version/sortOrder nội bộ.</summary>
public sealed record PublicLanguageDto(
    string Code, string Name, string NativeName, string Tagline, string DescriptionMarkdown,
    string Status, string? AppUrl, string? AccentColor, string? CoverUrl);

/// <summary>Một FAQ công khai (điền từ W3b — W3a luôn trả mảng rỗng).</summary>
public sealed record PublicFaqDto(Guid Id, string Question, string AnswerMarkdown, string GroupKey);

/// <summary>
/// Toàn bộ dữ liệu nền website (§6.2) — ẩn danh, cache 60 giây. <see cref="Settings"/> đã bỏ khoá
/// giá trị rỗng; <see cref="Languages"/> loại <c>hidden</c>, sắp theo <c>sortOrder</c>;
/// <see cref="OgImageUrl"/> luôn null tới W4; <see cref="Faqs"/> luôn rỗng ở W3a (W3b điền).
/// </summary>
public sealed record PublicSiteDto(
    IReadOnlyDictionary<string, string> Settings, string? OgImageUrl,
    IReadOnlyList<PublicLanguageDto> Languages, IReadOnlyList<PublicFaqDto> Faqs);
