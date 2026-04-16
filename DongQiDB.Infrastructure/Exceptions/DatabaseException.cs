using DongQiDB.Domain.Common;

namespace DongQiDB.Infrastructure.Exceptions;

/// <summary>
/// Database related exception
/// </summary>
public class DatabaseException : BusinessException
{
    public string? Sql { get; }
    public string? TableName { get; }

    public DatabaseException(string message, string? sql = null, string? tableName = null)
        : base(ErrorCode.DatabaseError, message)
    {
        Sql = sql;
        TableName = tableName;
    }

    public DatabaseException(string message, Exception inner)
        : base(ErrorCode.DatabaseError, message, inner)
    {
    }
}
