using DongQiDB.Domain.Common;

namespace DongQiDB.Domain.Entities;

/// <summary>
/// AI message entity within a session
/// </summary>
public class AiMessage : BaseEntity
{
    public long SessionId { get; set; }
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public string? SqlGenerated { get; set; }
    public string? ErrorMessage { get; set; }
    public double? TokenUsage { get; set; }
    public long? ExecutionTimeMs { get; set; }

    // Navigation property
    public virtual AiSession? Session { get; set; }
}
