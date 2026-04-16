using System.Text.Json;
using System.Text.Json.Serialization;

namespace DongQiDB.Infrastructure.Utilities;

/// <summary>
/// JSON serialization utility
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string Serialize<T>(T obj)
        => JsonSerializer.Serialize(obj, Options);

    public static T? Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, Options);

    public static T? Deserialize<T>(byte[] bytes)
        => JsonSerializer.Deserialize<T>(bytes, Options);

    public static bool TryDeserialize<T>(string json, out T? result)
    {
        try
        {
            result = Deserialize<T>(json);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}
