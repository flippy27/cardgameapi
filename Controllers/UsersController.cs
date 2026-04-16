using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Contracts;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("{playerId}/profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(string playerId)
    {
        EnsurePlayer(playerId);

        var user = await dbContext.Users
            .Include(u => u.Rating)
            .FirstOrDefaultAsync(u => u.Id == playerId);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserProfileDto(
            userId: user.Id,
            username: user.Username,
            email: user.Email,
            rating: user.Rating?.RatingValue ?? 1000,
            wins: user.Rating?.Wins ?? 0,
            losses: user.Rating?.Losses ?? 0,
            createdAt: user.CreatedAt,
            lastLoginAt: user.LastLoginAt
        ));
    }

    [HttpGet("{playerId}/stats")]
    public async Task<ActionResult<UserStatsDto>> GetStats(string playerId)
    {
        EnsurePlayer(playerId);

        var user = await dbContext.Users
            .Include(u => u.Rating)
            .FirstOrDefaultAsync(u => u.Id == playerId);

        if (user == null)
        {
            return NotFound();
        }

        var totalGames = (user.Rating?.Wins ?? 0) + (user.Rating?.Losses ?? 0);
        var winRate = totalGames == 0 ? 0 : (double)(user.Rating?.Wins ?? 0) / totalGames;

        return Ok(new UserStatsDto(
            userId: user.Id,
            username: user.Username,
            rating: user.Rating?.RatingValue ?? 1000,
            wins: user.Rating?.Wins ?? 0,
            losses: user.Rating?.Losses ?? 0,
            totalGames: totalGames,
            winRate: winRate,
            region: user.Rating?.Region ?? "global"
        ));
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<LeaderboardDto>> GetLeaderboard(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string region = "global")
    {
        const int maxPageSize = 1000;
        if (pageSize > maxPageSize)
        {
            pageSize = maxPageSize;
        }

        var query = dbContext.Ratings
            .Where(r => r.Region == region)
            .OrderByDescending(r => r.RatingValue)
            .AsAsyncEnumerable();

        var totalCount = await dbContext.Ratings
            .Where(r => r.Region == region)
            .CountAsync();

        var entries = await dbContext.Ratings
            .Where(r => r.Region == region)
            .OrderByDescending(r => r.RatingValue)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.User)
            .ToListAsync();

        var leaderboardEntries = entries
            .Select((r, index) =>
            {
                var totalGames = r.Wins + r.Losses;
                var winRate = totalGames == 0 ? 0.0 : (double)r.Wins / totalGames;
                return new LeaderboardEntryDto(
                    rank: ((page - 1) * pageSize) + index + 1,
                    username: r.User?.Username ?? "Unknown",
                    rating: r.RatingValue,
                    wins: r.Wins,
                    losses: r.Losses,
                    winRate: winRate
                );
            })
            .ToList();

        return Ok(new LeaderboardDto(
            page: page,
            pageSize: pageSize,
            totalCount: totalCount,
            entries: leaderboardEntries
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

public sealed record UserProfileDto(
    string userId,
    string username,
    string email,
    int rating,
    int wins,
    int losses,
    DateTimeOffset createdAt,
    DateTimeOffset? lastLoginAt);

public sealed record UserStatsDto(
    string userId,
    string username,
    int rating,
    int wins,
    int losses,
    int totalGames,
    double winRate,
    string region);

public sealed record LeaderboardDto(
    int page,
    int pageSize,
    int totalCount,
    IReadOnlyList<LeaderboardEntryDto> entries);

public sealed record LeaderboardEntryDto(
    int rank,
    string username,
    int rating,
    int wins,
    int losses,
    double winRate);
