using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Route("api/v1/admin/metrics")]
[Authorize]
public sealed class MetricsController : ControllerBase
{
    [HttpGet]
    public ActionResult<object> GetMetrics()
    {
        return Ok(new
        {
            Timestamp = DateTimeOffset.UtcNow,
            Metrics = MetricsMiddleware.GetMetricsSummary()
        });
    }

    [HttpPost("reset")]
    public ActionResult ResetMetrics()
    {
        MetricsMiddleware.ResetMetrics();
        return Ok(new { Message = "Metrics reset" });
    }
}
