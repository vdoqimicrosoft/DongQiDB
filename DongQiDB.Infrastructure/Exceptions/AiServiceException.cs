using DongQiDB.Domain.Common;

namespace DongQiDB.Infrastructure.Exceptions;

/// <summary>
/// AI service related exception
/// </summary>
public class AiServiceException : BusinessException
{
    public string? Provider { get; }
    public string? Operation { get; }

    public AiServiceException(string message, string? provider = null, string? operation = null)
        : base(ErrorCode.AiServiceError, message)
    {
        Provider = provider;
        Operation = operation;
    }

    public AiServiceException(string message, Exception inner)
        : base(ErrorCode.AiServiceError, message, inner)
    {
    }
}
