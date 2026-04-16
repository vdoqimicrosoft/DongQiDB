using DongQiDB.Application.DTOs;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Common;
using DongQiDB.Domain.Entities;
using DongQiDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DongQiDB.Infrastructure.AI;

/// <summary>
/// AI session service implementation
/// </summary>
public class AiSessionService : IAiSessionService
{
    private readonly SystemDbContext _context;
    private readonly ILogger<AiSessionService> _logger;

    public AiSessionService(SystemDbContext context, ILogger<AiSessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AiSession>> CreateSessionAsync(
        CreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = new AiSession
            {
                Title = request.Title,
                ConnectionId = request.ConnectionId,
                DatabaseType = request.DatabaseType,
                MessageCount = 0,
                LastActivityAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.AiSessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<AiSession>.Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AI session");
            return Result<AiSession>.Fail(ErrorCode.InternalError, ex.Message);
        }
    }

    public async Task<Result<AiSession>> GetSessionAsync(
        long sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _context.AiSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session == null)
            {
                return Result<AiSession>.Fail(ErrorCode.NotFound, "Session not found");
            }

            return Result<AiSession>.Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session {SessionId}", sessionId);
            return Result<AiSession>.Fail(ErrorCode.InternalError, ex.Message);
        }
    }

    public async Task<Result<IEnumerable<AiSession>>> GetSessionsAsync(
        long? connectionId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.AiSessions.AsQueryable();

            if (connectionId.HasValue)
            {
                query = query.Where(s => s.ConnectionId == connectionId);
            }

            var sessions = await query
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<AiSession>>.Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions");
            return Result<IEnumerable<AiSession>>.Fail(ErrorCode.InternalError, ex.Message);
        }
    }

    public async Task<Result<AiMessage>> AddMessageAsync(
        AddMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _context.AiSessions.FindAsync(new object[] { request.SessionId }, cancellationToken);
            if (session == null)
            {
                return Result<AiMessage>.Fail(ErrorCode.NotFound, "Session not found");
            }

            var message = new AiMessage
            {
                SessionId = request.SessionId,
                Role = request.Role,
                Content = request.Content,
                SqlGenerated = request.SqlGenerated,
                ErrorMessage = request.ErrorMessage,
                TokenUsage = request.TokenUsage,
                ExecutionTimeMs = request.ExecutionTimeMs,
                CreatedAt = DateTime.UtcNow
            };

            _context.AiMessages.Add(message);

            // Update session
            session.MessageCount++;
            session.LastActivityAt = DateTime.UtcNow;
            if (request.Role == "user")
            {
                session.LastUserMessage = request.Content;
            }
            else if (request.Role == "assistant")
            {
                session.LastAiResponse = request.Content;
                if (string.IsNullOrEmpty(session.Title) && request.Content.Length > 0)
                {
                    // Auto-generate title from first user message
                    session.Title = session.LastUserMessage.Length > 50
                        ? session.LastUserMessage[..50] + "..."
                        : session.LastUserMessage;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result<AiMessage>.Ok(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to session {SessionId}", request.SessionId);
            return Result<AiMessage>.Fail(ErrorCode.InternalError, ex.Message);
        }
    }

    public async Task<Result<IEnumerable<AiMessage>>> GetMessagesAsync(
        long sessionId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _context.AiMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<AiMessage>>.Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for session {SessionId}", sessionId);
            return Result<IEnumerable<AiMessage>>.Fail(ErrorCode.InternalError, ex.Message);
        }
    }

    public async Task<Result<bool>> DeleteSessionAsync(
        long sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _context.AiSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session == null)
            {
                return Result<bool>.Fail(ErrorCode.NotFound, "Session not found");
            }

            _context.AiMessages.RemoveRange(session.Messages);
            _context.AiSessions.Remove(session);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
            return Result<bool>.Fail(ErrorCode.InternalError, ex.Message);
        }
    }

    public async Task<List<AiMessageItem>> GetContextAsync(
        long sessionId,
        int maxMessages = 10,
        CancellationToken cancellationToken = default)
    {
        var messages = await _context.AiMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(maxMessages)
            .ToListAsync(cancellationToken);

        return messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new AiMessageItem
            {
                Role = m.Role,
                Content = m.Content
            })
            .ToList();
    }
}
