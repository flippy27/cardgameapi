using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var dbHealthy = await CheckDatabaseHealth();

        return Ok(new
        {
            ok = dbHealthy,
            service = "cardduel-server-api",
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            utc = DateTimeOffset.UtcNow,
            database = dbHealthy ? "healthy" : "unhealthy"
        });
    }

    private async Task<bool> CheckDatabaseHealth()
    {
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
