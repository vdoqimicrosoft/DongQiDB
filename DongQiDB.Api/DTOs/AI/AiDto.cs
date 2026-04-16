namespace DongQiDB.Api.DTOs.AI;

/// <summary>
/// Text to SQL request
/// </summary>
public class TextToSqlRequestDto
{
    public long ConnectionId { get; set; }
    public string UserQuestion { get; set; } = string.Empty;
    public string? SchemaName { get; set; }
    public bool IncludeExplanation { get; set; } = true;
    public bool ValidateOnly { get; set; } = false;
    public bool StreamOutput { get; set; } = false;
}

/// <summary>
/// Text to SQL response
/// </summary>
public class TextToSqlResponseDto
{
    public string SqlQuery { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public List<string> TablesUsed { get; set; } = new();
    public List<string> Parameters { get; set; } = new();
    public bool IsValid { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public double Confidence { get; set; }
}

/// <summary>
/// SQL to text request
/// </summary>
public class SqlToTextRequestDto
{
    public string SqlQuery { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = "postgresql";
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// SQL to text response
/// </summary>
public class SqlToTextResponseDto
{
    public string Explanation { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> TablesInvolved { get; set; } = new();
    public List<string> Operations { get; set; } = new();
    public List<ConditionInfo> Conditions { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Condition info
/// </summary>
public class ConditionInfo
{
    public string Column { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// SQL optimize request
/// </summary>
public class SqlOptimizeRequestDto
{
    public string SqlQuery { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = "postgresql";
    public string? TargetGoal { get; set; }
}

/// <summary>
/// SQL optimize response
/// </summary>
public class SqlOptimizeResponseDto
{
    public string OptimizedSql { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public double? EstimatedImprovement { get; set; }
    public List<string> Suggestions { get; set; } = new();
    public List<string> IndexRecommendations { get; set; } = new();
    public bool WasModified { get; set; }
}

/// <summary>
/// AI session list item
/// </summary>
public class AiSessionListItemDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long? ConnectionId { get; set; }
    public string DatabaseType { get; set; } = string.Empty;
    public string LastUserMessage { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create session request
/// </summary>
public class CreateSessionRequestDto
{
    public string Title { get; set; } = string.Empty;
    public long? ConnectionId { get; set; }
    public string DatabaseType { get; set; } = string.Empty;
}

/// <summary>
/// AI message DTO
/// </summary>
public class AiMessageDto
{
    public long Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? SqlGenerated { get; set; }
    public string? ErrorMessage { get; set; }
    public double? TokenUsage { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
