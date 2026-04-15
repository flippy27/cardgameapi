using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;
using CardDuel.ServerApi.Game;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/matches")]
public sealed class MatchesController(IMatchService matchService) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(matchService.ListMatches());

    [HttpGet("{matchId}/summary")]
    public ActionResult<MatchSummaryDto> GetSummary(string matchId) => Ok(matchService.GetSummary(matchId));

    [HttpGet("{matchId}/snapshot/{playerId}")]
    public ActionResult<MatchSnapshot> GetSnapshot(string matchId, string playerId)
    {
        EnsurePlayer(playerId);
        return Ok(matchService.GetSnapshot(matchId, playerId));
    }

    [HttpPost("{matchId}/ready")]
    public ActionResult<MatchSnapshot> SetReady(string matchId, SetReadyRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return Ok(matchService.SetReady(matchId, request.PlayerId, request.IsReady));
    }

    [HttpPost("{matchId}/play")]
    public ActionResult<MatchSnapshot> PlayCard(string matchId, PlayCardRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return Ok(matchService.PlayCard(matchId, request.PlayerId, request.RuntimeHandKey, request.SlotIndex));
    }

    [HttpPost("{matchId}/end-turn")]
    public ActionResult<MatchSnapshot> EndTurn(string matchId, EndTurnRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return Ok(matchService.EndTurn(matchId, request.PlayerId));
    }

    [HttpPost("{matchId}/forfeit")]
    public ActionResult<MatchSnapshot> Forfeit(string matchId, ForfeitRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return Ok(matchService.Forfeit(matchId, request.PlayerId));
    }

    [HttpPost("{matchId}/complete")]
    public ActionResult<MatchCompletionResponse> Complete(string matchId, MatchCompletionRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return Ok(matchService.CompleteMatch(matchId, request.PlayerId, request.OpponentId,
            request.PlayerWon, request.DurationSeconds));
    }

    private void EnsurePlayer(string playerId)
    {
        var authenticated = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.Equals(authenticated, playerId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Authenticated player mismatch.");
        }
    }
}
