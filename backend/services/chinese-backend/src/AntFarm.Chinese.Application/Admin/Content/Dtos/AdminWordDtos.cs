namespace AntFarm.Chinese.Application.Admin.Content.Dtos;

/// <summary>Hình dạng dùng chung <c>AdminWord</c> (§6.3) — trả về từ danh sách/chi tiết/sửa.</summary>
public sealed record AdminWordDto(
    Guid Id, uint Version, string Simplified, string? Traditional, string Pinyin,
    short? Hsk3Level, int? PathOrder, IReadOnlyList<string> Pos, IReadOnlyList<string> MeaningsEn,
    IReadOnlyList<string> MeaningsVi, string MeaningViStatus, string MeaningViSource,
    string? HanViet, string? HanVietStatus, DateTime? EditedAt, string? EditedByName);

public sealed record AdminWordsPageDto(IReadOnlyList<AdminWordDto> Items, int Page, int PageSize, int TotalCount);

/// <summary>
/// Query string <c>GET /api/admin/words</c> (§6.3, R-CA11) — <see cref="Hsk"/> KHÔNG bind được thì
/// mặc định 1 (§5.2.3 "Danh sách từ": "hsk (mặc định 1)") — property giữ <c>short?</c> để phân biệt
/// "không truyền" (áp mặc định 1 ở service) với việc FE có thể truyền tường minh giá trị khác.
/// </summary>
public sealed class AdminWordsQuery
{
    public string? MeaningViStatus { get; init; }
    public string? HanVietStatus { get; init; }
    public short? Hsk { get; init; }
    public string? Q { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>Body <c>PUT /api/admin/words/{id}</c> (§6.3, R-CA9).</summary>
public sealed record UpdateWordRequest(
    uint Version, IReadOnlyList<string> MeaningsVi, string MeaningViStatus, string? HanViet, string HanVietStatus);

/// <summary>Một mục trong <c>POST /api/admin/words/review</c> (§6.3) — duyệt hàng loạt, KHÔNG đổi nội dung (R-CA9).</summary>
public sealed record BulkReviewWordItem(Guid Id, uint Version);

public sealed record BulkReviewWordsRequest(IReadOnlyList<BulkReviewWordItem> Items);

/// <summary>Kết quả duyệt hàng loạt (§6.3) — xử lý TỪNG MỤC độc lập (không nguyên tử, §5.2.3 "Duyệt từ"): <see cref="Conflicts"/> = lệch <c>version</c>, <see cref="NotFound"/> = id không tồn tại.</summary>
public sealed record BulkReviewResultDto(int Updated, IReadOnlyList<Guid> Conflicts, IReadOnlyList<Guid> NotFound);
