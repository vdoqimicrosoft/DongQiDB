namespace DongQiDB.Application.Interfaces;

/// <summary>
/// AI configuration options
/// </summary>
public class AiOptions
{
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.3;
    public double TopP { get; set; } = 0.9;
    public string? StopSequences { get; set; }
    public bool Stream { get; set; } = false;
    public string ApiVersion { get; set; } = "2023-06-01";
}

/// <summary>
/// Anthropic API configuration
/// </summary>
public class AnthropicConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.anthropic.com";
    public string Model { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2023-06-01";
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxRetries { get; set; } = 3;
    public bool EnableStreaming { get; set; } = true;
}
