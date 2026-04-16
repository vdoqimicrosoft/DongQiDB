using DongQiDB.Application.DTOs;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Application.Interfaces;

/// <summary>
/// Query history filter
/// </summary>
public class QueryHistoryFilter
{
    public long? ConnectionId { get; set; }
    public string? SessionId { get; set; }
    public bool? IsSuccess { get; set; }
    public string? SearchText { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Query history service interface
/// </summary>
public interface IQueryHistoryService
{
    /// <summary>
    /// Saves a query execution to history
    /// </summary>
    Task<QueryHistory> SaveAsync(
        long connectionId,
        string naturalLanguageQuery,
        string generatedSql,
        bool isSuccess,
        string? errorMessage,
        long executionTimeMs,
        int rowCount,
        string? aiModel = null,
        string? sessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets query history with pagination
    /// </summary>
    Task<PagedResult<QueryHistory>> GetHistoryAsync(
        QueryHistoryFilter filter,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single history record by ID
    /// </summary>
    Task<QueryHistory?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches history by text
    /// </summary>
    Task<IEnumerable<QueryHistory>> SearchAsync(
        long connectionId,
        string searchText,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a history record
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all history for a connection
    /// </summary>
    Task ClearAsync(long connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports history to CSV format
    /// </summary>
    Task<string> ExportToCsvAsync(
        QueryHistoryFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent queries for a session
    /// </summary>
    Task<IEnumerable<QueryHistory>> GetRecentBySessionAsync(
        string sessionId,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
