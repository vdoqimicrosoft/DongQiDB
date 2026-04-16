namespace DongQiDB.Infrastructure.Utilities;

/// <summary>
/// Collection manipulation utility
/// </summary>
public static class CollectionHelper
{
    public static bool IsNullOrEmpty<T>(IReadOnlyCollection<T>? collection)
        => collection is null || collection.Count == 0;

    public static bool HasItems<T>(IReadOnlyCollection<T>? collection)
        => collection is not null && collection.Count > 0;

    public static List<T> EmptyIfNull<T>(List<T>? list)
        => list ?? new List<T>();

    public static IEnumerable<T> EmptyIfNull<T>(IEnumerable<T>? source)
        => source ?? Enumerable.Empty<T>();

    public static void AddIfNotNull<T>(ICollection<T> collection, T? item) where T : class
    {
        if (item is not null)
            collection.Add(item);
    }

    public static IReadOnlyList<T> ToReadOnlyList<T>(IEnumerable<T> source)
        => source.ToList().AsReadOnly();

    public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
        Dictionary<TKey, TValue> first,
        Dictionary<TKey, TValue> second) where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>(first);
        foreach (var kvp in second)
            result[kvp.Key] = kvp.Value;
        return result;
    }
}
