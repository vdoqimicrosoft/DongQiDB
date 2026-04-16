using System.Text.RegularExpressions;

namespace DongQiDB.Infrastructure.Utilities;

/// <summary>
/// Validation utility
/// </summary>
public static class ValidationHelper
{
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
            && result.Scheme is "http" or "https";
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;
        return Regex.IsMatch(phone, @"^1[3-9]\d{9}$");
    }

    public static bool IsInRange(int value, int min, int max)
        => value >= min && value <= max;

    public static bool IsInRange(long value, long min, long max)
        => value >= min && value <= max;

    public static bool IsInRange(double value, double min, double max)
        => value >= min && value <= max;

    public static bool IsPositive(long value) => value > 0;
    public static bool IsPositive(int value) => value > 0;
    public static bool IsPositive(double value) => value > 0;

    public static bool IsNonNegative(long value) => value >= 0;
    public static bool IsNonNegative(int value) => value >= 0;
    public static bool IsNonNegative(double value) => value >= 0;
}
