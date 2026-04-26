using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Game;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;

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
    PlayerDeckDetails GetDeckDetails(string playerId, string deckId);
    IReadOnlyList<PlayerDeck> GetDecks(string playerId);
    PlayerDeckDetails AddCard(string playerId, string deckId, string cardId, int? position = null);
    PlayerDeckDetails RemoveCard(string playerId, string deckId, string entryId);
}

public interface IMatchService
{
    MatchReservationDto CreatePrivate(string playerId, string deckId, string? name, ResolvedGameRules resolvedRules);
    MatchReservationDto JoinPrivate(string playerId, string deckId, string roomCode);
    MatchReservationDto Queue(string playerId, string deckId, QueueMode mode, int rating, ResolvedGameRules resolvedRules);
    bool TryReconnect(string matchId, string playerId, string reconnectToken, out int seatIndex);
    MatchSnapshot Connect(string matchId, string playerId, string reconnectToken, string connectionId);
    MatchSnapshot SetReady(string matchId, string playerId, bool ready);
    MatchSnapshot PlayCard(string matchId, string playerId, string runtimeHandKey, int slotIndex);
    MatchSnapshot EndTurn(string matchId, string playerId);
    MatchSnapshot DestroyCard(string matchId, string playerId, string runtimeCardId);
    MatchSnapshot Forfeit(string matchId, string playerId);
    MatchCompletionResponse CompleteMatch(string matchId, string playerId, string opponentId, bool playerWon, int durationSeconds);
    PostActionsResponse ProcessActions(string matchId, PostActionsRequest request);
    MatchSnapshot MarkDisconnected(string matchId, string playerId);
    MatchSnapshot GetSnapshot(string matchId, string playerId, bool spectator = false);
    MatchSummaryDto GetSummary(string matchId);
    IReadOnlyList<MatchSummaryDto> ListMatches();
    IReadOnlyList<DispatchEnvelope> BuildDispatches(string matchId);
    void AddSpectator(string matchId, string spectatorId, string connectionId);
}

public sealed record PlayerDeck(string PlayerId, string DeckId, string DisplayName, IReadOnlyList<string> CardIds);
public sealed record PlayerDeckCard(string EntryId, string CardId, int Position);
public sealed record PlayerDeckDetails(string PlayerId, string DeckId, string DisplayName, IReadOnlyList<PlayerDeckCard> Cards)
{
    public IReadOnlyList<string> CardIds => Cards.OrderBy(card => card.Position).Select(card => card.CardId).ToArray();
}

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
        ServerAbilityDefinition ability(string id, string displayName, TriggerKind trigger, TargetSelectorKind selector, params ServerEffectDefinition[] effects) =>
            new(id, displayName, trigger, selector, effects);

        return new[]
        {
            new ServerCardDefinition("ember_vanguard", "Ember Vanguard", "", 2, 3, 3, 0, 0, 0, 0, (int)UnitType.Melee, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, 1, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("ember_archer", "Ember Archer", "", 2, 2, 2, 0, 0, 0, 0, (int)UnitType.Ranged, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, 1, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("ember_burnseer", "Burnseer", "", 3, 2, 3, 0, 0, 0, 0, (int)UnitType.Magic, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, 1,
                new[] { ability("hero_ping", "Hero Ping", TriggerKind.OnTurnEnd, TargetSelectorKind.Self, new ServerEffectDefinition(EffectKind.HitHero, 2)) }),
            new ServerCardDefinition("tidal_priest", "Tidal Priest", "", 2, 1, 3, 0, 0, 0, 0, (int)UnitType.Magic, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, 1,
                new[] { ability("battle_heal", "Battle Heal", TriggerKind.OnTurnEnd, TargetSelectorKind.LowestHealthAlly, new ServerEffectDefinition(EffectKind.Heal, 2)) }),
            new ServerCardDefinition("tidal_lancer", "Tidal Lancer", "", 2, 3, 2, 0, 0, 0, 0, (int)UnitType.Melee, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, 1, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("tidal_sniper", "Tidal Sniper", "", 3, 3, 2, 0, 0, 0, 0, (int)UnitType.Ranged, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, 1, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("grove_guardian", "Grove Guardian", "", 3, 2, 5, 1, 0, 0, 0, (int)UnitType.Melee, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, 1, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("grove_shaper", "Grove Shaper", "", 3, 1, 4, 0, 0, 0, 0, (int)UnitType.Magic, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, 1,
                new[] { ability("battle_buff", "Battle Buff", TriggerKind.OnTurnStart, TargetSelectorKind.Self, new ServerEffectDefinition(EffectKind.BuffAttack, 1)) }),
            new ServerCardDefinition("grove_raincaller", "Raincaller", "", 2, 1, 3, 0, 0, 0, 0, (int)UnitType.Magic, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, 1,
                new[] { ability("ally_heal", "Ally Heal", TriggerKind.OnTurnEnd, TargetSelectorKind.LowestHealthAlly, new ServerEffectDefinition(EffectKind.Heal, 2)) }),
            new ServerCardDefinition("alloy_bulwark", "Alloy Bulwark", "", 3, 2, 4, 0, 0, 0, 0, (int)UnitType.Melee, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, 1,
                new[] { ability("armor_on_play", "Armor On Play", TriggerKind.OnPlay, TargetSelectorKind.Self, new ServerEffectDefinition(EffectKind.GainArmor, 2)) }),
            new ServerCardDefinition("alloy_ballista", "Alloy Ballista", "", 4, 4, 2, 0, 0, 0, 0, (int)UnitType.Ranged, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, 1, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("alloy_hoplite", "Alloy Hoplite", "", 2, 2, 3, 0, 0, 0, 0, (int)UnitType.Melee, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, 1, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("void_stalker", "Void Stalker", "", 2, 3, 2, 0, 0, 0, 0, (int)UnitType.Melee, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, 1, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("void_caller", "Void Caller", "", 4, 2, 3, 0, 0, 0, 0, (int)UnitType.Magic, AllowedRow.BackOnly, TargetSelectorKind.FrontlineFirst, 1,
                new[] { ability("splash", "Splash", TriggerKind.OnBattlePhase, TargetSelectorKind.AllEnemies, new ServerEffectDefinition(EffectKind.Damage, 1)) }),
            new ServerCardDefinition("void_magus", "Void Magus", "", 4, 3, 4, 0, 0, 0, 0, (int)UnitType.Magic, AllowedRow.BackOnly, TargetSelectorKind.FrontlineFirst, 1,
                new[] { ability("self_buff", "Self Buff", TriggerKind.OnTurnStart, TargetSelectorKind.Self, new ServerEffectDefinition(EffectKind.BuffAttack, 1)) }),
            new ServerCardDefinition("ember_colossus", "Ember Colossus", "", 5, 5, 6, 0, 0, 0, 0, (int)UnitType.Melee, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, 1, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("tidal_waveblade", "Waveblade", "", 1, 2, 1, 0, 0, 0, 0, (int)UnitType.Ranged, AllowedRow.BackOnly, TargetSelectorKind.BacklineFirst, 1, Array.Empty<ServerAbilityDefinition>()),
            new ServerCardDefinition("grove_myr", "Grove Myr", "", 1, 1, 2, 0, 0, 0, 0, (int)UnitType.Melee, AllowedRow.FrontOnly, TargetSelectorKind.FrontlineFirst, 1, Array.Empty<ServerAbilityDefinition>())
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

    public PlayerDeckDetails GetDeckDetails(string playerId, string deckId)
    {
        var deck = GetDeck(playerId, deckId);
        return new PlayerDeckDetails(
            deck.PlayerId,
            deck.DeckId,
            deck.DisplayName,
            deck.CardIds.Select((cardId, index) => new PlayerDeckCard($"{deck.DeckId}-{index}", cardId, index)).ToArray());
    }

    public IReadOnlyList<PlayerDeck> GetDecks(string playerId)
    {
        if (_decks.TryGetValue(playerId, out var playerDecks))
        {
            return playerDecks.Values.OrderBy(x => x.DisplayName).ToArray();
        }

        return Array.Empty<PlayerDeck>();
    }

    public PlayerDeckDetails AddCard(string playerId, string deckId, string cardId, int? position = null)
    {
        var deck = GetDeck(playerId, deckId);
        _ = catalogService.ResolveDeck(new[] { cardId });
        var cards = deck.CardIds.ToList();
        var insertAt = Math.Clamp(position ?? cards.Count, 0, cards.Count);
        cards.Insert(insertAt, cardId);
        Upsert(playerId, deckId, deck.DisplayName, cards);
        return GetDeckDetails(playerId, deckId);
    }

    public PlayerDeckDetails RemoveCard(string playerId, string deckId, string entryId)
    {
        var deck = GetDeck(playerId, deckId);
        var cards = deck.CardIds.ToList();
        var suffix = entryId.Split('-').LastOrDefault();
        if (!int.TryParse(suffix, out var index) || index < 0 || index >= cards.Count)
        {
            throw new InvalidOperationException("Deck card entry not found.");
        }

        cards.RemoveAt(index);
        Upsert(playerId, deckId, deck.DisplayName, cards);
        return GetDeckDetails(playerId, deckId);
    }
}

public sealed class InMemoryMatchService : IMatchService, IDisposable
{
    private readonly IDeckRepository _deckRepository;
    private readonly ICardCatalogService _catalogService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, MatchRoom> _matches = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _roomCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();
    private readonly List<WaitingTicket> _casualWaiting = new();
    private readonly List<WaitingTicket> _rankedWaiting = new();
    private readonly Timer _timer;
    private readonly TimeSpan _disconnectGrace;

    public InMemoryMatchService(IDeckRepository deckRepository, ICardCatalogService catalogService, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _deckRepository = deckRepository;
        _catalogService = catalogService;
        _serviceProvider = serviceProvider;
        _disconnectGrace = TimeSpan.FromSeconds(configuration.GetValue<int?>("Game:DisconnectGraceSeconds") ?? 20);
        _timer = new Timer(_ => Tick(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public MatchReservationDto CreatePrivate(string playerId, string deckId, string? name, ResolvedGameRules resolvedRules)
    {
        lock (_sync)
        {
            var room = CreateRoom(QueueMode.Private, name, resolvedRules);
            var deck = _deckRepository.GetDeck(playerId, deckId);
            var reservation = room.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(deck.CardIds));
            SyncMatchRecord(room);
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
            var reservation = room.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(deck.CardIds));
            SyncMatchRecord(room);
            return reservation;
        }
    }

    public MatchReservationDto Queue(string playerId, string deckId, QueueMode mode, int rating, ResolvedGameRules resolvedRules)
    {
        lock (_sync)
        {
            if (mode == QueueMode.Casual)
            {
                var candidate = _casualWaiting.FirstOrDefault(ticket => string.Equals(ticket.RulesetId, resolvedRules.RulesetId, StringComparison.Ordinal));
                if (candidate == null)
                {
                    var room = CreateRoom(QueueMode.Casual, null, resolvedRules);
                    var hostDeck = _deckRepository.GetDeck(playerId, deckId);
                    var reservation = room.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(hostDeck.CardIds));
                    _casualWaiting.Add(new WaitingTicket(playerId, deckId, rating, room.MatchId, resolvedRules.RulesetId));
                    SyncMatchRecord(room);
                    return reservation with { WaitingForOpponent = true, Status = "queued" };
                }

                if (!_matches.TryGetValue(candidate.MatchId, out var waitingRoom))
                {
                    _casualWaiting.Remove(candidate);
                    return Queue(playerId, deckId, mode, rating, resolvedRules);
                }

                var joinDeck = _deckRepository.GetDeck(playerId, deckId);
                var joined = waitingRoom.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(joinDeck.CardIds));
                _casualWaiting.Remove(candidate);
                SyncMatchRecord(waitingRoom);
                return joined with { WaitingForOpponent = false, Status = "matched" };
            }

            var rankedCandidate = _rankedWaiting
                .Where(ticket => string.Equals(ticket.RulesetId, resolvedRules.RulesetId, StringComparison.Ordinal))
                .OrderBy(ticket => Math.Abs(ticket.Rating - rating))
                .FirstOrDefault();
            if (rankedCandidate == null)
            {
                var room = CreateRoom(QueueMode.Ranked, null, resolvedRules);
                var hostDeck = _deckRepository.GetDeck(playerId, deckId);
                var reservation = room.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(hostDeck.CardIds));
                _rankedWaiting.Add(new WaitingTicket(playerId, deckId, rating, room.MatchId, resolvedRules.RulesetId));
                SyncMatchRecord(room);
                return reservation with { WaitingForOpponent = true, Status = "queued" };
            }

            _rankedWaiting.Remove(rankedCandidate);
            var rankedRoom = _matches[rankedCandidate.MatchId];
            var otherDeck = _deckRepository.GetDeck(playerId, deckId);
            var rankedJoined = rankedRoom.Engine.ReserveSeat(playerId, deckId, _catalogService.ResolveDeck(otherDeck.CardIds));
            SyncMatchRecord(rankedRoom);
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

        // Log action to replay
        _ = LogReplayActionAsync(matchId, playerId, "PlayCard", new { runtimeHandKey, slotIndex });

        return room.Engine.CreateSnapshotForSeat(seatIndex);
    }

    public MatchSnapshot EndTurn(string matchId, string playerId)
    {
        var room = GetRoom(matchId);
        room.Engine.EndTurn(playerId);
        var seatIndex = room.Engine.Seats.First(x => x.PlayerId == playerId).SeatIndex;

        // Log action to replay
        _ = LogReplayActionAsync(matchId, playerId, "EndTurn", new { });

        return room.Engine.CreateSnapshotForSeat(seatIndex);
    }

    public MatchSnapshot DestroyCard(string matchId, string playerId, string runtimeCardId)
    {
        var room = GetRoom(matchId);
        room.Engine.DestroyCard(playerId, runtimeCardId);
        var seatIndex = room.Engine.Seats.First(x => x.PlayerId == playerId).SeatIndex;

        _ = LogReplayActionAsync(matchId, playerId, "DestroyCard", new { runtimeCardId });

        return room.Engine.CreateSnapshotForSeat(seatIndex);
    }

    public MatchSnapshot Forfeit(string matchId, string playerId)
    {
        var room = GetRoom(matchId);
        room.Engine.Forfeit(playerId);
        var seatIndex = room.Engine.Seats.First(x => x.PlayerId == playerId).SeatIndex;

        // Log action to replay
        _ = LogReplayActionAsync(matchId, playerId, "Forfeit", new { });

        return room.Engine.CreateSnapshotForSeat(seatIndex);
    }

    public MatchCompletionResponse CompleteMatch(string matchId, string playerId, string opponentId, bool playerWon, int durationSeconds)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ratingDbService = scope.ServiceProvider.GetRequiredService<IRatingDbService>();

            using var transaction = dbContext.Database.BeginTransaction();
            try
            {
                var room = GetRoom(matchId);
                var match = dbContext.Matches.FirstOrDefault(m => m.MatchId == matchId);

                if (match == null)
                {
                    match = new MatchRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        MatchId = matchId,
                        RoomCode = room.Engine.RoomCode,
                        Player1Id = room.Engine.Seats[0].PlayerId,
                        Player2Id = room.Engine.Seats[1].PlayerId,
                        Mode = room.Engine.Mode,
                        GameRulesetId = room.Engine.Rules.RulesetId,
                        GameRulesetName = room.Engine.Rules.DisplayName,
                        GameRulesSnapshotJson = room.Engine.Rules.ToSnapshotJson(),
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    dbContext.Matches.Add(match);
                    dbContext.SaveChanges();
                }

                if (match.CompletedAt.HasValue)
                    return new MatchCompletionResponse(matchId, false, "Match already completed");

                var isPlayer1 = match.Player1Id == playerId;
                var isPlayer2 = match.Player2Id == playerId;

                if (!isPlayer1 && !isPlayer2)
                    throw new InvalidOperationException("Player not in this match");

                var expectedOpponent = isPlayer1 ? match.Player2Id : match.Player1Id;
                if (expectedOpponent != opponentId)
                    throw new InvalidOperationException("Opponent ID mismatch");

                var player1Rating = match.Player1RatingBefore ?? 1000;
                var player2Rating = match.Player2RatingBefore ?? 1000;
                var player1Won = isPlayer1 ? playerWon : !playerWon;

                var (newRating1, newRating2) = ratingDbService.UpdateRatingsForMatch(
                    match.Player1Id, match.Player2Id, player1Rating, player2Rating, player1Won);

                match.WinnerId = playerWon ? playerId : opponentId;
                match.DurationSeconds = durationSeconds;
                match.CompletedAt = DateTimeOffset.UtcNow;
                match.Player1RatingBefore = player1Rating;
                match.Player2RatingBefore = player2Rating;
                match.Player1RatingAfter = newRating1;
                match.Player2RatingAfter = newRating2;

                dbContext.Matches.Update(match);
                dbContext.SaveChanges();
                transaction.Commit();

                return new MatchCompletionResponse(
                    matchId,
                    true,
                    $"Match completed. Winner: {(playerWon ? playerId : opponentId)}, Rating change: {(playerWon ? "+" : "")}{newRating1 - player1Rating}");
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            return new MatchCompletionResponse(matchId, false, $"Error: {ex.Message}");
        }
    }

    public PostActionsResponse ProcessActions(string matchId, PostActionsRequest request)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var match = dbContext.Matches.FirstOrDefault(m => m.MatchId == matchId);
                if (match == null)
                {
                    match = new MatchRecord
                    {
                        Id = Guid.NewGuid().ToString(),
                        MatchId = matchId,
                        GameRulesetId = string.Empty,
                        GameRulesetName = string.Empty,
                        GameRulesSnapshotJson = "{}",
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                dbContext.Matches.Add(match);
                dbContext.SaveChanges();
            }

            foreach (var action in request.Actions)
            {
                var actionRecord = new MatchAction
                {
                    Id = Guid.NewGuid().ToString(),
                    MatchId = matchId,
                    PlayerId = action.PlayerId,
                    ActionType = action.ActionType,
                    ActionNumber = action.ActionNumber,
                    ActionData = System.Text.Json.JsonSerializer.Serialize(action.Data) ?? ""
                };
                dbContext.MatchActions.Add(actionRecord);
            }

            dbContext.SaveChanges();
            return new PostActionsResponse(matchId, request.Actions.Count, true, $"Recorded {request.Actions.Count} actions");
        }
        catch (Exception ex)
        {
            return new PostActionsResponse(matchId, 0, false, $"Error: {ex.Message}");
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

    private MatchRoom CreateRoom(QueueMode mode, string? name, ResolvedGameRules resolvedRules)
    {
        var matchId = Guid.NewGuid().ToString("N");
        var code = CreateRoomCode();
        var room = new MatchRoom(
            matchId,
            name ?? $"{mode}-match",
            new MatchEngine(matchId, code, mode, _disconnectGrace, resolvedRules.Rules),
            resolvedRules.SnapshotJson);
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

    private async Task LogReplayActionAsync(string matchId, string playerId, string actionType, object actionData)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var replayService = scope.ServiceProvider.GetRequiredService<IReplayPersistenceService>();
            var actionNumber = MatchActionCounter.IncrementAndGet(matchId);
            await replayService.LogActionAsync(matchId, actionNumber, playerId, actionType, actionData);
        }
        catch (Exception ex)
        {
            // Non-blocking - don't fail the game action if replay logging fails
            var logger = _serviceProvider.GetRequiredService<ILogger<InMemoryMatchService>>();
            logger.LogError(ex, "Failed to log replay action for match {MatchId}", matchId);
        }
    }

    private void SyncMatchRecord(MatchRoom room)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var model = dbContext.Matches.FirstOrDefault(match => match.MatchId == room.MatchId);
        if (model == null)
        {
            model = new MatchRecord
            {
                MatchId = room.MatchId,
                RoomCode = room.Engine.RoomCode,
                Mode = room.Engine.Mode,
                CreatedAt = DateTimeOffset.UtcNow,
                GameRulesetId = room.Engine.Rules.RulesetId,
                GameRulesetName = room.Engine.Rules.DisplayName,
                GameRulesSnapshotJson = room.RulesSnapshotJson
            };
            dbContext.Matches.Add(model);
        }

        model.Player1Id = room.Engine.Seats[0].PlayerId;
        model.Player2Id = room.Engine.Seats[1].PlayerId;
        model.Player1ReconnectToken = room.Engine.Seats[0].ReconnectToken;
        model.Player2ReconnectToken = room.Engine.Seats[1].ReconnectToken;
        model.GameRulesetId = room.Engine.Rules.RulesetId;
        model.GameRulesetName = room.Engine.Rules.DisplayName;
        model.GameRulesSnapshotJson = room.RulesSnapshotJson;

        dbContext.SaveChanges();
    }

    private sealed record WaitingTicket(string PlayerId, string DeckId, int Rating, string MatchId, string RulesetId);

    private sealed class MatchRoom
    {
        public MatchRoom(string matchId, string name, MatchEngine engine, string rulesSnapshotJson)
        {
            MatchId = matchId;
            Name = name;
            Engine = engine;
            RulesSnapshotJson = rulesSnapshotJson;
        }

        public string MatchId { get; }
        public string Name { get; }
        public MatchEngine Engine { get; }
        public string RulesSnapshotJson { get; }
        public ConcurrentDictionary<string, string> PlayerConnections { get; } = new(StringComparer.Ordinal);
    }
}
