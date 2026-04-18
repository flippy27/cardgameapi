using System.Text.Json;

namespace CardDuel.ServerApi.Infrastructure;

public sealed class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception occurred");
            await HandleExceptionAsync(context, exception);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, isDevelopment ? exception.Message : "Invalid operation"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ArgumentException => (StatusCodes.Status400BadRequest, isDevelopment ? exception.Message : "Invalid argument"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new ApiErrorResponse(
            code: statusCode,
            message: message,
            timestamp: DateTimeOffset.UtcNow,
            correlationId: correlationId,
            details: isDevelopment ? exception.StackTrace : null
        ));
    }
}

public sealed record ApiErrorResponse(
    int code,
    string message,
    DateTimeOffset timestamp,
    string correlationId,
    string? details = null);
