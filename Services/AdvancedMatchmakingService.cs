using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Contracts;

namespace CardDuel.ServerApi.Services;

public interface IAdvancedMatchmakingService
{
    Task<(string? player2Id, int? ratingDelta)> FindRankedOpponentAsync(string playerId, int playerRating, int ratingBracket = 100);
    Task<IReadOnlyList<(string playerId, int rating)>> GetQueueAsync(QueueMode mode);
}

public sealed class AdvancedMatchmakingService(AppDbContext dbContext, ILogger<AdvancedMatchmakingService> logger) : IAdvancedMatchmakingService
{
    private const int DefaultBracket = 100;
    private const int MaxBracketExpand = 400;

    public async Task<(string? player2Id, int? ratingDelta)> FindRankedOpponentAsync(
        string playerId, int playerRating, int ratingBracket = 100)
    {
        var bracket = ratingBracket;

        while (bracket <= MaxBracketExpand)
        {
            var opponent = await dbContext.Ratings
                .Where(r => r.RatingValue >= playerRating - bracket &&
                           r.RatingValue <= playerRating + bracket &&
                           r.UserId != playerId)
                .OrderBy(r => Math.Abs(r.RatingValue - playerRating))
                .FirstOrDefaultAsync();

            if (opponent != null)
            {
                logger.LogInformation(
                    "Matchmaking found opponent: {PlayerId}({Rating}) vs {OpponentId}({OpponentRating})",
                    playerId, playerRating, opponent.UserId, opponent.RatingValue);

                return (opponent.UserId, Math.Abs(opponent.RatingValue - playerRating));
            }

            bracket += 100; // Expand bracket
            await Task.Delay(500); // Brief delay before expanding
        }

        logger.LogWarning("No opponent found for {PlayerId} after bracket expansion", playerId);
        return (null, null);
    }

    public async Task<IReadOnlyList<(string playerId, int rating)>> GetQueueAsync(QueueMode mode)
    {
        var queue = await dbContext.Ratings
            .OrderByDescending(r => r.RatingValue)
            .Take(100)
            .Select(r => new { r.UserId, r.RatingValue })
            .ToListAsync();

        return queue.Select(q => (q.UserId, q.RatingValue)).ToList();
    }
}
