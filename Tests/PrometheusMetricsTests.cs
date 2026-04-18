using Xunit;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Tests;

public class PrometheusMetricsTests
{
    [Fact]
    public void ExportMetrics_EmptyMetrics_ReturnsValidPrometheus()
    {
        PrometheusMetricsService.Reset();

        var metrics = PrometheusMetricsService.ExportMetrics();

        Assert.Contains("cardduel_requests_total 0", metrics);
        Assert.Contains("cardduel_errors_total 0", metrics);
        Assert.Contains("TYPE cardduel_request_duration_avg_ms gauge", metrics);
    }

    [Fact]
    public void RecordRequest_TracksMetrics()
    {
        PrometheusMetricsService.Reset();

        PrometheusMetricsService.RecordRequest(100, false);
        PrometheusMetricsService.RecordRequest(200, true);

        var metrics = PrometheusMetricsService.ExportMetrics();

        Assert.Contains("cardduel_requests_total 2", metrics);
        Assert.Contains("cardduel_errors_total 1", metrics);
        Assert.Contains("cardduel_error_rate_percent 50", metrics);
    }
}
