using System.Security.Claims;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Infrastructure;

public sealed class AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        var completed = false;

        try
        {
            await next(context);
            completed = true;

            var statusCode = context.Response.StatusCode;
            if (ShouldAudit(method, path))
            {
                var userId = GetUserIdentifier(context);
                var resource = ExtractResource(path);
                var resourceId = ExtractResourceId(path);
                var action = $"{method} {path}";

                _ = auditService.LogAsync(userId, action, resource, resourceId, statusCode: statusCode, ipAddress: ip);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in audit logging");
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;

            if (completed)
            {
                responseBody.Position = 0;
                context.Response.ContentLength = responseBody.Length;
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }

    private static bool ShouldAudit(string method, string path)
    {
        var unsafeMethod = method is "POST" or "PUT" or "DELETE";
        var criticalPath = path.Contains("matches") || path.Contains("decks") || path.Contains("auth/register");
        return unsafeMethod && criticalPath;
    }

    private static string ExtractResource(string path)
    {
        return path.Split('/')[^2] switch
        {
            "matches" => "Match",
            "decks" => "Deck",
            "auth" => "Auth",
            _ => "Unknown"
        };
    }

    private static string ExtractResourceId(string path)
    {
        var parts = path.Split('/');
        return parts.Length > 3 ? parts[^1] : string.Empty;
    }

    private static string GetUserIdentifier(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return "anonymous";
        }

        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.Identity?.Name
            ?? "authenticated";
    }
}
