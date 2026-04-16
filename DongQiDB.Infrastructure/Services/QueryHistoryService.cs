using System.Globalization;
using System.Text;
using DongQiDB.Application.DTOs;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Entities;
using DongQiDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DongQiDB.Infrastructure.Services;

/// <summary>
/// Query history service implementation
/// </summary>
public class QueryHistoryService : IQueryHistoryService
{
    private readonly SystemDbContext _context;
    private readonly ILogger<QueryHistoryService> _logger;

    public QueryHistoryService(SystemDbContext context, ILogger<QueryHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QueryHistory> SaveAsync(
        long connectionId,
        string naturalLanguageQuery,
        string generatedSql,
        bool isSuccess,
        string? errorMessage,
        long executionTimeMs,
        int rowCount,
        string? aiModel = null,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        var history = new QueryHistory
        {
            ConnectionId = connectionId,
            NaturalLanguageQuery = naturalLanguageQuery,
            GeneratedSql = generatedSql,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            ExecutionTimeMs = executionTimeMs,
            RowCount = rowCount,
            AiModel = aiModel,
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.QueryHistories.Add(history);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Saved query history: {Id} for connection {ConnectionId}", history.Id, connectionId);
        return history;
    }

    public async Task<PagedResult<QueryHistory>> GetHistoryAsync(
        QueryHistoryFilter filter,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(filter);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return PagedResult<QueryHistory>.Create(items, totalCount, pageIndex, pageSize);
    }

    public async Task<QueryHistory?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.QueryHistories
            .Include(q => q.Connection)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<QueryHistory>> SearchAsync(
        long connectionId,
        string searchText,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return Enumerable.Empty<QueryHistory>();

        var searchLower = searchText.ToLowerInvariant();

        return await _context.QueryHistories
            .Where(q => q.ConnectionId == connectionId &&
                       (q.NaturalLanguageQuery.ToLower().Contains(searchLower) ||
                        q.GeneratedSql.ToLower().Contains(searchLower)))
            .OrderByDescending(q => q.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var history = await _context.QueryHistories.FindAsync(new object[] { id }, cancellationToken);
        if (history != null)
        {
            _context.QueryHistories.Remove(history);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Deleted query history: {Id}", id);
        }
    }

    public async Task ClearAsync(long connectionId, CancellationToken cancellationToken = default)
    {
        var histories = await _context.QueryHistories
            .Where(q => q.ConnectionId == connectionId)
            .ToListAsync(cancellationToken);

        _context.QueryHistories.RemoveRange(histories);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cleared {Count} query history records for connection {ConnectionId}",
            histories.Count, connectionId);
    }

    public async Task<string> ExportToCsvAsync(
        QueryHistoryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(filter);
        var histories = await query
            .OrderByDescending(q => q.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Id,ConnectionId,NaturalLanguageQuery,GeneratedSql,IsSuccess,ErrorMessage,ExecutionTimeMs,RowCount,AiModel,SessionId,CreatedAt");

        // Data rows
        foreach (var h in histories)
        {
            sb.AppendLine(string.Join(",",
                h.Id,
                h.ConnectionId,
                EscapeCsv(h.NaturalLanguageQuery),
                EscapeCsv(h.GeneratedSql),
                h.IsSuccess,
                EscapeCsv(h.ErrorMessage ?? ""),
                h.ExecutionTimeMs,
                h.RowCount,
                EscapeCsv(h.AiModel ?? ""),
                EscapeCsv(h.SessionId ?? ""),
                h.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    public async Task<IEnumerable<QueryHistory>> GetRecentBySessionAsync(
        string sessionId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.QueryHistories
            .Where(q => q.SessionId == sessionId)
            .OrderByDescending(q => q.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private IQueryable<QueryHistory> BuildFilteredQuery(QueryHistoryFilter filter)
    {
        var query = _context.QueryHistories.AsQueryable();

        if (filter.ConnectionId.HasValue)
            query = query.Where(q => q.ConnectionId == filter.ConnectionId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SessionId))
            query = query.Where(q => q.SessionId == filter.SessionId);

        if (filter.IsSuccess.HasValue)
            query = query.Where(q => q.IsSuccess == filter.IsSuccess.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var searchLower = filter.SearchText.ToLowerInvariant();
            query = query.Where(q =>
                q.NaturalLanguageQuery.ToLower().Contains(searchLower) ||
                q.GeneratedSql.ToLower().Contains(searchLower));
        }

        if (filter.StartDate.HasValue)
            query = query.Where(q => q.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(q => q.CreatedAt <= filter.EndDate.Value);

        return query;
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "\"\"";

        // Escape quotes by doubling them
        var escaped = value.Replace("\"", "\"\"");

        // Wrap in quotes if contains special characters
        if (escaped.Contains(',') || escaped.Contains('"') ||
            escaped.Contains('\n') || escaped.Contains('\r'))
            return $"\"{escaped}\"";

        return $"\"{escaped}\"";
    }
}
