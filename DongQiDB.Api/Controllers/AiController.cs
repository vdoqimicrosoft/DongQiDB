using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DongQiDB.Api.DTOs.AI;
using DongQiDB.Application.DTOs;
using DongQiDB.Application.Interfaces;
using DongQiDB.Domain.Common;

namespace DongQiDB.Api.Controllers;

/// <summary>
/// AI-powered Text-to-SQL controller
/// </summary>
[ApiController]
[Route("api/v1/ai")]
[ApiVersion("1")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IConnectionService _connectionService;
    private readonly ITextToSqlService _textToSqlService;
    private readonly ISqlToTextService _sqlToTextService;
    private readonly ISqlOptimizeService _sqlOptimizeService;
    private readonly IAiSessionService _sessionService;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IConnectionService connectionService,
        ITextToSqlService textToSqlService,
        ISqlToTextService sqlToTextService,
        ISqlOptimizeService sqlOptimizeService,
        IAiSessionService sessionService,
        IConnectionManager connectionManager,
        ILogger<AiController> logger)
    {
        _connectionService = connectionService;
        _textToSqlService = textToSqlService;
        _sqlToTextService = sqlToTextService;
        _sqlOptimizeService = sqlOptimizeService;
        _sessionService = sessionService;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// Convert natural language to SQL query
    /// </summary>
    [HttpPost("text-to-sql")]
    [ProducesResponseType(typeof(Result<TextToSqlResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TextToSqlResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TextToSql([FromBody] TextToSqlRequestDto request, CancellationToken cancellationToken)
    {
        if (request.ConnectionId <= 0 || string.IsNullOrEmpty(request.UserQuestion))
        {
            return BadRequest(Result<TextToSqlResponseDto>.Fail(
                ErrorCode.ValidationFailed,
                "ConnectionId and UserQuestion are required"));
        }

        var connection = await _connectionService.GetByIdAsync(request.ConnectionId, cancellationToken);
        if (connection == null)
        {
            return BadRequest(Result<TextToSqlResponseDto>.Fail(
                ErrorCode.NotFound,
                $"Connection {request.ConnectionId} not found"));
        }

        var password = DecryptPassword(connection.EncryptedPassword);

        var textToSqlRequest = new TextToSqlRequest
        {
            ConnectionId = request.ConnectionId,
            UserQuestion = request.UserQuestion,
            SchemaName = request.SchemaName,
            Context = password,
            DatabaseType = connection.DatabaseType.ToString().ToLower(),
            Options = new TextToSqlOptions
            {
                IncludeExplanation = request.IncludeExplanation,
                ValidateOnly = request.ValidateOnly,
                StreamOutput = request.StreamOutput
            }
        };

        var result = await _textToSqlService.ConvertAsync(textToSqlRequest, cancellationToken);

        if (!result.IsSuccess || result.Data == null)
        {
            return Ok(Result<TextToSqlResponseDto>.Fail(
                result.ErrorCode,
                result.ErrorMessage ?? "Text-to-SQL conversion failed"));
        }

        var dto = new TextToSqlResponseDto
        {
            SqlQuery = result.Data.SqlQuery,
            Explanation = result.Data.Explanation,
            DatabaseType = result.Data.DatabaseType,
            TablesUsed = result.Data.TablesUsed,
            Parameters = result.Data.Parameters,
            IsValid = result.Data.IsValid,
            ErrorMessage = result.Data.ErrorMessage,
            Confidence = result.Data.Confidence
        };

        return Ok(Result<TextToSqlResponseDto>.Ok(dto));
    }

    /// <summary>
    /// Explain SQL query in natural language
    /// </summary>
    [HttpPost("sql-to-text")]
    [ProducesResponseType(typeof(Result<SqlToTextResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SqlToTextResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SqlToText([FromBody] SqlToTextRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.SqlQuery))
        {
            return BadRequest(Result<SqlToTextResponseDto>.Fail(
                ErrorCode.ValidationFailed,
                "SqlQuery is required"));
        }

        var sqlToTextRequest = new SqlToTextRequest
        {
            SqlQuery = request.SqlQuery,
            DatabaseType = request.DatabaseType,
            IncludeDetails = request.IncludeDetails
        };

        var result = await _sqlToTextService.ExplainAsync(sqlToTextRequest, cancellationToken);

        var dto = new SqlToTextResponseDto
        {
            Explanation = result.Explanation,
            Summary = result.Summary,
            TablesInvolved = result.TablesInvolved,
            Operations = result.Operations,
            Conditions = result.Conditions.Select(c => new ConditionInfo
            {
                Column = c.Column,
                Operator = c.Operator,
                Value = c.Value
            }).ToList(),
            Warnings = result.Warnings
        };

        return Ok(Result<SqlToTextResponseDto>.Ok(dto));
    }

    /// <summary>
    /// Optimize SQL query
    /// </summary>
    [HttpPost("sql-optimize")]
    [ProducesResponseType(typeof(Result<SqlOptimizeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<SqlOptimizeResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SqlOptimize([FromBody] SqlOptimizeRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.SqlQuery))
        {
            return BadRequest(Result<SqlOptimizeResponseDto>.Fail(
                ErrorCode.ValidationFailed,
                "SqlQuery is required"));
        }

        var optimizeRequest = new SqlOptimizeRequest
        {
            SqlQuery = request.SqlQuery,
            DatabaseType = request.DatabaseType,
            TargetGoal = request.TargetGoal
        };

        var result = await _sqlOptimizeService.OptimizeAsync(optimizeRequest, cancellationToken);

        var dto = new SqlOptimizeResponseDto
        {
            OptimizedSql = result.OptimizedSql,
            Explanation = result.Explanation,
            EstimatedImprovement = result.EstimatedImprovement,
            Suggestions = result.Suggestions,
            IndexRecommendations = result.IndexRecommendations,
            WasModified = result.WasModified
        };

        return Ok(Result<SqlOptimizeResponseDto>.Ok(dto));
    }

    /// <summary>
    /// Get all AI sessions
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(Result<IEnumerable<AiSessionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions([FromQuery] long? connectionId, CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetSessionsAsync(connectionId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Ok(Result<IEnumerable<AiSessionListItemDto>>.Fail(
                result.ErrorCode,
                result.ErrorMessage ?? "Failed to get sessions"));
        }

        var dtos = result.Data?.Select(s => new AiSessionListItemDto
        {
            Id = s.Id,
            Title = s.Title,
            ConnectionId = s.ConnectionId,
            DatabaseType = s.DatabaseType,
            LastUserMessage = s.LastUserMessage,
            MessageCount = s.MessageCount,
            LastActivityAt = s.LastActivityAt,
            CreatedAt = s.CreatedAt
        }) ?? new List<AiSessionListItemDto>();

        return Ok(Result<IEnumerable<AiSessionListItemDto>>.Ok(dtos));
    }

    /// <summary>
    /// Create a new AI session
    /// </summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(Result<AiSessionListItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<AiSessionListItemDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Title))
        {
            return BadRequest(Result<AiSessionListItemDto>.Fail(
                ErrorCode.ValidationFailed,
                "Title is required"));
        }

        var createRequest = new CreateSessionRequest
        {
            Title = request.Title,
            ConnectionId = request.ConnectionId,
            DatabaseType = request.DatabaseType
        };

        var result = await _sessionService.CreateSessionAsync(createRequest, cancellationToken);

        if (!result.IsSuccess || result.Data == null)
        {
            return BadRequest(Result<AiSessionListItemDto>.Fail(
                result.ErrorCode,
                result.ErrorMessage ?? "Failed to create session"));
        }

        var dto = new AiSessionListItemDto
        {
            Id = result.Data.Id,
            Title = result.Data.Title,
            ConnectionId = result.Data.ConnectionId,
            DatabaseType = result.Data.DatabaseType,
            LastUserMessage = result.Data.LastUserMessage,
            MessageCount = result.Data.MessageCount,
            LastActivityAt = result.Data.LastActivityAt,
            CreatedAt = result.Data.CreatedAt
        };

        return CreatedAtAction(nameof(GetSessionMessages), new { id = result.Data.Id }, Result<AiSessionListItemDto>.Ok(dto));
    }

    /// <summary>
    /// Get messages for a session
    /// </summary>
    [HttpGet("sessions/{id}/messages")]
    [ProducesResponseType(typeof(Result<IEnumerable<AiMessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IEnumerable<AiMessageDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionMessages(long id, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.GetMessagesAsync(id, limit, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(Result<IEnumerable<AiMessageDto>>.Fail(
                result.ErrorCode,
                result.ErrorMessage ?? "Session not found"));
        }

        var dtos = result.Data?.Select(m => new AiMessageDto
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            SqlGenerated = m.SqlGenerated,
            ErrorMessage = m.ErrorMessage,
            TokenUsage = m.TokenUsage,
            ExecutionTimeMs = m.ExecutionTimeMs,
            CreatedAt = m.CreatedAt
        }) ?? new List<AiMessageDto>();

        return Ok(Result<IEnumerable<AiMessageDto>>.Ok(dtos));
    }

    private static string GetEncryptionKey() => "DongQiDB32ByteEncryptionKey123456";
    private static string GetEncryptionIv() => "DongQiDB16Byte";

    private string DecryptPassword(string encryptedPassword)
    {
        try
        {
            return _connectionManager.DecryptPassword(encryptedPassword, GetEncryptionKey(), GetEncryptionIv());
        }
        catch
        {
            return string.Empty;
        }
    }
}
