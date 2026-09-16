namespace AntFarm.Core.Pagination;

/// <summary>Chuẩn hoá tham số phân trang từ query string (§5.0.3): <c>page</c> từ 1,
/// <c>pageSize</c> mặc định 20, tối đa 100. Hàm thuần — dễ unit test, không phụ thuộc HTTP.</summary>
public static class PageQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>page &lt; 1 ⇒ 1; pageSize &lt; 1 ⇒ 20 (mặc định); pageSize &gt; 100 ⇒ 100.</summary>
    public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
    {
        var normalizedPage = page is null or < 1 ? 1 : page.Value;
        var normalizedPageSize = pageSize switch
        {
            null or < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize.Value
        };
        return (normalizedPage, normalizedPageSize);
    }
}
