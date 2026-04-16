namespace DongQiDB.Application.Interfaces;

/// <summary>
/// AI service interface for LLM interactions
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Sends a chat request to the AI and returns the response
    /// </summary>
    Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a chat request with streaming response
    /// </summary>
    IAsyncEnumerable<string> ChatStreamAsync(AiChatRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// AI chat request model
/// </summary>
public class AiChatRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public List<AiMessageItem> Messages { get; set; } = new();
    public AiOptions Options { get; set; } = new();
}

/// <summary>
/// Individual message in the conversation
/// </summary>
public class AiMessageItem
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// AI chat response model
/// </summary>
public class AiChatResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string? StopReason { get; set; }
}
