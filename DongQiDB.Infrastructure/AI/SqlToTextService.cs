using System.Text.Json;
using DongQiDB.Application.Interfaces;
using DongQiDB.Infrastructure.AI.Prompts;
using DongQiDB.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace DongQiDB.Infrastructure.AI;

/// <summary>
/// SQL to natural language explanation service implementation
/// </summary>
public class SqlToTextService : ISqlToTextService
{
    private readonly IAiService _aiService;
    private readonly ILogger<SqlToTextService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SqlToTextService(IAiService aiService, ILogger<SqlToTextService> logger)
    {
        _aiService = aiService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<SqlToTextResponse> ExplainAsync(
        SqlToTextRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = PromptTemplates.SqlToTextSystemPrompt;
            var userPrompt = string.Format(
                PromptTemplates.SqlToTextUserPrompt,
                request.SqlQuery,
                request.DatabaseType);

            var aiRequest = new AiChatRequest
            {
                SystemPrompt = systemPrompt,
                Messages = new List<AiMessageItem>
                {
                    new() { Role = "user", Content = userPrompt }
                },
                Options = new AiOptions
                {
                    Temperature = 0.3,
                    MaxTokens = 2048
                }
            };

            var response = await _aiService.ChatAsync(aiRequest, cancellationToken);

            return ParseExplanation(response.Content);
        }
        catch (AiServiceException ex)
        {
            _logger.LogError(ex, "AI service error in SqlToText");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining SQL");
            throw new AiServiceException("Failed to explain SQL", ex);
        }
    }

    private SqlToTextResponse ParseExplanation(string content)
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
                    return new SqlToTextResponse
                    {
                        Summary = GetStringValue(parsed, "summary"),
                        Explanation = GetStringValue(parsed, "explanation"),
                        TablesInvolved = GetStringListValue(parsed, "tables_involved"),
                        Operations = GetStringListValue(parsed, "operations"),
                        Conditions = GetConditionList(parsed),
                        Warnings = GetStringListValue(parsed, "warnings")
                    };
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse explanation response");
        }

        // Fallback
        return new SqlToTextResponse
        {
            Explanation = content.Trim(),
            Summary = "SQL explanation",
            Warnings = new List<string> { "Failed to parse structured response" }
        };
    }

    private static string GetStringValue(Dictionary<string, JsonElement> dict, string key)
    {
        return dict.TryGetValue(key, out var element) ? element.GetString() ?? string.Empty : string.Empty;
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

    private static List<SqlConditionInfo> GetConditionList(Dictionary<string, JsonElement> dict)
    {
        if (!dict.TryGetValue("conditions", out var element) || !element.ValueKind.Equals(JsonValueKind.Array))
            return new List<SqlConditionInfo>();

        var conditions = new List<SqlConditionInfo>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                conditions.Add(new SqlConditionInfo
                {
                    Column = item.TryGetProperty("column", out var col) ? col.GetString() ?? "" : "",
                    Operator = item.TryGetProperty("operator", out var op) ? op.GetString() ?? "" : "",
                    Value = item.TryGetProperty("value", out var val) ? val.GetString() ?? "" : ""
                });
            }
        }
        return conditions;
    }
}
