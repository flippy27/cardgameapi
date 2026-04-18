using System.Text;

namespace CardDuel.ServerApi.Infrastructure;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var requestBody = await ReadRequestBody(request);
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation(
            "HTTP Request: {Method} {Path} | IP: {ClientIp} | User: {User}",
            request.Method,
            request.Path,
            clientIp,
            context.User?.Identity?.Name ?? "anonymous");

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var startTime = DateTime.UtcNow;
        await _next(context);
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

        _logger.LogInformation(
            "HTTP Response: {Method} {Path} | Status: {StatusCode} | Duration: {DurationMs}ms",
            request.Method,
            request.Path,
            context.Response.StatusCode,
            duration);

        responseBody.Position = 0;
        context.Response.ContentLength = responseBody.Length;
        await responseBody.CopyToAsync(originalBodyStream);
    }

    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        request.EnableBuffering();
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        request.Body.Position = 0;
        return body.Length > 500 ? body[..497] + "..." : body;
    }
}
