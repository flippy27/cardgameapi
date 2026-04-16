namespace CardDuel.ServerApi.Services;

public interface ISpectatorService
{
    Task AddSpectatorAsync(string matchId, string spectatorId);
    Task RemoveSpectatorAsync(string matchId, string spectatorId);
    Task<IReadOnlyList<string>> GetSpectatorsAsync(string matchId);
}

public sealed class SpectatorService : ISpectatorService
{
    private readonly Dictionary<string, HashSet<string>> _spectators = new();
    private readonly object _lock = new();

    public Task AddSpectatorAsync(string matchId, string spectatorId)
    {
        lock (_lock)
        {
            if (!_spectators.ContainsKey(matchId))
            {
                _spectators[matchId] = new HashSet<string>();
            }
            _spectators[matchId].Add(spectatorId);
        }
        return Task.CompletedTask;
    }

    public Task RemoveSpectatorAsync(string matchId, string spectatorId)
    {
        lock (_lock)
        {
            if (_spectators.TryGetValue(matchId, out var spectators))
            {
                spectators.Remove(spectatorId);
            }
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetSpectatorsAsync(string matchId)
    {
        lock (_lock)
        {
            if (_spectators.TryGetValue(matchId, out var spectators))
            {
                return Task.FromResult<IReadOnlyList<string>>(spectators.ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }
}
