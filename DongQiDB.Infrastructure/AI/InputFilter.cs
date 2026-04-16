using System.Text.RegularExpressions;
using DongQiDB.Application.Interfaces;
using DongQiDB.Infrastructure.AI.Prompts;
using Microsoft.Extensions.Logging;

namespace DongQiDB.Infrastructure.AI;

/// <summary>
/// Input filtering service implementation
/// </summary>
public class InputFilter : IInputFilter
{
    private readonly ILogger<InputFilter> _logger;

    // SQL injection patterns
    private static readonly Regex[] SqlInjectionPatterns = new[]
    {
        new Regex(@"(\bUNION\b.*\bSELECT\b|\bSELECT\b.*\bFROM\b.*\bWHERE\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(\bDROP\b|\bDELETE\b|\bTRUNCATE\b|\bALTER\b|\bCREATE\b|\bINSERT\b|\bUPDATE\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(--|\/\*|\*\/|;|\bOR\b\s+\d+\s*=\s*\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(\bEXEC\b|\bEXECUTE\b|\b xp_)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    // Intent patterns
    private static readonly Dictionary<string, string[]> IntentPatterns = new()
    {
        ["select"] = new[] { "show", "get", "find", "list", "display", "fetch", "retrieve", "query" },
        ["count"] = new[] { "how many", "count", "number of", "total", "sum" },
        ["aggregate"] = new[] { "average", "min", "max", "total", "sum", "group" },
        ["join"] = new[] { "join", "combine", "merge", "together", "both" },
        ["filter"] = new[] { "where", "filter", "only", "with", "having", "containing" },
        ["sort"] = new[] { "order", "sort", "ascending", "descending", "by" },
        ["limit"] = new[] { "top", "first", "last", "limit", "page" },
    };

    public InputFilter(ILogger<InputFilter> logger)
    {
        _logger = logger;
    }

    public FilterResult Filter(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new FilterResult
            {
                CleanedInput = string.Empty,
                IsValid = false,
                Warning = "Input is empty"
            };
        }

        var result = new FilterResult();
        var removedNoise = new List<string>();
        var cleaned = input.Trim();

        // Remove noise words
        foreach (var noise in PromptTemplates.NoiseWords)
        {
            var pattern = new Regex($@"\b{Regex.Escape(noise)}\b", RegexOptions.IgnoreCase);
            if (pattern.IsMatch(cleaned))
            {
                removedNoise.Add(noise);
                cleaned = pattern.Replace(cleaned, "");
            }
        }

        // Normalize whitespace
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        // Remove extra punctuation
        cleaned = Regex.Replace(cleaned, @"[,\.\!\?]{2,}", ",");

        result.CleanedInput = cleaned;
        result.RemovedNoise = removedNoise;
        result.IsValid = cleaned.Length >= 2; // Minimum 2 characters

        if (cleaned.Length < 2)
        {
            result.Warning = "Input too short after filtering";
        }

        return result;
    }

    public IntentResult ExtractIntent(string input)
    {
        var result = new IntentResult
        {
            Intent = "query", // Default intent
            Confidence = 0.5
        };

        var lowerInput = input.ToLowerInvariant();
        var matchedIntents = new List<(string Intent, double Confidence)>();

        foreach (var (intent, patterns) in IntentPatterns)
        {
            foreach (var pattern in patterns)
            {
                if (lowerInput.Contains(pattern))
                {
                    matchedIntents.Add((intent, 0.8));
                    break;
                }
            }
        }

        if (matchedIntents.Any())
        {
            // Use the most confident intent
            var best = matchedIntents.OrderByDescending(x => x.Confidence).First();
            result.Intent = best.Intent;
            result.Confidence = best.Confidence;

            // Extract simple entities (table names from context)
            // This is a simplified version - could be enhanced with NLP
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                // Potential table names (capitalized words that aren't common keywords)
                if (char.IsUpper(word[0]) && !PromptTemplates.SqlKeywords.Contains(word.ToUpperInvariant()))
                {
                    result.Entities["table"] = word;
                }
            }
        }

        return result;
    }

    public ParameterResult ExtractParameters(string input)
    {
        var result = new ParameterResult();

        // Extract date patterns
        var datePatterns = new[]
        {
            @"\d{4}-\d{2}-\d{2}",  // 2024-01-01
            @"\d{1,2}/\d{1,2}/\d{2,4}", // 01/01/2024
            @"(today|yesterday|last week|last month)",
        };

        foreach (var pattern in datePatterns)
        {
            var matches = Regex.Matches(input, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                result.Parameters["date"] = match.Value;
            }
        }

        // Extract numbers (potential limits, IDs)
        var numberPattern = new Regex(@"\b(\d+)\b");
        var numbers = numberPattern.Matches(input);
        foreach (Match match in numbers)
        {
            if (int.TryParse(match.Value, out var num) && num > 0 && num < 10000)
            {
                // Could be limit, offset, or ID
                if (input.ToLowerInvariant().Contains("top") || input.ToLowerInvariant().Contains("first") || input.ToLowerInvariant().Contains("limit"))
                {
                    result.Parameters["limit"] = num;
                }
                else if (!result.Parameters.ContainsKey("id"))
                {
                    result.Parameters["id"] = num;
                }
            }
        }

        // Extract quoted strings as potential search values
        var quotedPattern = new Regex(@"""([^""]+)""");
        var quoted = quotedPattern.Matches(input);
        foreach (Match match in quoted)
        {
            result.Parameters["search"] = match.Groups[1].Value;
        }

        return result;
    }

    public SecurityResult SecurityCheck(string input)
    {
        var result = new SecurityResult
        {
            IsSafe = true,
            Threats = new List<string>(),
            SanitizedContent = new List<string>()
        };

        if (string.IsNullOrWhiteSpace(input))
        {
            return result;
        }

        // Check for SQL injection patterns
        foreach (var pattern in SqlInjectionPatterns)
        {
            if (pattern.IsMatch(input))
            {
                result.IsSafe = false;
                result.Threats.Add($"Potential SQL injection pattern detected: {pattern}");
                _logger.LogWarning("Potential SQL injection detected: {Input}", input);
            }
        }

        // Check for very long input (potential DoS)
        if (input.Length > 10000)
        {
            result.IsSafe = false;
            result.Threats.Add("Input exceeds maximum length");
        }

        // Check for suspicious patterns
        if (input.Contains("<script") || input.Contains("javascript:"))
        {
            result.IsSafe = false;
            result.Threats.Add("Potential XSS attack detected");
        }

        // If not safe, sanitize the input
        if (!result.IsSafe)
        {
            var sanitized = Regex.Replace(input, @"[^\w\s\-_\.\,\:\;]", "");
            result.SanitizedContent.Add(sanitized);
        }

        return result;
    }
}
