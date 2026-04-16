using System.Text.Json;
using DongQiDB.Application.Interfaces;
using DongQiDB.Infrastructure.AI.Prompts;
using DongQiDB.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace DongQiDB.Infrastructure.AI;

/// <summary>
/// SQL optimization service implementation
/// </summary>
public class SqlOptimizeService : ISqlOptimizeService
{
    private readonly IAiService _aiService;
    private readonly ILogger<SqlOptimizeService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SqlOptimizeService(IAiService aiService, ILogger<SqlOptimizeService> logger)
    {
        _aiService = aiService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<SqlOptimizeResponse> OptimizeAsync(
        SqlOptimizeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = PromptTemplates.SqlOptimizeSystemPrompt;
            var userPrompt = string.Format(
                PromptTemplates.SqlOptimizeUserPrompt,
                request.SqlQuery,
                request.DatabaseType,
                request.TargetGoal ?? "balance");

            var aiRequest = new AiChatRequest
            {
                SystemPrompt = systemPrompt,
                Messages = new List<AiMessageItem>
                {
                    new() { Role = "user", Content = userPrompt }
                },
                Options = new AiOptions
                {
                    Temperature = 0.2,
                    MaxTokens = 4096
                }
            };

            var response = await _aiService.ChatAsync(aiRequest, cancellationToken);

            return ParseOptimization(response.Content);
        }
        catch (AiServiceException ex)
        {
            _logger.LogError(ex, "AI service error in SqlOptimize");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing SQL");
            throw new AiServiceException("Failed to optimize SQL", ex);
        }
    }

    private SqlOptimizeResponse ParseOptimization(string content)
    {
        try
        {
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = content[jsonStart..(jsonEnd + 1)];
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonOptions);

                if (parsed != null)
                {
                    return new SqlOptimizeResponse
                    {
                        OptimizedSql = GetStringValue(parsed, "optimized_sql"),
                        Explanation = GetStringValue(parsed, "explanation"),
                        EstimatedImprovement = GetDoubleValue(parsed, "estimated_improvement"),
                        Suggestions = GetStringListValue(parsed, "suggestions"),
                        IndexRecommendations = GetStringListValue(parsed, "index_recommendations"),
                        WasModified = GetBoolValue(parsed, "was_modified")
                    };
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse optimization response");
        }

        // Fallback
        return new SqlOptimizeResponse
        {
            OptimizedSql = content.Trim(),
            Explanation = "Optimization result",
            WasModified = false,
            Suggestions = new List<string> { "Failed to parse structured response" }
        };
    }

    private static string GetStringValue(Dictionary<string, JsonElement> dict, string key)
    {
        return dict.TryGetValue(key, out var element) ? element.GetString() ?? string.Empty : string.Empty;
    }

    private static double? GetDoubleValue(Dictionary<string, JsonElement> dict, string key)
    {
        if (!dict.TryGetValue(key, out var element) || !element.ValueKind.Equals(JsonValueKind.Number))
            return null;
        return element.GetDouble();
    }

    private static bool GetBoolValue(Dictionary<string, JsonElement> dict, string key)
    {
        return dict.TryGetValue(key, out var element) && element.ValueKind.Equals(JsonValueKind.True);
    }

    private static List<string> GetStringListValue(Dictionary<string, JsonElement> dict, string key)
    {
        if (!dict.TryGetValue(key, out var element) || !element.ValueKind.Equals(JsonValueKind.Array))
            return new List<string>();

        return element.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString() ?? string.Empty)
            .ToList();
    }
}
