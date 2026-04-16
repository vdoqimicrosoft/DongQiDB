namespace DongQiDB.Application.DTOs;

/// <summary>
/// Paginated result wrapper
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageIndex > 0;
    public bool HasNext => PageIndex < TotalPages - 1;

    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
        => new(items, totalCount, pageIndex, pageSize);
}
