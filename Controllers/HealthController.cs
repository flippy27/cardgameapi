using Microsoft.AspNetCore.Mvc;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        ok = true,
        service = "cardduel-server-api",
        utc = DateTimeOffset.UtcNow
    });
}
