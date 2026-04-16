namespace DongQiDB.Application.DTOs;

/// <summary>
/// Pagination request parameters
/// </summary>
public class PaginationQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private int _pageSize = DefaultPageSize;

    public int PageIndex { get; set; } = 0;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}
