using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Services;

public interface IReplayService
{
    Task LogActionAsync(string matchId, string playerId, string actionType, object actionData);
    Task<IReadOnlyList<ReplayLog>> GetReplayAsync(string matchId);
}

public sealed class ReplayService(AppDbContext dbContext) : IReplayService
{
    public async Task LogActionAsync(string matchId, string playerId, string actionType, object actionData)
    {
        var actionNumber = await dbContext.ReplayLogs
            .Where(r => r.MatchId == matchId)
            .CountAsync();

        var log = new ReplayLog
        {
            MatchId = matchId,
            PlayerId = playerId,
            ActionType = actionType,
            ActionNumber = actionNumber + 1,
            ActionData = JsonSerializer.Serialize(actionData)
        };

        dbContext.ReplayLogs.Add(log);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ReplayLog>> GetReplayAsync(string matchId)
    {
        return await dbContext.ReplayLogs
            .Where(r => r.MatchId == matchId)
            .OrderBy(r => r.ActionNumber)
            .ToListAsync();
    }
}
