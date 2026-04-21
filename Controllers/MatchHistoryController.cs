using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Contracts;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/matches")]
public sealed class MatchHistoryController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("history/{playerId}")]
    public async Task<ActionResult<MatchHistoryPageDto>> GetHistory(
        string playerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        EnsurePlayer(playerId);

        const int maxPageSize = 100;
        if (pageSize > maxPageSize)
        {
            pageSize = maxPageSize;
        }

        var totalCount = await dbContext.Matches
            .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
            .CountAsync();

        var matches = await dbContext.Matches
            .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var entries = matches.Select(m => new MatchHistoryEntryDto(
            matchId: m.MatchId,
            opponentId: m.Player1Id == playerId ? m.Player2Id : m.Player1Id,
            result: (m.WinnerId == playerId) ? "win" : (m.WinnerId == null ? "draw" : "loss"),
            mode: m.Mode.ToString(),
            rulesetId: m.GameRulesetId,
            rulesetName: m.GameRulesetName,
            durationSeconds: m.DurationSeconds,
            ratingBefore: m.Player1Id == playerId ? m.Player1RatingBefore : m.Player2RatingBefore,
            ratingAfter: m.Player1Id == playerId ? m.Player1RatingAfter : m.Player2RatingAfter,
            createdAt: m.CreatedAt
        )).ToList();

        return Ok(new MatchHistoryPageDto(
            page: page,
            pageSize: pageSize,
            totalCount: totalCount,
            matches: entries
        ));
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

public sealed record MatchHistoryPageDto(
    int page,
    int pageSize,
    int totalCount,
    IReadOnlyList<MatchHistoryEntryDto> matches);

public sealed record MatchHistoryEntryDto(
    string matchId,
    string opponentId,
    string result,
    string mode,
    string rulesetId,
    string rulesetName,
    int durationSeconds,
    int? ratingBefore,
    int? ratingAfter,
    DateTimeOffset createdAt);
