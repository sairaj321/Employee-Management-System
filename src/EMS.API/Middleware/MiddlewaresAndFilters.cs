using System.Diagnostics;
using System.Text.Json;
using EMS.Application.DTOs;
using EMS.Application.Exceptions;

namespace EMS.API.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status400BadRequest, ex.Message, ex.Errors);
        }
        catch (NotFoundException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (ForbiddenException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N");
            _logger.LogError(ex, "UNHANDLED_EXCEPTION. CorrelationId: {CorrelationId}", correlationId);
            await WriteResponseAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.", traceId: correlationId);
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, string message, object? errors = null, string? traceId = null)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = ApiResponse<object>.FailureResult(message, errors, traceId);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var path = context.Request.Path.Value;
        var method = context.Request.Method;

        await _next(context);

        sw.Stop();
        var statusCode = context.Response.StatusCode;
        var correlationId = context.Items["CorrelationId"]?.ToString();

        _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms [Trace: {CorrelationId}]",
            method, path, statusCode, sw.ElapsedMilliseconds, correlationId);
    }
}
