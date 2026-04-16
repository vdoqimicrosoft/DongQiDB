using DongQiDB.Domain.Common;

namespace DongQiDB.Infrastructure.Exceptions;

/// <summary>
/// Validation exception for input validation errors
/// </summary>
public class ValidationException : BusinessException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(ErrorCode.ValidationFailed, "Validation failed")
    {
        Errors = errors;
    }

    public ValidationException(string field, string message)
        : base(ErrorCode.ValidationFailed, $"{field}: {message}")
    {
        Errors = new Dictionary<string, string[]> { { field, new[] { message } } };
    }
}
