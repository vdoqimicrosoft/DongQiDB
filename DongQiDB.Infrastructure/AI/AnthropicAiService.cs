using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DongQiDB.Application.Interfaces;
using DongQiDB.Infrastructure.AI.Models;
using DongQiDB.Infrastructure.Configuration;
using DongQiDB.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace DongQiDB.Infrastructure.AI;

/// <summary>
/// Anthropic API based AI service implementation
/// </summary>
public class AnthropicAiService : IAiService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AnthropicConfig _config;
    private readonly ILogger<AnthropicAiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AnthropicAiService(AnthropicConfig config, ILogger<AnthropicAiService> logger)
    {
        _config = config;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_config.Endpoint),
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", _config.ApiVersion);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        var anthropicRequest = BuildRequest(request);

        var json = JsonSerializer.Serialize(anthropicRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await ExecuteWithRetryAsync(async () =>
        {
            var httpResponse = await _httpClient.PostAsync("/v1/messages", content, cancellationToken);
            return httpResponse;
        }, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogInformation("Raw API response: {Response}", responseContent);

        if (!response.IsSuccessStatusCode)
        {
            var error = TryDeserializeError(responseContent);
            throw new AiServiceException(
                error?.Error?.Message ?? $"API request failed with status {response.StatusCode}",
                "Anthropic",
                "Chat");
        }

        var anthropicResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseContent, _jsonOptions)
            ?? throw new AiServiceException("Failed to deserialize API response", "Anthropic", "Chat");

        // Get text content (skip thinking blocks)
        var contentText = anthropicResponse.Content
            .FirstOrDefault(c => c.Type == "text")?.Text ?? string.Empty;

        return new AiChatResponse
        {
            Content = contentText,
            Model = anthropicResponse.Model,
            TokensUsed = (anthropicResponse.Usage?.InputTokens ?? 0) + (anthropicResponse.Usage?.OutputTokens ?? 0),
            StopReason = anthropicResponse.StopReason
        };
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        AiChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var anthropicRequest = BuildRequest(request, stream: true);
        anthropicRequest.Stream = true;

        var json = JsonSerializer.Serialize(anthropicRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await ExecuteWithRetryAsync(async () =>
        {
            var httpResponse = await _httpClient.PostAsync("/v1/messages", content, cancellationToken);
            return httpResponse;
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = TryDeserializeError(responseContent);
            throw new AiServiceException(
                error?.Error?.Message ?? $"API request failed with status {response.StatusCode}",
                "Anthropic",
                "ChatStream");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line[6..];
            if (data == "[DONE]")
                break;

            var text = ParseStreamChunk(data);
            if (text != null)
            {
                yield return text;
            }
        }
    }

    private string? ParseStreamChunk(string data)
    {
        try
        {
            var chunk = JsonSerializer.Deserialize<AnthropicStreamChunk>(data, _jsonOptions);
            return chunk?.Delta?.Text;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private AnthropicRequest BuildRequest(AiChatRequest request, bool stream = false)
    {
        return new AnthropicRequest
        {
            Model = !string.IsNullOrEmpty(request.Options.Model)
                ? request.Options.Model
                : _config.Model,
            MaxTokens = request.Options.MaxTokens,
            Temperature = request.Options.Temperature,
            TopP = request.Options.TopP,
            System = request.SystemPrompt,
            Stream = stream,
            Messages = request.Messages.Select(m => new AnthropicMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList()
        };
    }

    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> action,
        CancellationToken cancellationToken)
    {
        var attempts = 0;
        var maxRetries = _config.MaxRetries;

        while (true)
        {
            try
            {
                attempts++;
                var response = await action();

                // Retry on 5xx errors and rate limiting
                if ((int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (attempts < maxRetries)
                    {
                        _logger.LogWarning("Request failed with {StatusCode}, attempt {Attempt}/{MaxRetries}",
                            response.StatusCode, attempts, maxRetries);
                        await Task.Delay(GetRetryDelay(attempts), cancellationToken);
                        continue;
                    }
                }

                return response;
            }
            catch (HttpRequestException ex) when (attempts < maxRetries)
            {
                _logger.LogWarning(ex, "Request exception, attempt {Attempt}/{MaxRetries}", attempts, maxRetries);
                await Task.Delay(GetRetryDelay(attempts), cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout - retry
                if (attempts < maxRetries)
                {
                    _logger.LogWarning("Request timeout, attempt {Attempt}/{MaxRetries}", attempts, maxRetries);
                    await Task.Delay(GetRetryDelay(attempts), cancellationToken);
                    continue;
                }
                throw;
            }
        }
    }

    private static TimeSpan GetRetryDelay(int attempt) => TimeSpan.FromSeconds(Math.Pow(2, attempt));

    private AnthropicError? TryDeserializeError(string content)
    {
        try
        {
            return JsonSerializer.Deserialize<AnthropicError>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
