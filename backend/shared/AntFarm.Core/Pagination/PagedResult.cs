namespace AntFarm.Core.Pagination;

/// <summary>Bọc kết quả phân trang thống nhất cho mọi API danh sách (§5.0.3).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
