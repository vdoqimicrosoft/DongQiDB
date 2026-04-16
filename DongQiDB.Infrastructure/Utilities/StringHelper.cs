using System.Text.RegularExpressions;

namespace DongQiDB.Infrastructure.Utilities;

/// <summary>
/// String manipulation utility
/// </summary>
public static class StringHelper
{
    public static bool IsNullOrEmpty(string? value)
        => string.IsNullOrEmpty(value);

    public static bool IsNullOrWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value);

    public static string? NullIfEmpty(string? value)
        => string.IsNullOrEmpty(value) ? null : value;

    public static string Trim(string? value)
        => value?.Trim() ?? string.Empty;

    public static string ToCamelCase(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToLower(value[0]) + value[1..];

    public static string ToPascalCase(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToUpper(value[0]) + value[1..];

    public static string ToSnakeCase(string value)
        => Regex.Replace(value, "([A-Z])", "_$1").TrimStart('_').ToLower();

    public static bool ContainsIgnoreCase(string source, string value)
        => source.Contains(value, StringComparison.OrdinalIgnoreCase);

    public static bool EqualsIgnoreCase(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    public static string JoinNotEmpty(string separator, params string?[] values)
        => string.Join(separator, values.Where(v => !string.IsNullOrWhiteSpace(v)));
}
