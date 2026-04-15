using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Game;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Services;

public interface ICardCatalogService
{
    IReadOnlyDictionary<string, ServerCardDefinition> GetAll();
    IReadOnlyList<ServerCardDefinition> ResolveDeck(IEnumerable<string> cardIds);
}

public interface IDeckRepository
{
    void Upsert(string playerId, string deckId, string displayName, IReadOnlyList<string> cardIds);
    PlayerDeck GetDeck(string playerId, string deckId);
    IReadOnlyList<PlayerDeck> GetDecks(string playerId);
}

public interface IMatchService
{
    MatchReservationDto CreatePrivate(string playerId, string deckId, string? name);
    MatchReservationDto JoinPrivate(string playerId, string deckId, string roomCode);
    MatchReservationDto Queue(string playerId, string deckId, QueueMode mode, int rating);
    bool TryReconnect(string matchId, string playerId, string reconnectToken, out int seatIndex);
    MatchSnapshot Connect(string matchId, string playerId, string reconnectToken, string connectionId);
    MatchSnapshot SetReady(string matchId, string playerId, bool ready);
    MatchSnapshot PlayCard(string matchId, string playerId, string runtimeHandKey, int slotIndex);
    MatchSnapshot EndTurn(string matchId, string playerId);
    MatchSnapshot Forfeit(string matchId, string playerId);
    MatchCompletionResponse CompleteMatch(string matchId, string playerId, string opponentId, bool playerWon, int durationSeconds);
    MatchSnapshot MarkDisconnected(string matchId, string playerId);
    MatchSnapshot GetSnapshot(string matchId, string playerId, bool spectator = false);
    MatchSummaryDto GetSummary(string matchId);
    IReadOnlyList<MatchSummaryDto> ListMatches();
    IReadOnlyList<DispatchEnvelope> BuildDispatches(string matchId);
    void AddSpectator(string matchId, string spectatorId, string connectionId);
}

public sealed record PlayerDeck(string PlayerId, string DeckId, string DisplayName, IReadOnlyList<string> CardIds);

public sealed record DispatchEnvelope(string ConnectionId, MatchSnapshot Snapshot, bool Spectator);

public sealed class InMemoryCardCatalogService : ICardCatalogService
{
    private readonly Dictionary<string, ServerCardDefinition> _catalog;

    public InMemoryCardCatalogService()
    {
        _catalog = BuildCatalog();
    }

    public IReadOnlyDictionary<string, ServerCardDefinition> GetAll() => _catalog;

    public IReadOnlyList<ServerCardDefinition> ResolveDeck(IEnumerable<string> cardIds)
    {
        return cardIds.Select(cardId => _catalog.TryGetValue(cardId, out var card)
                ? card
                : throw new InvalidOperationException($"Unknown card id '{cardId}'."))
            .ToArray();
    }

    private static Dictionary<string, ServerCardDefinition> BuildCatalog()
    {
        ServerAbilityDefinition ability(string id, TriggerKind trigger, TargetSelectorKind selector, params ServerEffectDefinition[] effects) =>
            new(id, trigger, selector, effects);

        return new[]
        {
            new ServerCardDefinition("ember_vanguard", "Ember Vanguard", 2, 3, 3, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("ember_archer", "Ember Archer", 2, 2, 2, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("ember_burnseer", "Burnseer", 3, 2, 3, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst,
                new[] { ability("hero_ping", TriggerKind.OnTurnEnd, TargetSelectorKind.Self, new ServerEffectDefinition(EffectKind.HitHero, 2)) }),
            new ServerCardDefinition("tidal_priest", "Tidal Priest", 2, 1, 3, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst,
                new[] { ability("battle_heal", TriggerKind.OnTurnEnd, TargetSelectorKind.LowestHealthAlly, new ServerEffectDefinition(EffectKind.Heal, 2)) }),
            new ServerCardDefinition("tidal_lancer", "Tidal Lancer", 2, 3, 2, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("tidal_sniper", "Tidal Sniper", 3, 3, 2, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("grove_guardian", "Grove Guardian", 3, 2, 5, 1, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("grove_shaper", "Grove Shaper", 3, 1, 4, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst,
                new[] { ability("battle_buff", TriggerKind.OnTurnStart, TargetSelectorKind.Self, new ServerEffectDefinition(EffectKind.BuffAttack, 1)) }),
            new ServerCardDefinition("grove_raincaller", "Raincaller", 2, 1, 3, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst,
                new[] { ability("ally_heal", TriggerKind.OnTurnEnd, TargetSelectorKind.LowestHealthAlly, new ServerEffectDefinition(EffectKind.Heal, 2)) }),
            new ServerCardDefinition("alloy_bulwark", "Alloy Bulwark", 3, 2, 4, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst,
                new[] { ability("armor_on_play", TriggerKind.OnPlay, TargetSelectorKind.Self, new ServerEffectDefinition(EffectKind.GainArmor, 2)) }),
            new ServerCardDefinition("alloy_ballista", "Alloy Ballista", 4, 4, 2, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("alloy_hoplite", "Alloy Hoplite", 2, 2, 3, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("void_stalker", "Void Stalker", 2, 3, 2, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("void_caller", "Void Caller", 4, 2, 3, 0, AllowedRow.BackOnly, TargetSelectorKind.FrontlineFirst,
                new[] { ability("splash", TriggerKind.OnBattlePhase, TargetSelectorKind.AllEnemies, new ServerEffectDefinition(EffectKind.Damage, 1)) }),
            new ServerCardDefinition("void_magus", "Void Magus", 4, 3, 4, 0, AllowedRow.BackOnly, TargetSelectorKind.FrontlineFirst,
                new[] { ability("self_buff", TriggerKind.OnTurnStart, TargetSelectorKind.Self, new ServerEffectDefinition(EffectKind.BuffAttack, 1)) }),
            new ServerCardDefinition("ember_colossus", "Ember Colossus", 5, 5, 6, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("tidal_waveblade", "Waveblade", 1, 2, 1, 0, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("grove_myr", "Grove Myr", 1, 1, 2, 0, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, Array.Empty<ServerAbilityDefinition>())
        }.ToDictionary(card => card.CardId, card => card, StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class InMemoryDeckRepository(ICardCatalogService catalogService) : IDeckRepository
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PlayerDeck>> _decks = new(StringComparer.Ordinal);

    public void Upsert(string playerId, string deckId, string displayName, IReadOnlyList<string> cardIds)
    {
        _ = catalogService.ResolveDeck(cardIds);
        var bucket = _decks.GetOrAdd(playerId, _ => new ConcurrentDictionary<string, PlayerDeck>(StringComparer.OrdinalIgnoreCase));
        bucket[deckId] = new PlayerDeck(playerId, deckId, displayName, cardIds);
    }

    public PlayerDeck GetDeck(string playerId, string deckId)
    {
        if (_decks.TryGetValue(playerId, out var playerDecks) && playerDecks.TryGetValue(deckId, out var deck))
        {
            return deck;
        }

        throw new InvalidOperationException("Deck not found.");
    }

    public IReadOnlyList<PlayerDeck> GetDecks(string playerId)
    {
        if (_decks.TryGetValue(playerId, out var playerDecks))
        {
            return playerDecks.Values.OrderBy(x => x.DisplayName).ToArray();
        }

        return Array.Empty<PlayerDeck>();
    }
}

public sealed class InMemoryMatchService : IMatchService, IDisposable
{
    private readonly IDeckRepository _deckRepository;
    private readonly ICardCatalogService _catalogService;
    private readonly AppDbContext _dbContext;
    private readonly ConcurrentDictionary<string, MatchRoom> _matches = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _roomCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();
    private WaitingTicket? _casualWaiting;
    private readonly List<WaitingTicket> _rankedWaiting = new();
    private readonly Timer _timer;
    private readonly TimeSpan _disconnectGrace;

    public InMemoryMatchService(IDeckRepository deckRepository, ICardCatalogService catalogService, IConfiguration configuration, AppDbContext dbContext)
    {
        _deckRepository = deckRepository;
        _catalogService = catalogService;
        _dbContext = dbContext;
        _disconnectGrace = TimeSpan.FromSeconds(configuration.GetValue<int?>("Game:DisconnectGraceSeconds") ?? 20);
        _timer = new Timer(_ => Tick(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public MatchReservationDto CreatePrivate(string playerId, string deckId, string? name)
    {
        lock (_sync)
        {
            var room = CreateRoom(QueueMode.Private, name);
            var deck = _deckRepository.GetDeck(playerId, deckId);
            var reservation = room.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(deck.CardIds));
            return reservation;
        }
    }

    public MatchReservationDto JoinPrivate(string playerId, string deckId, string roomCode)
    {
        lock (_sync)
        {
            if (!_roomCodes.TryGetValue(roomCode, out var matchId) || !_matches.TryGetValue(matchId, out var room))
            {
                throw new InvalidOperationException("Room code not found.");
            }

            var deck = _deckRepository.GetDeck(playerId, deckId);
            return room.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(deck.CardIds));
        }
    }

    public MatchReservationDto Queue(string playerId, string deckId, QueueMode mode, int rating)
    {
        lock (_sync)
        {
            if (mode == QueueMode.Casual)
            {
                if (_casualWaiting == null)
                {
                    var room = CreateRoom(QueueMode.Casual, null);
                    var hostDeck = _deckRepository.GetDeck(playerId, deckId);
                    var reservation = room.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(hostDeck.CardIds));
                    _casualWaiting = new WaitingTicket(playerId, deckId, rating, room.MatchId);
                    return reservation with { WaitingForOpponent = true, Status = "queued" };
                }

                if (!_matches.TryGetValue(_casualWaiting.MatchId, out var waitingRoom))
                {
                    _casualWaiting = null;
                    return Queue(playerId, deckId, mode, rating);
                }

                var joinDeck = _deckRepository.GetDeck(playerId, deckId);
                var joined = waitingRoom.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(joinDeck.CardIds));
                _casualWaiting = null;
                return joined with { WaitingForOpponent = false, Status = "matched" };
            }

            var candidate = _rankedWaiting.OrderBy(ticket => Math.Abs(ticket.Rating - rating)).FirstOrDefault();
            if (candidate == null)
            {
                var room = CreateRoom(QueueMode.Ranked, null);
                var hostDeck = _deckRepository.GetDeck(playerId, deckId);
                var reservation = room.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(hostDeck.CardIds));
                _rankedWaiting.Add(new WaitingTicket(playerId, deckId, rating, room.MatchId));
                return reservation with { WaitingForOpponent = true, Status = "queued" };
            }

            _rankedWaiting.Remove(candidate);
            var rankedRoom = _matches[candidate.MatchId];
            var otherDeck = _deckRepository.GetDeck(playerId, deckId);
            var rankedJoined = rankedRoom.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(otherDeck.CardIds));
            return rankedJoined with { WaitingForOpponent = false, Status = "matched" };
        }
    }

    public bool TryReconnect(string matchId, string playerId, string reconnectToken, out int seatIndex)
    {
        if (_matches.TryGetValue(matchId, out var room))
        {
            return room.Engine.TryReconnect(playerId, reconnectToken, out seatIndex);
        }

        seatIndex = -1;
        return false;
    }

    public MatchSnapshot Connect(string matchId, string playerId, string reconnectToken, string connectionId)
    {
        var room = GetRoom(matchId);
        if (!room.Engine.TryReconnect(playerId, reconnectToken, out var seatIndex))
        {
            throw new InvalidOperationException("Reconnect token invalid.");
        }

        room.PlayerConnections[playerId] = connectionId;
        return room.Engine.CreateSnapshotForSeat(seatIndex);
    }

    public MatchSnapshot SetReady(string matchId, string playerId, bool ready)
    {
        var room = GetRoom(matchId);
        room.Engine.SetReady(playerId, ready);
        var seatIndex = room.Engine.Seats.First(x => x.PlayerId == playerId).SeatIndex;
        return room.Engine.CreateSnapshotForSeat(seatIndex);
    }

    public MatchSnapshot PlayCard(string matchId, string playerId, string runtimeHandKey, int slotIndex)
    {
        var room = GetRoom(matchId);
        room.Engine.PlayCard(playerId, runtimeHandKey, (BoardSlot)slotIndex);
        var seatIndex = room.Engine.Seats.First(x => x.PlayerId == playerId).SeatIndex;
        return room.Engine.CreateSnapshotForSeat(seatIndex);
    }

    public MatchSnapshot EndTurn(string matchId, string playerId)
    {
        var room = GetRoom(matchId);
        room.Engine.EndTurn(playerId);
        var seatIndex = room.Engine.Seats.First(x => x.PlayerId == playerId).SeatIndex;
        return room.Engine.CreateSnapshotForSeat(seatIndex);
    }

    public MatchSnapshot Forfeit(string matchId, string playerId)
    {
        var room = GetRoom(matchId);
        room.Engine.Forfeit(playerId);
        var seatIndex = room.Engine.Seats.First(x => x.PlayerId == playerId).SeatIndex;
        return room.Engine.CreateSnapshotForSeat(seatIndex);
    }

    public MatchCompletionResponse CompleteMatch(string matchId, string playerId, string opponentId, bool playerWon, int durationSeconds)
    {
        try
        {
            var match = _dbContext.Matches.FirstOrDefault(m => m.MatchId == matchId);
            if (match == null)
                throw new InvalidOperationException($"Match {matchId} not found");

            if (match.CompletedAt.HasValue)
                return new MatchCompletionResponse(matchId, false, "Match already completed");

            var isPlayer1 = match.Player1Id == playerId;
            var isPlayer2 = match.Player2Id == playerId;

            if (!isPlayer1 && !isPlayer2)
                throw new InvalidOperationException("Player not in this match");

            // Verify opponent ID matches
            var expectedOpponent = isPlayer1 ? match.Player2Id : match.Player1Id;
            if (expectedOpponent != opponentId)
                throw new InvalidOperationException("Opponent ID mismatch");

            // Get current ratings (default 1000 if not set)
            var ratingService = new EloRatingService();
            var player1Rating = match.Player1RatingBefore ?? 1000;
            var player2Rating = match.Player2RatingBefore ?? 1000;

            // Determine winner
            var player1Won = isPlayer1 ? playerWon : !playerWon;
            var (newRating1, newRating2) = ratingService.CalculateEloChange(player1Rating, player2Rating, player1Won);

            // Update match record
            match.WinnerId = playerWon ? playerId : opponentId;
            match.DurationSeconds = durationSeconds;
            match.CompletedAt = DateTimeOffset.UtcNow;
            match.Player1RatingBefore = player1Rating;
            match.Player2RatingBefore = player2Rating;
            match.Player1RatingAfter = newRating1;
            match.Player2RatingAfter = newRating2;

            _dbContext.Matches.Update(match);
            _dbContext.SaveChanges();

            return new MatchCompletionResponse(
                matchId,
                true,
                $"Match completed. Winner: {(playerWon ? playerId : opponentId)}, Rating change: {(playerWon ? "+" : "")}{newRating1 - player1Rating}");
        }
        catch (Exception ex)
        {
            return new MatchCompletionResponse(matchId, false, $"Error: {ex.Message}");
        }
    }

    public MatchSnapshot MarkDisconnected(string matchId, string playerId)
    {
        var room = GetRoom(matchId);
        room.Engine.MarkDisconnected(playerId);
        var seatIndex = room.Engine.Seats.First(x => x.PlayerId == playerId).SeatIndex;
        return room.Engine.CreateSnapshotForSeat(seatIndex);
    }

    public MatchSnapshot GetSnapshot(string matchId, string playerId, bool spectator = false)
    {
        var room = GetRoom(matchId);
        if (spectator)
        {
            return room.Engine.CreateSnapshotForSeat(-1, redactHandsForSpectator: true);
        }

        var seatIndex = room.Engine.Seats.First(x => x.PlayerId == playerId).SeatIndex;
        return room.Engine.CreateSnapshotForSeat(seatIndex);
    }

    public MatchSummaryDto GetSummary(string matchId) => GetRoom(matchId).Engine.ToSummary();

    public IReadOnlyList<MatchSummaryDto> ListMatches() => _matches.Values.Select(x => x.Engine.ToSummary()).OrderByDescending(x => x.MatchId).ToArray();

    public IReadOnlyList<DispatchEnvelope> BuildDispatches(string matchId)
    {
        var room = GetRoom(matchId);
        var dispatches = new List<DispatchEnvelope>();

        foreach (var seat in room.Engine.Seats.Where(x => !string.IsNullOrWhiteSpace(x.PlayerId)))
        {
            if (room.PlayerConnections.TryGetValue(seat.PlayerId, out var connectionId))
            {
                dispatches.Add(new DispatchEnvelope(connectionId, room.Engine.CreateSnapshotForSeat(seat.SeatIndex), Spectator: false));
            }
        }

        foreach (var spectator in room.Engine.Spectators)
        {
            dispatches.Add(new DispatchEnvelope(spectator.Value, room.Engine.CreateSnapshotForSeat(-1, redactHandsForSpectator: true), Spectator: true));
        }

        return dispatches;
    }

    public void AddSpectator(string matchId, string spectatorId, string connectionId)
    {
        var room = GetRoom(matchId);
        room.Engine.AddSpectator(spectatorId, connectionId);
    }

    private MatchRoom CreateRoom(QueueMode mode, string? name)
    {
        var matchId = Guid.NewGuid().ToString("N");
        var code = CreateRoomCode();
        var room = new MatchRoom(matchId, name ?? $"{mode}-match", new MatchEngine(matchId, code, mode, _disconnectGrace));
        _matches[matchId] = room;
        _roomCodes[code] = matchId;
        return room;
    }

    private MatchRoom GetRoom(string matchId)
    {
        return _matches.TryGetValue(matchId, out var room)
            ? room
            : throw new InvalidOperationException("Match not found.");
    }

    private static string CreateRoomCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[6];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[Random.Shared.Next(0, alphabet.Length)];
        }

        return new string(chars);
    }

    private void Tick()
    {
        foreach (var room in _matches.Values)
        {
            room.Engine.Tick();
        }
    }

    public void Dispose() => _timer.Dispose();

    private sealed record WaitingTicket(string PlayerId, string DeckId, int Rating, string MatchId);

    private sealed class MatchRoom
    {
        public MatchRoom(string matchId, string name, MatchEngine engine)
        {
            MatchId = matchId;
            Name = name;
            Engine = engine;
        }

        public string MatchId { get; }
        public string Name { get; }
        public MatchEngine Engine { get; }
        public ConcurrentDictionary<string, string> PlayerConnections { get; } = new(StringComparer.Ordinal);
    }
}
