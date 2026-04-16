namespace DongQiDB.Application.Interfaces;

/// <summary>
/// Input filtering service interface
/// </summary>
public interface IInputFilter
{
    /// <summary>
    /// Filters and cleans user input
    /// </summary>
    FilterResult Filter(string input);

    /// <summary>
    /// Extracts intent from user input
    /// </summary>
    IntentResult ExtractIntent(string input);

    /// <summary>
    /// Extracts parameters from user input
    /// </summary>
    ParameterResult ExtractParameters(string input);

    /// <summary>
    /// Performs security check on input
    /// </summary>
    SecurityResult SecurityCheck(string input);
}

/// <summary>
/// Filter result
/// </summary>
public class FilterResult
{
    public string CleanedInput { get; set; } = string.Empty;
    public List<string> RemovedNoise { get; set; } = new();
    public bool IsValid { get; set; } = true;
    public string? Warning { get; set; }
}

/// <summary>
/// Intent extraction result
/// </summary>
public class IntentResult
{
    public string Intent { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public Dictionary<string, string> Entities { get; set; } = new();
}

/// <summary>
/// Parameter extraction result
/// </summary>
public class ParameterResult
{
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> UnmatchedTerms { get; set; } = new();
}

/// <summary>
/// Security check result
/// </summary>
public class SecurityResult
{
    public bool IsSafe { get; set; } = true;
    public List<string> Threats { get; set; } = new();
    public List<string> SanitizedContent { get; set; } = new();
}
