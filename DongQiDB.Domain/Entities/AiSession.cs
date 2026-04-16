using DongQiDB.Domain.Common;

namespace DongQiDB.Domain.Entities;

/// <summary>
/// AI conversation session entity
/// </summary>
public class AiSession : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public long? ConnectionId { get; set; }
    public string DatabaseType { get; set; } = string.Empty;
    public string LastUserMessage { get; set; } = string.Empty;
    public string LastAiResponse { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Connection? Connection { get; set; }
    public virtual ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
}
