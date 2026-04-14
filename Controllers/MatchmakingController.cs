using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/matchmaking")]
public sealed class MatchmakingController(IMatchService matchService) : ControllerBase
{
    [HttpPost("private")]
    public ActionResult<MatchReservationDto> CreatePrivate(CreatePrivateMatchRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return Ok(matchService.CreatePrivate(request.PlayerId, request.DeckId, request.MatchName));
    }

    [HttpPost("private/join")]
    public ActionResult<MatchReservationDto> JoinPrivate(JoinPrivateMatchRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return Ok(matchService.JoinPrivate(request.PlayerId, request.DeckId, request.RoomCode));
    }

    [HttpPost("queue")]
    public ActionResult<MatchReservationDto> Queue(QueueForMatchRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return Ok(matchService.Queue(request.PlayerId, request.DeckId, request.Mode, request.Rating));
    }

    private void EnsurePlayer(string playerId)
    {
        var authenticated = User.FindFirst("sub")?.Value;
        if (!string.Equals(authenticated, playerId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Authenticated player mismatch.");
        }
    }
}
