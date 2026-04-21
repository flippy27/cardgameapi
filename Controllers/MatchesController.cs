using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;
using CardDuel.ServerApi.Game;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/matches")]
public sealed class MatchesController(IMatchService matchService, AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(matchService.ListMatches());

    [HttpGet("{matchId}/summary")]
    public ActionResult<MatchSummaryDto> GetSummary(string matchId) => Ok(matchService.GetSummary(matchId));

    [HttpGet("{matchId}/rules/{playerId}")]
    public async Task<ActionResult<GameRulesDto>> GetRules(string matchId, string playerId, CancellationToken cancellationToken)
    {
        EnsurePlayer(playerId);

        var match = await dbContext.Matches
            .AsNoTracking()
            .FirstOrDefaultAsync(model => model.MatchId == matchId && (model.Player1Id == playerId || model.Player2Id == playerId), cancellationToken);

        if (match == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(match.GameRulesSnapshotJson))
        {
            return NotFound(new { message = "No rules snapshot was recorded for this match." });
        }

        var snapshot = JsonSerializer.Deserialize<GameRulesDto>(match.GameRulesSnapshotJson);
        return snapshot == null
            ? Problem(title: "Rules snapshot could not be read for this match.")
            : Ok(snapshot);
    }

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
        return ExecuteMatchAction(() => matchService.SetReady(matchId, request.PlayerId, request.IsReady));
    }

    [HttpPost("{matchId}/play")]
    public ActionResult<MatchSnapshot> PlayCard(string matchId, PlayCardRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return ExecuteMatchAction(() => matchService.PlayCard(matchId, request.PlayerId, request.RuntimeHandKey, request.SlotIndex));
    }

    [HttpPost("{matchId}/end-turn")]
    public ActionResult<MatchSnapshot> EndTurn(string matchId, EndTurnRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return ExecuteMatchAction(() => matchService.EndTurn(matchId, request.PlayerId));
    }

    [HttpPost("{matchId}/forfeit")]
    public ActionResult<MatchSnapshot> Forfeit(string matchId, ForfeitRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return ExecuteMatchAction(() => matchService.Forfeit(matchId, request.PlayerId));
    }

    [HttpPost("{matchId}/complete")]
    public ActionResult<MatchCompletionResponse> Complete(string matchId, MatchCompletionRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return Ok(matchService.CompleteMatch(matchId, request.PlayerId, request.OpponentId,
            request.PlayerWon, request.DurationSeconds));
    }

    [HttpPost("{matchId}/actions")]
    public ActionResult<PostActionsResponse> PostActions(string matchId, PostActionsRequest request)
    {
        if (request.Actions == null || request.Actions.Count == 0)
            return Ok(new PostActionsResponse(matchId, 0, true, "No actions to process"));

        EnsurePlayer(request.Actions[0].PlayerId);
        return Ok(matchService.ProcessActions(matchId, request));
    }

    private void EnsurePlayer(string playerId)
    {
        var authenticated = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.Equals(authenticated, playerId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Authenticated player mismatch.");
        }
    }

    private ActionResult<MatchSnapshot> ExecuteMatchAction(Func<MatchSnapshot> action)
    {
        try
        {
            return Ok(action());
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
