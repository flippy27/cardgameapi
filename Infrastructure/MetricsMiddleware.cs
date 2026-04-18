using System.Diagnostics;

namespace CardDuel.ServerApi.Infrastructure;

public sealed class MetricsMiddleware(RequestDelegate next, ILogger<MetricsMiddleware> logger)
{
    private static readonly Dictionary<string, (long Count, long TotalMs)> Metrics = new();
    private static readonly object Lock = new();

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;

        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            var key = $"{method} {path}";
            var statusCode = context.Response.StatusCode;
            var isError = statusCode >= 400;

            PrometheusMetricsService.RecordRequest(sw.ElapsedMilliseconds, isError);

            lock (Lock)
            {
                if (Metrics.TryGetValue(key, out var metric))
                {
                    Metrics[key] = (metric.Count + 1, metric.TotalMs + sw.ElapsedMilliseconds);
                }
                else
                {
                    Metrics[key] = (1, sw.ElapsedMilliseconds);
                }
            }

            if (sw.ElapsedMilliseconds > 500)
            {
                logger.LogWarning("Slow request: {Method} {Path} took {Elapsed}ms (status {StatusCode})",
                    method, path, sw.ElapsedMilliseconds, statusCode);
            }
        }
    }

    public static Dictionary<string, object> GetMetricsSummary()
    {
        lock (Lock)
        {
            return Metrics.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)new
                {
                    Count = kvp.Value.Count,
                    AvgMs = kvp.Value.Count > 0 ? (double)kvp.Value.TotalMs / kvp.Value.Count : 0,
                    TotalMs = kvp.Value.TotalMs
                });
        }
    }

    public static void ResetMetrics()
    {
        lock (Lock)
        {
            Metrics.Clear();
        }
    }
}
