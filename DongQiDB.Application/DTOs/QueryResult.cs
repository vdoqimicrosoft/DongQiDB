namespace DongQiDB.Application.DTOs;

/// <summary>
/// Column metadata in result set
/// </summary>
public class ResultColumn
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public TypeCode TypeCode { get; set; }
    public bool IsNullable { get; set; }
    public int Ordinal { get; set; }
}

/// <summary>
/// Row data in result set
/// </summary>
public class ResultRow
{
    public List<object?> Values { get; set; } = new();
    public object? this[int index] => Values.Count > index ? Values[index] : null;
    public T? GetValue<T>(int index) => Values.Count > index ? ConvertValue<T>(Values[index]) : default;
    public T? GetValue<T>(string columnName) => throw new NotImplementedException();

    private static T? ConvertValue<T>(object? value)
    {
        if (value == null || value == DBNull.Value) return default;
        if (typeof(T) == typeof(object)) return (T)value;
        return (T)Convert.ChangeType(value, typeof(T));
    }
}

/// <summary>
/// Result set containing rows and metadata
/// </summary>
public class ResultSet
{
    public List<ResultColumn> Columns { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
    public int RowCount => Rows.Count;
    public bool HasRows => Rows.Count > 0;
    public long ExecutionTimeMs { get; set; }
    public bool IsTruncated { get; set; }
    public int? TotalRowCount { get; set; }

    public ResultRow? FirstOrDefault() => Rows.FirstOrDefault();
    public IEnumerable<ResultRow> Skip(int count) => Rows.Skip(count);
    public IEnumerable<ResultRow> Take(int count) => Rows.Take(count);
}

/// <summary>
/// Query result with metadata
/// </summary>
public class QueryResult
{
    public ResultSet? Data { get; set; }
    public int AffectedRows { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string? Message { get; set; }
    public bool HasData => Data?.HasRows == true;
    public bool IsQuery { get; set; } = true;
    public Guid ExecutionId { get; set; } = Guid.NewGuid();
}
