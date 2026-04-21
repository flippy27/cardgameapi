using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/matchmaking")]
public sealed class MatchmakingController(
    IMatchService matchService,
    IGameRulesetService gameRulesetService,
    ILogger<MatchmakingController> logger) : ControllerBase
{
    [HttpPost("private")]
    public async Task<ActionResult<MatchReservationDto>> CreatePrivate(CreatePrivateMatchRequest request, CancellationToken cancellationToken)
    {
        EnsurePlayer(request.PlayerId);

        try
        {
            var resolvedRules = await gameRulesetService.ResolveAsync(request.RulesetId, cancellationToken);
            return Ok(matchService.CreatePrivate(request.PlayerId, request.DeckId, request.MatchName, resolvedRules));
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Private match creation rejected for {PlayerId}", request.PlayerId);
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("private/join")]
    public ActionResult<MatchReservationDto> JoinPrivate(JoinPrivateMatchRequest request)
    {
        EnsurePlayer(request.PlayerId);
        return Ok(matchService.JoinPrivate(request.PlayerId, request.DeckId, request.RoomCode));
    }

    [HttpPost("queue")]
    public async Task<ActionResult<MatchReservationDto>> Queue([FromBody] QueueForMatchRequest request, CancellationToken cancellationToken)
    {
        EnsurePlayer(request.PlayerId);

        if (request.Mode == QueueMode.Private)
        {
            return BadRequest(new { message = "Queue mode must be Casual or Ranked." });
        }

        try
        {
            var resolvedRules = await gameRulesetService.ResolveAsync(request.RulesetId, cancellationToken);
            return Ok(matchService.Queue(request.PlayerId, request.DeckId, request.Mode, request.Rating, resolvedRules));
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception,
                "Queue request rejected for {PlayerId} with deck {DeckId} in mode {Mode}",
                request.PlayerId,
                request.DeckId,
                request.Mode);
            return BadRequest(new { message = exception.Message });
        }
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
