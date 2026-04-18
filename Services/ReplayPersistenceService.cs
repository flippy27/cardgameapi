using System.Text.Json;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Services;

public interface IReplayPersistenceService
{
    Task LogActionAsync(string matchId, int actionNumber, string playerId, string actionType, object actionData);
}

public sealed class ReplayPersistenceService(AppDbContext dbContext) : IReplayPersistenceService
{
    public async Task LogActionAsync(string matchId, int actionNumber, string playerId, string actionType, object actionData)
    {
        var action = new MatchAction
        {
            Id = Guid.NewGuid().ToString(),
            MatchId = matchId,
            ActionNumber = actionNumber,
            PlayerId = playerId,
            ActionType = actionType,
            ActionData = JsonSerializer.Serialize(actionData),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.MatchActions.Add(action);
        await dbContext.SaveChangesAsync();
    }
}
