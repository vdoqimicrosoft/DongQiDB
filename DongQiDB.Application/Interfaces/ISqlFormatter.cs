namespace DongQiDB.Application.Interfaces;

/// <summary>
/// SQL formatting options
/// </summary>
public class SqlFormattingOptions
{
    public bool UseTabs { get; set; } = false;
    public int IndentSize { get; set; } = 2;
    public bool UppercaseKeywords { get; set; } = true;
    public bool AddNewLineBeforeAnd { get; set; } = true;
    public bool AddNewLineBeforeOr { get; set; } = true;
    public int CommaPadding { get; set; } = 1; // 0: no padding, 1: space after comma
    public bool AlignColumnAssignments { get; set; } = false;
    public bool HighlightKeywords { get; set; } = false;
    public bool Colorize { get; set; } = false;
}

/// <summary>
/// SQL formatter interface
/// </summary>
public interface ISqlFormatter
{
    /// <summary>
    /// Formats SQL string with default options
    /// </summary>
    string Format(string sql);

    /// <summary>
    /// Formats SQL with custom options
    /// </summary>
    string Format(string sql, SqlFormattingOptions options);

    /// <summary>
    /// Formats SQL with indentation only
    /// </summary>
    string FormatWithIndent(string sql, int indentSize = 2);

    /// <summary>
    /// Formats SQL with syntax highlighting (returns HTML)
    /// </summary>
    string FormatWithHighlighting(string sql, SqlFormattingOptions? options = null);

    /// <summary>
    /// Minifies SQL (removes extra whitespace)
    /// </summary>
    string Minify(string sql);
}
