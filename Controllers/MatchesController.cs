using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;
using CardDuel.ServerApi.Game;
using CardDuel.ServerApi.Hubs;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/matches")]
public sealed class MatchesController(IMatchService matchService, AppDbContext dbContext, IHubContext<MatchHub> matchHub) : ControllerBase
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
    public Task<ActionResult<MatchSnapshot>> SetReady(string matchId, SetReadyRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return ExecuteMatchActionAsync(matchId, () => matchService.SetReady(matchId, request.PlayerId, request.IsReady));
    }

    [HttpPost("{matchId}/play")]
    public Task<ActionResult<MatchSnapshot>> PlayCard(string matchId, PlayCardRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return ExecuteMatchActionAsync(matchId, () => matchService.PlayCard(matchId, request.PlayerId, request.RuntimeHandKey, request.SlotIndex, request.TargetRuntimeId));
    }

    [HttpPost("{matchId}/end-turn")]
    public Task<ActionResult<MatchSnapshot>> EndTurn(string matchId, EndTurnRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return ExecuteMatchActionAsync(matchId, () => matchService.EndTurn(matchId, request.PlayerId));
    }

    [HttpPost("{matchId}/destroy-card")]
    public Task<ActionResult<MatchSnapshot>> DestroyCard(string matchId, DestroyCardRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return ExecuteMatchActionAsync(matchId, () => matchService.DestroyCard(matchId, request.PlayerId, request.RuntimeCardId));
    }

    [HttpPost("{matchId}/forfeit")]
    public Task<ActionResult<MatchSnapshot>> Forfeit(string matchId, ForfeitRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return ExecuteMatchActionAsync(matchId, () => matchService.Forfeit(matchId, request.PlayerId));
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

    // Runs a state-changing match action, then pushes the fresh per-seat snapshots to every connected
    // SignalR client (same dispatch the hub uses). Without this, actions taken over HTTP — e.g. the
    // single-player AI seat, or a client on the HTTP fallback — never reach the opponent's live view.
    private async Task<ActionResult<MatchSnapshot>> ExecuteMatchActionAsync(string matchId, Func<MatchSnapshot> action)
    {
        MatchSnapshot snapshot;
        try
        {
            snapshot = action();
        }
        catch (GameActionException exception)
        {
            return BadRequest(new GameActionErrorDto(exception.Code, exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        await BroadcastMatchAsync(matchId);
        return Ok(snapshot);
    }

    private async Task BroadcastMatchAsync(string matchId)
    {
        try
        {
            foreach (var dispatch in matchService.BuildDispatches(matchId))
            {
                await matchHub.Clients.Client(dispatch.ConnectionId).SendAsync("MatchSnapshot", dispatch.Snapshot);
            }
        }
        catch
        {
            // Broadcasting is best-effort; the action already succeeded and is returned to the caller.
        }
    }
}
