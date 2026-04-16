namespace DongQiDB.Application.Interfaces;

/// <summary>
/// SQL optimization service interface
/// </summary>
public interface ISqlOptimizeService
{
    /// <summary>
    /// Optimizes SQL query for better performance
    /// </summary>
    Task<SqlOptimizeResponse> OptimizeAsync(SqlOptimizeRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// SQL optimization request model
/// </summary>
public class SqlOptimizeRequest
{
    public string SqlQuery { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = "postgresql";
    public string? TargetGoal { get; set; } // "speed", "memory", "balance"
}

/// <summary>
/// SQL optimization response model
/// </summary>
public class SqlOptimizeResponse
{
    public string OptimizedSql { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public double? EstimatedImprovement { get; set; }
    public List<string> Suggestions { get; set; } = new();
    public List<string> IndexRecommendations { get; set; } = new();
    public bool WasModified { get; set; }
}
