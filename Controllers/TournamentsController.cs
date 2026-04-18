using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tournaments")]
public sealed class TournamentsController(InMemoryTournamentStore tournamentStore) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(tournamentStore.List());

    [HttpPost]
    public IActionResult Create([FromQuery] string displayName, [FromQuery] DateTimeOffset startsAtUtc, [FromQuery] int maxPlayers = 64)
    {
        return Ok(tournamentStore.Create(displayName, startsAtUtc, maxPlayers));
    }

    [HttpPost("{tournamentId}/register")]
    public IActionResult Register(string tournamentId)
    {
        var playerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("Missing player ID.");
        return Ok(tournamentStore.Register(tournamentId, playerId));
    }
}
