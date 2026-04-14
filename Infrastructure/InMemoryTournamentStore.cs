using System.Collections.Concurrent;

namespace CardDuel.ServerApi.Infrastructure;

public sealed record TournamentDto(string TournamentId, string DisplayName, DateTimeOffset StartsAtUtc, int MaxPlayers, IReadOnlyList<string> RegisteredPlayerIds);

public sealed class InMemoryTournamentStore
{
    private readonly ConcurrentDictionary<string, TournamentDto> _tournaments = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<TournamentDto> List() => _tournaments.Values.OrderBy(x => x.StartsAtUtc).ToArray();

    public TournamentDto Create(string displayName, DateTimeOffset startsAtUtc, int maxPlayers)
    {
        var id = Guid.NewGuid().ToString("N");
        var dto = new TournamentDto(id, displayName, startsAtUtc, maxPlayers, Array.Empty<string>());
        _tournaments[id] = dto;
        return dto;
    }

    public TournamentDto Register(string tournamentId, string playerId)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var dto))
        {
            throw new InvalidOperationException("Tournament not found.");
        }

        if (dto.RegisteredPlayerIds.Contains(playerId, StringComparer.Ordinal))
        {
            return dto;
        }

        if (dto.RegisteredPlayerIds.Count >= dto.MaxPlayers)
        {
            throw new InvalidOperationException("Tournament is full.");
        }

        var updated = dto with { RegisteredPlayerIds = dto.RegisteredPlayerIds.Append(playerId).ToArray() };
        _tournaments[tournamentId] = updated;
        return updated;
    }
}
