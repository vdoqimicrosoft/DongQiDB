using DongQiDB.Domain.Common;

namespace DongQiDB.Infrastructure.Exceptions;

/// <summary>
/// Base business exception
/// </summary>
public class BusinessException : Exception
{
    public ErrorCode ErrorCode { get; }

    public BusinessException(ErrorCode errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public BusinessException(ErrorCode errorCode, string message, Exception inner)
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}
