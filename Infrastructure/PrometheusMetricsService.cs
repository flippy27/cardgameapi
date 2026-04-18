namespace CardDuel.ServerApi.Infrastructure;

public sealed class PrometheusMetricsService
{
    private static long _totalRequests;
    private static long _totalErrors;
    private static long _totalLatencyMs;
    private static readonly object Lock = new();

    public static void RecordRequest(long latencyMs, bool isError)
    {
        lock (Lock)
        {
            _totalRequests++;
            _totalLatencyMs += latencyMs;
            if (isError) _totalErrors++;
        }
    }

    public static string ExportMetrics()
    {
        lock (Lock)
        {
            var avgLatency = _totalRequests > 0 ? (double)_totalLatencyMs / _totalRequests : 0;
            var errorRate = _totalRequests > 0 ? (double)_totalErrors / _totalRequests * 100 : 0;

            return $@"# HELP cardduel_requests_total Total number of HTTP requests
# TYPE cardduel_requests_total counter
cardduel_requests_total {_totalRequests}

# HELP cardduel_errors_total Total number of HTTP errors
# TYPE cardduel_errors_total counter
cardduel_errors_total {_totalErrors}

# HELP cardduel_request_duration_avg_ms Average request duration in milliseconds
# TYPE cardduel_request_duration_avg_ms gauge
cardduel_request_duration_avg_ms {avgLatency:F2}

# HELP cardduel_error_rate_percent Error rate percentage
# TYPE cardduel_error_rate_percent gauge
cardduel_error_rate_percent {errorRate:F2}
";
        }
    }

    public static void Reset()
    {
        lock (Lock)
        {
            _totalRequests = 0;
            _totalErrors = 0;
            _totalLatencyMs = 0;
        }
    }
}
