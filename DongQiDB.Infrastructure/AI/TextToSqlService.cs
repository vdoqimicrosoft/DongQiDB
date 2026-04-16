using System.Text.Json;
using DongQiDB.Application.DTOs;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Common;
using DongQiDB.Infrastructure.AI.Prompts;
using DongQiDB.Infrastructure.Configuration;
using DongQiDB.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace DongQiDB.Infrastructure.AI;

/// <summary>
/// Text-to-SQL service implementation
/// </summary>
public class TextToSqlService : ITextToSqlService
{
    private readonly IAiService _aiService;
    private readonly ISchemaService _schemaService;
    private readonly IInputFilter _inputFilter;
    private readonly AppSettings _appSettings;
    private readonly ILogger<TextToSqlService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public TextToSqlService(
        IAiService aiService,
        ISchemaService schemaService,
        IInputFilter inputFilter,
        AppSettings appSettings,
        ILogger<TextToSqlService> logger)
    {
        _aiService = aiService;
        _schemaService = schemaService;
        _inputFilter = inputFilter;
        _appSettings = appSettings;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Result<TextToSqlResponse>> ConvertAsync(
        TextToSqlRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Filter input
            var filterResult = _inputFilter.Filter(request.UserQuestion);
            if (!filterResult.IsValid)
            {
                return Result<TextToSqlResponse>.Fail(ErrorCode.InvalidInput, filterResult.Warning ?? "Invalid input");
            }

            var cleanedQuestion = filterResult.CleanedInput;

            // Get schema using provided context (decrypted password)
            var schema = await GetSchemaAsync(request.ConnectionId, request.Context ?? "", request.SchemaName, cancellationToken);

            // Build prompts
            var systemPrompt = PromptTemplates.TextToSqlSystemPrompt;
            var userPrompt = string.Format(
                PromptTemplates.TextToSqlUserPrompt,
                schema,
                cleanedQuestion,
                request.DatabaseType);

            // Call AI
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
                    MaxTokens = 4096
                }
            };

            var response = await _aiService.ChatAsync(aiRequest, cancellationToken);

            _logger.LogInformation("AI Response content: {Content}", response.Content);

            // Parse response
            var result = ParseSqlResponse(response.Content, request.DatabaseType);

            return Result<TextToSqlResponse>.Ok(result);
        }
        catch (AiServiceException ex)
        {
            _logger.LogError(ex, "AI service error in TextToSql");
            return Result<TextToSqlResponse>.Fail(ErrorCode.AiServiceError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting text to SQL");
            return Result<TextToSqlResponse>.Fail(ErrorCode.SqlGenerationError, ex.Message);
        }
    }

    public async IAsyncEnumerable<string> ConvertStreamAsync(
        TextToSqlRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var filterResult = _inputFilter.Filter(request.UserQuestion);
        var cleanedQuestion = filterResult.IsValid ? filterResult.CleanedInput : request.UserQuestion;

        // Get schema using provided context (decrypted password)
        var schema = await GetSchemaAsync(request.ConnectionId, request.Context ?? "", request.SchemaName, cancellationToken);

        var systemPrompt = PromptTemplates.TextToSqlSystemPrompt;
        var userPrompt = string.Format(
            PromptTemplates.TextToSqlUserPrompt,
            schema,
            cleanedQuestion,
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
                MaxTokens = 4096
            }
        };

        await foreach (var chunk in _aiService.ChatStreamAsync(aiRequest, cancellationToken))
        {
            yield return chunk;
        }
    }

    private async Task<string> GetSchemaAsync(
        long connectionId,
        string password,
        string? schemaName,
        CancellationToken cancellationToken)
    {
        var schemaResult = await _schemaService.GetFullSchemaAsync(connectionId, password, schemaName, cancellationToken);

        var schemaText = new System.Text.StringBuilder();
        schemaText.AppendLine("## Available Tables:");

        foreach (var table in schemaResult.Tables)
        {
            schemaText.AppendLine($"\n### {table.TableName}");
            if (!string.IsNullOrEmpty(table.TableComment))
            {
                schemaText.AppendLine($"Description: {table.TableComment}");
            }

            var columns = schemaResult.Columns.Where(c => c.TableId == table.Id).ToList();
            if (columns.Any())
            {
                schemaText.AppendLine("Columns:");
                foreach (var col in columns)
                {
                    var nullable = col.IsNullable ? "NULL" : "NOT NULL";
                    var primary = col.IsPrimaryKey ? " PRIMARY KEY" : "";
                    schemaText.AppendLine($"  - {col.ColumnName} ({col.DataType}) {nullable}{primary}");
                }
            }

            var indexes = schemaResult.Indexes.Where(i => i.TableId == table.Id).ToList();
            if (indexes.Any())
            {
                schemaText.AppendLine("Indexes:");
                foreach (var idx in indexes)
                {
                    schemaText.AppendLine($"  - {idx.IndexName} on ({idx.Columns})");
                }
            }
        }

        return schemaText.ToString();
    }

    private TextToSqlResponse ParseSqlResponse(string content, string databaseType)
    {
        try
        {
            // Try to extract JSON from response
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = content[jsonStart..(jsonEnd + 1)];
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonOptions);

                if (parsed != null)
                {
                    return new TextToSqlResponse
                    {
                        SqlQuery = GetStringValue(parsed, "sql"),
                        Explanation = GetStringValue(parsed, "explanation"),
                        DatabaseType = databaseType,
                        TablesUsed = GetStringListValue(parsed, "tables_used"),
                        Parameters = GetStringListValue(parsed, "parameters"),
                        IsValid = !string.IsNullOrEmpty(GetStringValue(parsed, "sql")),
                        Confidence = GetDoubleValue(parsed, "confidence", 0.5)
                    };
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse SQL response, attempting fallback");
        }

        // Fallback: return raw content as SQL
        return new TextToSqlResponse
        {
            SqlQuery = content.Trim(),
            Explanation = "Generated SQL (parsing failed)",
            DatabaseType = databaseType,
            IsValid = false,
            ErrorMessage = "Failed to parse AI response as JSON"
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

    private static double GetDoubleValue(Dictionary<string, JsonElement> dict, string key, double defaultValue)
    {
        return dict.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Number
            ? element.GetDouble()
            : defaultValue;
    }
}
