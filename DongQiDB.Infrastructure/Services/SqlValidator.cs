using System.Text.RegularExpressions;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Common;
using DongQiDB.Infrastructure.Exceptions;

namespace DongQiDB.Infrastructure.Services;

/// <summary>
/// SQL validator implementation
/// </summary>
public partial class SqlValidator : ISqlValidator
{
    private SqlValidationRules _rules = new();

    private static readonly HashSet<string> ReadOnlyKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "SHOW", "DESCRIBE", "DESC", "EXPLAIN", "WITH"
    };

    private static readonly HashSet<string> WriteKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "DROP", "DELETE", "UPDATE", "INSERT", "ALTER", "CREATE", "TRUNCATE", "REPLACE", "MERGE", "EXEC", "EXECUTE"
    };

    public SqlValidationResult Validate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return SqlValidationResult.Fail("SQL query cannot be empty");

        // Check max length
        if (sql.Length > _rules.MaxQueryLength)
            return SqlValidationResult.Fail($"SQL query exceeds maximum length of {_rules.MaxQueryLength} characters");

        var normalizedSql = sql.Trim();
        var warnings = new List<string>();

        // Check multiple statements
        var statementCount = GetStatementCount(normalizedSql);
        if (!_rules.AllowMultipleStatements && statementCount > 1)
            return SqlValidationResult.Fail("Multiple statements are not allowed");

        // Check if read-only
        var isReadOnly = IsReadOnlyQuery(normalizedSql);

        // Validate based on rules
        if (!isReadOnly)
        {
            if (!_rules.AllowDrop && ContainsKeyword(normalizedSql, "DROP"))
                return SqlValidationResult.Fail("DROP statements are not allowed");

            if (!_rules.AllowDelete && ContainsKeyword(normalizedSql, "DELETE"))
                return SqlValidationResult.Fail("DELETE statements are not allowed");

            if (!_rules.AllowUpdate && ContainsKeyword(normalizedSql, "UPDATE"))
                return SqlValidationResult.Fail("UPDATE statements are not allowed");

            if (!_rules.AllowInsert && ContainsKeyword(normalizedSql, "INSERT"))
                return SqlValidationResult.Fail("INSERT statements are not allowed");

            if (!_rules.AllowAlter && ContainsKeyword(normalizedSql, "ALTER"))
                return SqlValidationResult.Fail("ALTER statements are not allowed");

            if (!_rules.AllowCreate && ContainsKeyword(normalizedSql, "CREATE"))
                return SqlValidationResult.Fail("CREATE statements are not allowed");

            if (!_rules.AllowExecute && (ContainsKeyword(normalizedSql, "EXEC") || ContainsKeyword(normalizedSql, "EXECUTE")))
                return SqlValidationResult.Fail("EXECUTE statements are not allowed");
        }

        // Check SELECT requirement
        if (_rules.RequireSelect && !normalizedSql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !normalizedSql.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase) &&
            !normalizedSql.TrimStart().StartsWith("SHOW", StringComparison.OrdinalIgnoreCase) &&
            !normalizedSql.TrimStart().StartsWith("DESCRIBE", StringComparison.OrdinalIgnoreCase) &&
            !normalizedSql.TrimStart().StartsWith("EXPLAIN", StringComparison.OrdinalIgnoreCase))
            return SqlValidationResult.Fail("Only SELECT queries are allowed");

        // Warn about potential issues
        if (HasParameterizedValues(normalizedSql))
            warnings.Add("Query contains parameterized values - ensure they are properly sanitized");

        if (normalizedSql.Contains("DROP", StringComparison.OrdinalIgnoreCase))
            warnings.Add("Query contains DROP statement - this is a destructive operation");

        return warnings.Count > 0
            ? SqlValidationResult.SuccessWithWarnings(isReadOnly, warnings)
            : SqlValidationResult.Success(isReadOnly);
    }

    public bool IsReadOnlyQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return false;

        var normalizedSql = sql.Trim();

        // Check if starts with read-only keyword
        foreach (var keyword in ReadOnlyKeywords)
        {
            if (normalizedSql.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Also check for common read-only patterns
        if (normalizedSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
            normalizedSql.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public bool ValidateSyntax(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return false;

        try
        {
            // Basic syntax validation
            var trimmed = sql.Trim();

            // Check for unmatched parentheses
            var parenCount = 0;
            var inString = false;
            char? stringChar = null;

            foreach (var c in trimmed)
            {
                if (!inString && (c == '\'' || c == '"'))
                {
                    inString = true;
                    stringChar = c;
                }
                else if (inString && c == stringChar)
                {
                    // Check for escaped quote
                    var index = trimmed.IndexOf(c);
                    if (index + 1 < trimmed.Length && trimmed[index + 1] == c)
                        continue;
                    inString = false;
                    stringChar = null;
                }
                else if (!inString)
                {
                    if (c == '(') parenCount++;
                    else if (c == ')') parenCount--;
                }
            }

            if (parenCount != 0)
                return false;

            // Check for basic SQL keywords
            var upperSql = trimmed.ToUpperInvariant();
            if (!upperSql.Contains("SELECT") && !upperSql.Contains("INSERT") &&
                !upperSql.Contains("UPDATE") && !upperSql.Contains("DELETE") &&
                !upperSql.Contains("CREATE") && !upperSql.Contains("ALTER") &&
                !upperSql.Contains("DROP") && !upperSql.Contains("WITH") &&
                !upperSql.Contains("SHOW") && !upperSql.Contains("EXPLAIN"))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool HasParameterizedValues(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return false;

        // Check for common parameter patterns
        return ParameterPattern().IsMatch(sql) ||
               sql.Contains("@") ||
               sql.Contains("$") ||
               sql.Contains("?");
    }

    public void SetRules(SqlValidationRules rules)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    public SqlValidationRules GetRules() => _rules;

    private static int GetStatementCount(string sql)
    {
        var count = 0;
        var inString = false;
        var inComment = false;
        char? stringChar = null;

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];

            // Handle comments
            if (!inString && i + 1 < sql.Length)
            {
                if (sql[i] == '-' && sql[i + 1] == '-')
                {
                    inComment = true;
                    continue;
                }
                if (sql[i] == '/' && sql[i + 1] == '*')
                {
                    inComment = true;
                    i++;
                    continue;
                }
            }

            if (inComment)
            {
                if (c == '\n') inComment = false;
                else if (i + 1 < sql.Length && c == '*' && sql[i + 1] == '/')
                {
                    inComment = false;
                    i++;
                }
                continue;
            }

            // Handle strings
            if (!inString && (c == '\'' || c == '"'))
            {
                inString = true;
                stringChar = c;
                continue;
            }

            if (inString && c == stringChar)
            {
                // Check for escaped quote
                if (i + 1 < sql.Length && sql[i + 1] == c)
                {
                    i++;
                    continue;
                }
                inString = false;
                stringChar = null;
                continue;
            }

            // Count semicolons outside strings and comments
            if (!inString && !inComment && c == ';')
                count++;
        }

        // Last statement without semicolon
        if (!string.IsNullOrWhiteSpace(sql.Replace(";", "").Trim()))
            count++;

        return count;
    }

    private static bool ContainsKeyword(string sql, string keyword)
    {
        var regex = new Regex($@"\b{keyword}\b", RegexOptions.IgnoreCase);
        return regex.IsMatch(sql);
    }

    [GeneratedRegex(@"'[^']*'|\{[^{}]*\}|\$[a-zA-Z_][a-zA-Z0-9_]*")]
    private static partial Regex ParameterPattern();
}
