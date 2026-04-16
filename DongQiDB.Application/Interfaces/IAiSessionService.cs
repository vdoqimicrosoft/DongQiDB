using DongQiDB.Application.DTOs;
using DongQiDB.Domain.Entities;

namespace DongQiDB.Application.Interfaces;

/// <summary>
/// AI session service interface
/// </summary>
public interface IAiSessionService
{
    /// <summary>
    /// Creates a new AI session
    /// </summary>
    Task<Result<AiSession>> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session by ID
    /// </summary>
    Task<Result<AiSession>> GetSessionAsync(long sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions for a user/connection
    /// </summary>
    Task<Result<IEnumerable<AiSession>>> GetSessionsAsync(long? connectionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a message to the session
    /// </summary>
    Task<Result<AiMessage>> AddMessageAsync(AddMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages for a session
    /// </summary>
    Task<Result<IEnumerable<AiMessage>>> GetMessagesAsync(long sessionId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session
    /// </summary>
    Task<Result<bool>> DeleteSessionAsync(long sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets conversation context for a session
    /// </summary>
    Task<List<AiMessageItem>> GetContextAsync(long sessionId, int maxMessages = 10, CancellationToken cancellationToken = default);
}

/// <summary>
/// Create session request
/// </summary>
public class CreateSessionRequest
{
    public string Title { get; set; } = string.Empty;
    public long? ConnectionId { get; set; }
    public string DatabaseType { get; set; } = string.Empty;
}

/// <summary>
/// Add message request
/// </summary>
public class AddMessageRequest
{
    public long SessionId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? SqlGenerated { get; set; }
    public string? ErrorMessage { get; set; }
    public double? TokenUsage { get; set; }
    public long? ExecutionTimeMs { get; set; }
}
