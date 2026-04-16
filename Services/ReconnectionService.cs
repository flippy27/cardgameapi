using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Services;

public interface IReconnectionService
{
    Task<(bool canReconnect, string reason)> ValidateReconnectionAsync(string matchId, string playerId, string reconnectToken, int graceSeconds);
    Task MarkPlayerOfflineAsync(string matchId, string playerId);
    Task MarkPlayerOnlineAsync(string matchId, string playerId);
}

public sealed class ReconnectionService(AppDbContext dbContext) : IReconnectionService
{
    public async Task<(bool canReconnect, string reason)> ValidateReconnectionAsync(
        string matchId, string playerId, string reconnectToken, int graceSeconds)
    {
        var match = await dbContext.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId);
        if (match == null)
        {
            return (false, "Match not found");
        }

        if (match.CompletedAt.HasValue)
        {
            return (false, "Match already completed");
        }

        var isPlayer1 = match.Player1Id == playerId;
        var isPlayer2 = match.Player2Id == playerId;

        if (!isPlayer1 && !isPlayer2)
        {
            return (false, "Player not in this match");
        }

        // Check grace period (would need to track disconnect time separately)
        // For now, just validate basic conditions
        return (true, "Reconnection allowed");
    }

    public async Task MarkPlayerOfflineAsync(string matchId, string playerId)
    {
        var match = await dbContext.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId);
        if (match != null)
        {
            // Log disconnection (would update a status field)
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task MarkPlayerOnlineAsync(string matchId, string playerId)
    {
        var match = await dbContext.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId);
        if (match != null)
        {
            // Update connection status
            await dbContext.SaveChangesAsync();
        }
    }
}
