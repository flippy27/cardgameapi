using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Services;

public interface IReconnectionService
{
    Task<(bool canReconnect, string reason)> ValidateReconnectionAsync(string matchId, string playerId, string reconnectToken, int graceSeconds);
    Task MarkPlayerOfflineAsync(string matchId, string playerId);
    Task MarkPlayerOnlineAsync(string matchId, string playerId);
    string GenerateReconnectToken();
}

public sealed class ReconnectionService(AppDbContext dbContext, ILogger<ReconnectionService> logger) : IReconnectionService
{
    public async Task<(bool canReconnect, string reason)> ValidateReconnectionAsync(
        string matchId, string playerId, string reconnectToken, int graceSeconds)
    {
        var match = await dbContext.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId);
        if (match == null)
        {
            logger.LogWarning("Reconnection attempt on non-existent match {MatchId}", matchId);
            return (false, "Match not found");
        }

        if (match.CompletedAt.HasValue)
        {
            logger.LogWarning("Reconnection attempt on completed match {MatchId}", matchId);
            return (false, "Match already completed");
        }

        var isPlayer1 = match.Player1Id == playerId;
        var isPlayer2 = match.Player2Id == playerId;

        if (!isPlayer1 && !isPlayer2)
        {
            logger.LogWarning("Reconnection attempt by unauthorized player {PlayerId} on match {MatchId}", playerId, matchId);
            return (false, "Player not in this match");
        }

        var disconnectTime = isPlayer1 ? match.Player1DisconnectedAt : match.Player2DisconnectedAt;
        var token = isPlayer1 ? match.Player1ReconnectToken : match.Player2ReconnectToken;

        if (!disconnectTime.HasValue)
        {
            logger.LogWarning("Reconnection attempt but player not marked offline {PlayerId} on match {MatchId}", playerId, matchId);
            return (false, "Player was not disconnected");
        }

        var timeSinceDisconnect = (DateTimeOffset.UtcNow - disconnectTime.Value).TotalSeconds;
        if (timeSinceDisconnect > graceSeconds)
        {
            logger.LogWarning("Reconnection grace period expired for {PlayerId} on match {MatchId}. Disconnected {Seconds}s ago",
                playerId, matchId, timeSinceDisconnect);
            return (false, "Grace period expired");
        }

        if (token != reconnectToken)
        {
            logger.LogWarning("Invalid reconnect token for {PlayerId} on match {MatchId}", playerId, matchId);
            return (false, "Invalid reconnect token");
        }

        logger.LogInformation("Reconnection allowed for {PlayerId} on match {MatchId}", playerId, matchId);
        return (true, "Reconnection allowed");
    }

    public async Task MarkPlayerOfflineAsync(string matchId, string playerId)
    {
        var match = await dbContext.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId);
        if (match == null) return;

        var token = GenerateReconnectToken();
        var now = DateTimeOffset.UtcNow;

        if (match.Player1Id == playerId)
        {
            match.Player1DisconnectedAt = now;
            match.Player1ReconnectToken = token;
        }
        else if (match.Player2Id == playerId)
        {
            match.Player2DisconnectedAt = now;
            match.Player2ReconnectToken = token;
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Player marked offline {PlayerId} on match {MatchId}", playerId, matchId);
    }

    public async Task MarkPlayerOnlineAsync(string matchId, string playerId)
    {
        var match = await dbContext.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId);
        if (match == null) return;

        if (match.Player1Id == playerId)
        {
            match.Player1DisconnectedAt = null;
            match.Player1ReconnectToken = null;
        }
        else if (match.Player2Id == playerId)
        {
            match.Player2DisconnectedAt = null;
            match.Player2ReconnectToken = null;
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Player marked online {PlayerId} on match {MatchId}", playerId, matchId);
    }

    public string GenerateReconnectToken()
    {
        var buffer = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }
        return Convert.ToBase64String(buffer);
    }
}
