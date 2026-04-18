using System.Collections.Concurrent;

namespace CardDuel.ServerApi.Infrastructure;

public sealed class MatchActionCounter
{
    private static readonly ConcurrentDictionary<string, int> Counters = new();

    public static int IncrementAndGet(string matchId)
    {
        return Counters.AddOrUpdate(matchId, 1, (_, current) => current + 1);
    }

    public static void Reset(string matchId)
    {
        Counters.TryRemove(matchId, out _);
    }

    public static int GetCurrent(string matchId)
    {
        return Counters.TryGetValue(matchId, out var count) ? count : 0;
    }
}
