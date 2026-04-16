namespace DongQiDB.Domain.Common;

/// <summary>
/// System error codes
/// </summary>
public enum ErrorCode
{
    // Success
    Success = 0,

    // Common errors (1000-1999)
    BadRequest = 1001,
    Unauthorized = 1002,
    Forbidden = 1003,
    NotFound = 1004,
    InternalError = 1005,
    ValidationFailed = 1006,

    // Business errors (2000-2999)
    DatabaseError = 2001,
    AiServiceError = 2002,
    SqlGenerationError = 2003,
    QueryExecutionError = 2004,
    ConnectionError = 2005,

    // Validation errors (3000-3999)
    InvalidInput = 3001,
    MissingRequiredField = 3002,
    InvalidFormat = 3003,
    OutOfRange = 3004,

    // SQL validation errors (4000-4099)
    SqlValidationFailed = 4001,
    SqlNotReadOnly = 4002,
    SqlSyntaxError = 4003,
    SqlContainsWriteOperation = 4004,
    SqlQueryTooLong = 4005,
    SqlMultipleStatementsNotAllowed = 4006,

    // Query execution errors (4100-4199)
    QueryTimeout = 4101,
    QueryCancelled = 4102,
    QueryPlanFailed = 4103,
    ResultSetTooLarge = 4104,

    // History errors (4200-4299)
    HistoryNotFound = 4201,
    HistoryExportFailed = 4202,
}
