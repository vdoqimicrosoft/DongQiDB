using System.Net;
using System.Text.Json;
using DongQiDB.Domain.Common;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace DongQiDB.Infrastructure.Exceptions;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        Log.Error(exception, "Unhandled exception occurred");

        var (statusCode, errorCode, message) = exception switch
        {
            ValidationException ve => (HttpStatusCode.BadRequest, ErrorCode.ValidationFailed, ve.Message),
            BusinessException be => (HttpStatusCode.BadRequest, be.ErrorCode, be.Message),
            _ => (HttpStatusCode.InternalServerError, ErrorCode.InternalError, "Internal server error")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new { code = (int)errorCode, message };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
