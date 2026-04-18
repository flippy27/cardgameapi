using System.Collections.Concurrent;

namespace CardDuel.ServerApi.Infrastructure;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly ConcurrentDictionary<string, (int count, DateTime resetAt)> _requestCounts;
    private readonly int _maxRequests;
    private readonly int _windowSeconds;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _requestCounts = new();
        _maxRequests = 100;
        _windowSeconds = 60;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (IsRateLimited(clientIp))
        {
            _logger.LogWarning("Rate limit exceeded for IP {ClientIp}", clientIp);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded" });
            return;
        }

        await _next(context);
    }

    private bool IsRateLimited(string clientIp)
    {
        var now = DateTime.UtcNow;
        var entry = _requestCounts.AddOrUpdate(clientIp,
            (1, now.AddSeconds(_windowSeconds)),
            (key, current) =>
            {
                if (now >= current.resetAt)
                {
                    return (1, now.AddSeconds(_windowSeconds));
                }
                return (current.count + 1, current.resetAt);
            });

        if (entry.count > _maxRequests)
        {
            return true;
        }

        if (_requestCounts.Count > 10000)
        {
            var expired = _requestCounts.Where(x => now >= x.Value.resetAt).ToList();
            foreach (var item in expired)
            {
                _requestCounts.TryRemove(item.Key, out _);
            }
        }

        return false;
    }
}
