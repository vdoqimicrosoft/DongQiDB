using System.Text;
using System.Text.RegularExpressions;
using DongQiDB.Application.Interfaces;

namespace DongQiDB.Infrastructure.Services;

/// <summary>
/// SQL formatter implementation
/// </summary>
public partial class SqlFormatter : ISqlFormatter
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "FROM", "WHERE", "AND", "OR", "NOT", "IN", "LIKE", "BETWEEN",
        "JOIN", "INNER", "LEFT", "RIGHT", "OUTER", "FULL", "CROSS", "ON",
        "GROUP", "BY", "HAVING", "ORDER", "ASC", "DESC", "NULL", "IS",
        "AS", "DISTINCT", "TOP", "LIMIT", "OFFSET", "UNION", "ALL",
        "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE", "CREATE",
        "TABLE", "INDEX", "DROP", "ALTER", "ADD", "COLUMN", "PRIMARY",
        "KEY", "FOREIGN", "REFERENCES", "UNIQUE", "CHECK", "DEFAULT",
        "CONSTRAINT", "EXISTS", "CASE", "WHEN", "THEN", "ELSE", "END",
        "WITH", "RECURSIVE", "OVER", "PARTITION", "WINDOW", "ROWS",
        "RANGE", "PRECEDING", "FOLLOWING", "CURRENT", "ROW", "UNBOUNDED",
        "EXPLAIN", "ANALYZE", "COALESCE", "NULLIF", "CAST", "CONVERT",
        "COUNT", "SUM", "AVG", "MIN", "MAX", "ROUND", "FLOOR", "CEIL",
        "LENGTH", "UPPER", "LOWER", "TRIM", "SUBSTRING", "CONCAT",
        "TRUE", "FALSE", "BOOLEAN", "INTEGER", "VARCHAR", "TEXT",
        "DATE", "TIME", "TIMESTAMP", "DATETIME", "INTERVAL", "YEAR",
        "MONTH", "DAY", "HOUR", "MINUTE", "SECOND", "NOW", "CURRENT_TIMESTAMP",
        "TRUNCATE", "REPLACE", "MERGE", "EXEC", "EXECUTE", "PROCEDURE",
        "FUNCTION", "TRIGGER", "VIEW"
    };

    public string Format(string sql) => Format(sql, new SqlFormattingOptions());

    public string Format(string sql, SqlFormattingOptions options)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return string.Empty;

        var result = sql.Trim();

        // Normalize whitespace
        result = NormalizeWhitespace(result);

        // Format keywords
        result = FormatKeywords(result, options);

        // Add newlines
        result = AddNewLines(result);

        // Apply indentation
        result = ApplyIndentation(result, options);

        return result.Trim();
    }

    public string FormatWithIndent(string sql, int indentSize = 2)
    {
        var options = new SqlFormattingOptions { IndentSize = indentSize };
        return Format(sql, options);
    }

    public string FormatWithHighlighting(string sql, SqlFormattingOptions? options = null)
    {
        options ??= new SqlFormattingOptions { HighlightKeywords = true };

        if (string.IsNullOrWhiteSpace(sql))
            return string.Empty;

        var formatted = Format(sql, options);
        var sb = new StringBuilder();

        foreach (var token in Tokenize(formatted))
        {
            if (Keywords.Contains(token.Value))
            {
                sb.Append($"<span class=\"sql-keyword\">{token.Value}</span>");
            }
            else if (token.Type == TokenType.String)
            {
                sb.Append($"<span class=\"sql-string\">{token.Value}</span>");
            }
            else if (token.Type == TokenType.Number)
            {
                sb.Append($"<span class=\"sql-number\">{token.Value}</span>");
            }
            else if (token.Type == TokenType.Comment)
            {
                sb.Append($"<span class=\"sql-comment\">{token.Value}</span>");
            }
            else
            {
                sb.Append(token.Value);
            }
        }

        return sb.ToString();
    }

    public string Minify(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return string.Empty;

        // Remove comments
        var result = RemoveComments(sql);

        // Collapse whitespace
        result = WhitespaceCollapseRegex().Replace(result, " ");

        // Remove leading/trailing whitespace
        return result.Trim();
    }

    private static string NormalizeWhitespace(string sql)
    {
        // Replace multiple spaces with single space
        var result = MultipleSpacesRegex().Replace(sql, " ");

        // Normalize operators
        result = result.Replace("  >", " >");
        result = result.Replace("  <", " <");
        result = result.Replace("  =", " =");
        result = result.Replace(">  ", "> ");
        result = result.Replace("<  ", "< ");
        result = result.Replace("=  ", "= ");

        // Add space around operators
        result = OperatorRegex().Replace(result, " $1 ");

        // Normalize commas
        result = result.Replace(",", ", ");

        // Remove extra whitespace around parentheses
        result = result.Replace("( ", "(");
        result = result.Replace(" )", ")");

        return result.Trim();
    }

    private static string FormatKeywords(string sql, SqlFormattingOptions options)
    {
        if (!options.UppercaseKeywords)
            return sql;

        var result = sql;
        foreach (var keyword in Keywords)
        {
            // Match whole word only
            var pattern = $@"\b{Regex.Escape(keyword)}\b";
            result = Regex.Replace(result, pattern, keyword, RegexOptions.IgnoreCase);
        }

        return result;
    }

    private static string AddNewLines(string sql)
    {
        var result = sql;

        // New line before major clauses
        var clauses = new[] { "WHERE", "GROUP BY", "ORDER BY", "HAVING",
            "LIMIT", "OFFSET", "UNION", "EXCEPT", "INTERSECT", "JOIN",
            "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN",
            "CROSS JOIN", "FROM", "AND", "OR", "SET", "SELECT" };

        foreach (var clause in clauses)
        {
            var pattern = $@"\s+{Regex.Escape(clause)}\b";
            result = Regex.Replace(result, pattern, $"\n{clause}", RegexOptions.IgnoreCase);
        }

        return result;
    }

    private static string ApplyIndentation(string sql, SqlFormattingOptions options)
    {
        var indent = options.UseTabs ? "\t" : new string(' ', options.IndentSize);
        var lines = sql.Split('\n');
        var sb = new StringBuilder();
        var baseIndentLevel = 0;
        var previousClause = "";

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip the first line (SELECT usually)
            if (i == 0)
            {
                sb.AppendLine(line);
                continue;
            }

            // Determine indentation level
            var upperLine = line.ToUpperInvariant();

            // Increase indent after SELECT, FROM, WHERE, JOIN, SET
            if (upperLine.StartsWith("FROM") || upperLine.StartsWith("JOIN") ||
                upperLine.StartsWith("WHERE") || upperLine.StartsWith("SET"))
            {
                baseIndentLevel = 0;
            }

            // Increase indent for sub-clauses
            var increaseIndent = upperLine.StartsWith("AND ") ||
                                 upperLine.StartsWith("OR ") ||
                                 upperLine.StartsWith("ORDER BY") ||
                                 upperLine.StartsWith("GROUP BY") ||
                                 upperLine.StartsWith("HAVING") ||
                                 upperLine.StartsWith("ON ");

            if (increaseIndent)
                baseIndentLevel++;

            // Decrease indent for closing clauses
            var decreaseIndent = upperLine.StartsWith(")") ||
                                 upperLine.StartsWith("GROUP BY") ||
                                 upperLine.StartsWith("ORDER BY") ||
                                 upperLine.StartsWith("LIMIT");

            if (decreaseIndent && baseIndentLevel > 0)
                baseIndentLevel--;

            sb.AppendLine(indent.Repeat(baseIndentLevel) + line);
            previousClause = upperLine;
        }

        return sb.ToString();
    }

    private static string RemoveComments(string sql)
    {
        // Remove single-line comments
        var result = SingleLineCommentRegex().Replace(sql, "");

        // Remove multi-line comments
        result = MultiLineCommentRegex().Replace(result, "");

        return result;
    }

    private static IEnumerable<Token> Tokenize(string sql)
    {
        var inString = false;
        var stringChar = '\0';
        var currentToken = new StringBuilder();

        foreach (var c in sql)
        {
            if (!inString)
            {
                if (c == '\'' || c == '"')
                {
                    if (currentToken.Length > 0)
                    {
                        yield return new Token(currentToken.ToString(), TokenType.Word);
                        currentToken.Clear();
                    }

                    inString = true;
                    stringChar = c;
                    currentToken.Append(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (currentToken.Length > 0)
                    {
                        var token = currentToken.ToString();
                        yield return new Token(token, Keywords.Contains(token) ? TokenType.Keyword : TokenType.Word);
                        currentToken.Clear();
                    }
                }
                else
                {
                    currentToken.Append(c);
                }
            }
            else
            {
                currentToken.Append(c);
                if (c == stringChar)
                {
                    // Check for escaped quote
                    var index = sql.IndexOf(c);
                    if (index + 1 < sql.Length && sql[index + 1] == c)
                    {
                        currentToken.Append(sql[index + 1]);
                    }
                    else
                    {
                        yield return new Token(currentToken.ToString(), TokenType.String);
                        currentToken.Clear();
                        inString = false;
                    }
                }
            }
        }

        if (currentToken.Length > 0)
        {
            var token = currentToken.ToString();
            yield return new Token(token, Keywords.Contains(token) ? TokenType.Keyword : TokenType.Word);
        }
    }

    private enum TokenType { Word, Keyword, String, Number, Comment }

    private record Token(string Value, TokenType Type);

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex(@"\s*([=<>!]+)\s*")]
    private static partial Regex OperatorRegex();

    [GeneratedRegex(@"--[^\n]*")]
    private static partial Regex SingleLineCommentRegex();

    [GeneratedRegex(@"/\*[\s\S]*?\*/")]
    private static partial Regex MultiLineCommentRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceCollapseRegex();
}

file static class StringExtensions
{
    public static string Repeat(this string s, int count) =>
        string.Concat(Enumerable.Repeat(s, count));
}
