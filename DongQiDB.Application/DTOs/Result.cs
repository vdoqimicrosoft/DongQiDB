using DongQiDB.Domain.Common;

namespace DongQiDB.Application.DTOs;

/// <summary>
/// Unified response wrapper
/// </summary>
/// <typeparam name="T">Response data type</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public ErrorCode ErrorCode { get; }
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, T? data, ErrorCode errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Ok(T data) => new(true, data, ErrorCode.Success, null);
    public static Result<T> Fail(ErrorCode errorCode, string message) => new(false, default, errorCode, message);
}
