using Xunit;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Tests;

public class PrometheusIntegrationTests
{
    [Fact]
    public void ExportMetrics_ValidPrometheusFormat()
    {
        PrometheusMetricsService.Reset();

        var metrics = PrometheusMetricsService.ExportMetrics();

        // Verify format includes HELP and TYPE comments
        Assert.Contains("# HELP cardduel_requests_total", metrics);
        Assert.Contains("# TYPE cardduel_requests_total counter", metrics);
        Assert.Contains("# HELP cardduel_errors_total", metrics);
        Assert.Contains("# TYPE cardduel_errors_total counter", metrics);
        Assert.Contains("# HELP cardduel_request_duration_avg_ms", metrics);
        Assert.Contains("# TYPE cardduel_request_duration_avg_ms gauge", metrics);
        Assert.Contains("# HELP cardduel_error_rate_percent", metrics);
        Assert.Contains("# TYPE cardduel_error_rate_percent gauge", metrics);

        // Verify metric lines exist
        Assert.Contains("cardduel_requests_total 0", metrics);
        Assert.Contains("cardduel_errors_total 0", metrics);
        Assert.Contains("cardduel_request_duration_avg_ms 0.00", metrics);
        Assert.Contains("cardduel_error_rate_percent 0.00", metrics);
    }

    [Fact]
    public void RecordRequest_UpdatesMetricsCorrectly()
    {
        PrometheusMetricsService.Reset();

        // Simulate 100 requests with varying latencies
        for (int i = 0; i < 80; i++)
        {
            PrometheusMetricsService.RecordRequest(50, false);
        }
        for (int i = 0; i < 20; i++)
        {
            PrometheusMetricsService.RecordRequest(100, true);
        }

        var metrics = PrometheusMetricsService.ExportMetrics();

        // Total requests = 100
        Assert.Contains("cardduel_requests_total 100", metrics);

        // Total errors = 20
        Assert.Contains("cardduel_errors_total 20", metrics);

        // Error rate = 20%
        Assert.Contains("cardduel_error_rate_percent 20.00", metrics);

        // Avg latency = (80*50 + 20*100) / 100 = 60ms
        Assert.Contains("cardduel_request_duration_avg_ms 60.00", metrics);
    }

    [Fact]
    public void RepeatedRecording_AggregatesCorrectly()
    {
        PrometheusMetricsService.Reset();

        // First batch
        PrometheusMetricsService.RecordRequest(100, false);
        PrometheusMetricsService.RecordRequest(200, false);

        var metrics1 = PrometheusMetricsService.ExportMetrics();
        var lines1 = metrics1.Split('\n');
        var totalReqs1 = lines1.FirstOrDefault(l => l.StartsWith("cardduel_requests_total "));
        Assert.NotNull(totalReqs1);
        Assert.True(totalReqs1.Contains("2"), $"Expected '2' in {totalReqs1}");

        // Second batch
        PrometheusMetricsService.RecordRequest(150, true);

        var metrics2 = PrometheusMetricsService.ExportMetrics();
        var lines2 = metrics2.Split('\n');
        var totalReqs2 = lines2.FirstOrDefault(l => l.StartsWith("cardduel_requests_total "));
        var totalErrs = lines2.FirstOrDefault(l => l.StartsWith("cardduel_errors_total "));
        Assert.NotNull(totalReqs2);
        Assert.NotNull(totalErrs);
        Assert.True(totalReqs2.Contains("3"), $"Expected '3' in {totalReqs2}");
        Assert.True(totalErrs.Contains("1"), $"Expected '1' in {totalErrs}");
    }
}
