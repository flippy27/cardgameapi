using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CardDuel.ServerApi.Contracts;

namespace CardDuel.ServerApi.Game;

public enum MatchPhase
{
    WaitingForPlayers = 0,
    WaitingForReady = 1,
    InProgress = 2,
    Completed = 3,
    Abandoned = 4
}

public enum AllowedRow
{
    FrontOnly = 0,
    BackOnly = 1,
    Flexible = 2
}

public enum TriggerKind
{
    OnPlay = 0,
    OnTurnStart = 1,
    OnTurnEnd = 2,
    OnBattlePhase = 3
}

public enum TargetSelectorKind
{
    Self = 0,
    FrontlineFirst = 1,
    BacklineFirst = 2,
    AllEnemies = 3,
    LowestHealthAlly = 4
}

public enum EffectKind
{
    Damage = 0,
    Heal = 1,
    GainArmor = 2,
    BuffAttack = 3,
    HitHero = 4
}

public enum BoardSlot
{
    Front = 0,
    BackLeft = 1,
    BackRight = 2
}

public sealed record ServerEffectDefinition(EffectKind Kind, int Amount);

public sealed record ServerAbilityDefinition(
    string AbilityId,
    TriggerKind Trigger,
    TargetSelectorKind Selector,
    IReadOnlyList<ServerEffectDefinition> Effects);

public sealed record ServerCardDefinition(
    string CardId,
    string DisplayName,
    int ManaCost,
    int Attack,
    int Health,
    int Armor,
    AllowedRow AllowedRow,
    TargetSelectorKind DefaultAttackSelector,
    IReadOnlyList<ServerAbilityDefinition> Abilities);

public sealed record RuntimeHandCard(string RuntimeHandKey, ServerCardDefinition Definition);

public sealed class RuntimeBoardCard
{
    public required string RuntimeId { get; init; }
    public required ServerCardDefinition Definition { get; init; }
    public required int OwnerSeatIndex { get; init; }
    public required BoardSlot Slot { get; set; }
    public required int Attack { get; set; }
    public required int CurrentHealth { get; set; }
    public required int MaxHealth { get; init; }
    public required int Armor { get; set; }
    public bool IsDead => CurrentHealth <= 0;
}

public sealed class RuntimeSeatState
{
    public int SeatIndex { get; init; }
    public string PlayerId { get; set; } = string.Empty;
    public string DeckId { get; set; } = string.Empty;
    public string DeckHash { get; set; } = string.Empty;
    public bool Connected { get; set; }
    public bool Ready { get; set; }
    public string ReconnectToken { get; set; } = string.Empty;
    public DateTimeOffset? DisconnectedAt { get; set; }
    public int HeroHealth { get; set; } = 20;
    public int Mana { get; set; } = 1;
    public int MaxMana { get; set; } = 1;
    public List<ServerCardDefinition> Deck { get; } = new();
    public List<RuntimeHandCard> Hand { get; } = new();
    public Dictionary<BoardSlot, RuntimeBoardCard?> Board { get; } = new()
    {
        [BoardSlot.Front] = null,
        [BoardSlot.BackLeft] = null,
        [BoardSlot.BackRight] = null
    };
}

public sealed record HandCardSnapshot(
    string RuntimeHandKey,
    string CardId,
    string DisplayName,
    int ManaCost,
    int Attack,
    int Health,
    bool CanBePlayedInFront,
    bool CanBePlayedInBack);

public sealed record BoardCardSnapshot(
    string RuntimeId,
    string CardId,
    string DisplayName,
    int OwnerSeatIndex,
    int Attack,
    int CurrentHealth,
    int MaxHealth,
    int Armor,
    BoardSlot Slot);

public sealed record BoardSlotSnapshot(BoardSlot Slot, bool Occupied, BoardCardSnapshot? Occupant);

public sealed record SeatSnapshot(
    int SeatIndex,
    bool Connected,
    bool Ready,
    int HeroHealth,
    int Mana,
    int MaxMana,
    int RemainingDeckCount,
    IReadOnlyList<HandCardSnapshot> Hand,
    IReadOnlyList<BoardSlotSnapshot> Board);

public sealed record MatchSnapshot(
    string MatchId,
    string RoomCode,
    QueueMode Mode,
    MatchPhase Phase,
    int LocalSeatIndex,
    int ActiveSeatIndex,
    int TurnNumber,
    int ConnectedPlayers,
    int? WinnerSeatIndex,
    int MatchSeed,
    double ReconnectGraceRemainingSeconds,
    string StatusMessage,
    IReadOnlyList<SeatSnapshot> Seats,
    IReadOnlyList<string> Logs,
    bool DuelEnded);

public sealed class MatchEngine
{
    private readonly TimeSpan _disconnectGrace;
    private readonly QueueMode _mode;
    private readonly string _matchId;
    private readonly string _roomCode;
    private readonly RuntimeSeatState[] _seats = { new() { SeatIndex = 0 }, new() { SeatIndex = 1 } };
    private readonly List<string> _logs = new();
    private readonly ConcurrentDictionary<string, string> _spectators = new();

    public MatchEngine(string matchId, string roomCode, QueueMode mode, TimeSpan disconnectGrace)
    {
        _matchId = matchId;
        _roomCode = roomCode;
        _mode = mode;
        _disconnectGrace = disconnectGrace;
        Phase = MatchPhase.WaitingForPlayers;
    }

    public MatchPhase Phase { get; private set; }
    public int ActiveSeatIndex { get; private set; }
    public int TurnNumber { get; private set; }
    public int MatchSeed { get; private set; }
    public int? WinnerSeatIndex { get; private set; }
    public bool DuelEnded { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public string RoomCode => _roomCode;
    public QueueMode Mode => _mode;
    public string MatchId => _matchId;

    public IReadOnlyList<RuntimeSeatState> Seats => _seats;

    public MatchReservationDto ReserveSeat(string playerId, string deckId, IReadOnlyList<ServerCardDefinition> cards)
    {
        var existing = _seats.FirstOrDefault(x => x.PlayerId == playerId);
        if (existing != null)
        {
            existing.Connected = true;
            existing.DisconnectedAt = null;
            return ToReservation(existing, waitingForOpponent: !BothSeatsFilled());
        }

        var seat = _seats.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.PlayerId));
        if (seat == null)
        {
            throw new InvalidOperationException("Match is full.");
        }

        seat.PlayerId = playerId;
        seat.DeckId = deckId;
        seat.Deck.Clear();
        seat.Deck.AddRange(cards);
        seat.DeckHash = ComputeDeckHash(cards);
        seat.ReconnectToken = Guid.NewGuid().ToString("N");
        seat.Connected = true;
        seat.Ready = false;
        seat.DisconnectedAt = null;
        ResetSeatState(seat);

        Phase = BothSeatsFilled() ? MatchPhase.WaitingForReady : MatchPhase.WaitingForPlayers;
        UpdatedAt = DateTimeOffset.UtcNow;
        _logs.Add($"Seat {seat.SeatIndex + 1} reserved by {playerId}.");

        return ToReservation(seat, waitingForOpponent: !BothSeatsFilled());
    }

    public bool TryReconnect(string playerId, string reconnectToken, out int seatIndex)
    {
        var seat = _seats.FirstOrDefault(x => x.PlayerId == playerId && x.ReconnectToken == reconnectToken);
        if (seat == null)
        {
            seatIndex = -1;
            return false;
        }

        seat.Connected = true;
        seat.DisconnectedAt = null;
        seatIndex = seat.SeatIndex;
        UpdatedAt = DateTimeOffset.UtcNow;
        _logs.Add($"Seat {seatIndex + 1} reconnected.");
        return true;
    }

    public void SetReady(string playerId, bool ready)
    {
        var seat = GetSeat(playerId);
        seat.Ready = ready;
        UpdatedAt = DateTimeOffset.UtcNow;
        if (BothSeatsFilled() && _seats.All(x => x.Ready))
        {
            StartMatch();
        }
        else if (Phase != MatchPhase.InProgress)
        {
            Phase = BothSeatsFilled() ? MatchPhase.WaitingForReady : MatchPhase.WaitingForPlayers;
        }
    }

    public void MarkDisconnected(string playerId)
    {
        var seat = GetSeat(playerId);
        seat.Connected = false;
        seat.Ready = false;
        seat.DisconnectedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        _logs.Add($"Seat {seat.SeatIndex + 1} disconnected.");

        if (Phase != MatchPhase.InProgress)
        {
            Phase = BothSeatsFilled() ? MatchPhase.WaitingForReady : MatchPhase.WaitingForPlayers;
        }
    }

    public void Tick()
    {
        if (Phase != MatchPhase.InProgress)
        {
            return;
        }

        var disconnected = _seats.FirstOrDefault(x => !x.Connected && x.DisconnectedAt.HasValue);
        if (disconnected == null)
        {
            return;
        }

        if (disconnected.DisconnectedAt.HasValue && DateTimeOffset.UtcNow - disconnected.DisconnectedAt.Value >= _disconnectGrace)
        {
            Forfeit(disconnected.PlayerId, disconnected: true);
        }
    }

    public void Forfeit(string playerId, bool disconnected = false)
    {
        var seat = GetSeat(playerId);
        WinnerSeatIndex = 1 - seat.SeatIndex;
        DuelEnded = true;
        Phase = MatchPhase.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
        _logs.Add(disconnected
            ? $"Seat {seat.SeatIndex + 1} did not reconnect. Victory by abandonment."
            : $"Seat {seat.SeatIndex + 1} forfeited the match.");
    }

    public void PlayCard(string playerId, string runtimeHandKey, BoardSlot slot)
    {
        EnsurePlayable(playerId);

        var seat = GetSeat(playerId);
        var card = seat.Hand.FirstOrDefault(x => x.RuntimeHandKey == runtimeHandKey)
            ?? throw new InvalidOperationException("Card not found in hand.");

        EnsureLegalPlacement(seat, card.Definition, slot);

        seat.Mana -= card.Definition.ManaCost;
        seat.Hand.Remove(card);
        seat.Board[slot] = new RuntimeBoardCard
        {
            RuntimeId = Guid.NewGuid().ToString("N"),
            Definition = card.Definition,
            OwnerSeatIndex = seat.SeatIndex,
            Slot = slot,
            Attack = card.Definition.Attack,
            CurrentHealth = card.Definition.Health,
            MaxHealth = card.Definition.Health,
            Armor = card.Definition.Armor
        };

        _logs.Add($"{card.Definition.DisplayName} entered {slot}.");
        ResolveTriggeredAbilities(seat.SeatIndex, seat.Board[slot]!, TriggerKind.OnPlay);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void EndTurn(string playerId)
    {
        EnsurePlayable(playerId);
        var seat = GetSeat(playerId);
        ResolveTurnAbilities(seat.SeatIndex, TriggerKind.OnTurnEnd);
        ExecuteBattlePhase(seat.SeatIndex);
        CleanupDeaths();

        if (!DuelEnded)
        {
            ActiveSeatIndex = 1 - ActiveSeatIndex;
            TurnNumber += 1;
            var next = _seats[ActiveSeatIndex];
            next.MaxMana = Math.Min(10, next.MaxMana + 1);
            next.Mana = next.MaxMana;
            DrawCard(next);
            ResolveTurnAbilities(next.SeatIndex, TriggerKind.OnTurnStart);
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddSpectator(string spectatorId, string connectionId)
    {
        _spectators[spectatorId] = connectionId;
    }

    public IReadOnlyDictionary<string, string> Spectators => _spectators;

    public MatchSnapshot CreateSnapshotForSeat(int localSeatIndex, bool redactHandsForSpectator = false)
    {
        var seats = new List<SeatSnapshot>(2);
        foreach (var seat in _seats)
        {
            var includeHand = !redactHandsForSpectator && seat.SeatIndex == localSeatIndex;
            seats.Add(new SeatSnapshot(
                seat.SeatIndex,
                seat.Connected,
                seat.Ready,
                seat.HeroHealth,
                seat.Mana,
                seat.MaxMana,
                seat.Deck.Count,
                includeHand
                    ? seat.Hand.Select(card => new HandCardSnapshot(
                        card.RuntimeHandKey,
                        card.Definition.CardId,
                        card.Definition.DisplayName,
                        card.Definition.ManaCost,
                        card.Definition.Attack,
                        card.Definition.Health,
                        card.Definition.AllowedRow is AllowedRow.FrontOnly or AllowedRow.Flexible,
                        card.Definition.AllowedRow is AllowedRow.BackOnly or AllowedRow.Flexible)).ToArray()
                    : Array.Empty<HandCardSnapshot>(),
                seat.Board.Select(pair => new BoardSlotSnapshot(
                    pair.Key,
                    pair.Value != null,
                    pair.Value == null
                        ? null
                        : new BoardCardSnapshot(
                            pair.Value.RuntimeId,
                            pair.Value.Definition.CardId,
                            pair.Value.Definition.DisplayName,
                            pair.Value.OwnerSeatIndex,
                            pair.Value.Attack,
                            pair.Value.CurrentHealth,
                            pair.Value.MaxHealth,
                            pair.Value.Armor,
                            pair.Value.Slot))).ToArray()));
        }

        var otherSeat = localSeatIndex is 0 or 1 ? _seats[1 - localSeatIndex] : null;
        var reconnectGrace = otherSeat is { Connected: false, DisconnectedAt: not null } && Phase == MatchPhase.InProgress
            ? Math.Max(0d, (_disconnectGrace - (DateTimeOffset.UtcNow - otherSeat.DisconnectedAt.Value)).TotalSeconds)
            : 0d;

        return new MatchSnapshot(
            _matchId,
            _roomCode,
            _mode,
            Phase,
            localSeatIndex,
            ActiveSeatIndex,
            TurnNumber,
            _seats.Count(x => x.Connected),
            WinnerSeatIndex,
            MatchSeed,
            reconnectGrace,
            BuildStatus(localSeatIndex),
            seats,
            _logs.TakeLast(20).ToArray(),
            DuelEnded);
    }

    public MatchSummaryDto ToSummary()
    {
        return new MatchSummaryDto(_matchId, _roomCode, _mode, _seats.Count(x => x.Connected), Phase == MatchPhase.InProgress, DuelEnded, WinnerSeatIndex);
    }

    private void StartMatch()
    {
        MatchSeed = RandomNumberGenerator.GetInt32(1, int.MaxValue);
        foreach (var seat in _seats)
        {
            ResetSeatState(seat);
            ShuffleDeck(seat, MatchSeed ^ ((seat.SeatIndex + 1) * 7919));
        }

        ActiveSeatIndex = 0;
        TurnNumber = 1;
        Phase = MatchPhase.InProgress;
        DuelEnded = false;
        WinnerSeatIndex = null;

        for (var i = 0; i < 4; i++)
        {
            DrawCard(_seats[0]);
            DrawCard(_seats[1]);
        }

        _logs.Add($"Match started with seed {MatchSeed}.");
    }

    private void ResetSeatState(RuntimeSeatState seat)
    {
        seat.HeroHealth = 20;
        seat.Mana = 1;
        seat.MaxMana = 1;
        seat.Hand.Clear();
        seat.Board[BoardSlot.Front] = null;
        seat.Board[BoardSlot.BackLeft] = null;
        seat.Board[BoardSlot.BackRight] = null;
    }

    private void DrawCard(RuntimeSeatState seat)
    {
        if (seat.Deck.Count == 0)
        {
            return;
        }

        var next = seat.Deck[0];
        seat.Deck.RemoveAt(0);
        seat.Hand.Add(new RuntimeHandCard(Guid.NewGuid().ToString("N"), next));
    }

    private void ShuffleDeck(RuntimeSeatState seat, int seed)
    {
        var random = new Random(seed);
        var cards = seat.Deck.ToList();
        seat.Deck.Clear();
        while (cards.Count > 0)
        {
            var pick = random.Next(0, cards.Count);
            seat.Deck.Add(cards[pick]);
            cards.RemoveAt(pick);
        }
    }

    private void EnsurePlayable(string playerId)
    {
        if (Phase != MatchPhase.InProgress || DuelEnded)
        {
            throw new InvalidOperationException("Match is not currently playable.");
        }

        var seat = GetSeat(playerId);
        if (seat.SeatIndex != ActiveSeatIndex)
        {
            throw new InvalidOperationException("It is not this player's turn.");
        }
    }

    private void EnsureLegalPlacement(RuntimeSeatState seat, ServerCardDefinition card, BoardSlot slot)
    {
        if (seat.Board[slot] != null)
        {
            throw new InvalidOperationException("Slot is occupied.");
        }

        if (card.ManaCost > seat.Mana)
        {
            throw new InvalidOperationException("Not enough mana.");
        }

        if (slot == BoardSlot.Front && card.AllowedRow == AllowedRow.BackOnly)
        {
            throw new InvalidOperationException("This card can only be played in the back row.");
        }

        if ((slot == BoardSlot.BackLeft || slot == BoardSlot.BackRight) && card.AllowedRow == AllowedRow.FrontOnly)
        {
            throw new InvalidOperationException("This card can only be played in the front row.");
        }
    }

    private void ExecuteBattlePhase(int sourceSeatIndex)
    {
        foreach (var slot in new[] { BoardSlot.Front, BoardSlot.BackLeft, BoardSlot.BackRight })
        {
            var attacker = _seats[sourceSeatIndex].Board[slot];
            if (attacker == null || attacker.IsDead)
            {
                continue;
            }

            ResolveTriggeredAbilities(sourceSeatIndex, attacker, TriggerKind.OnBattlePhase);
            var targets = SelectTargets(sourceSeatIndex, 1 - sourceSeatIndex, attacker.RuntimeId, attacker.Definition.DefaultAttackSelector);
            if (targets.Count == 0)
            {
                DamageHero(1 - sourceSeatIndex, attacker.Attack);
                continue;
            }

            foreach (var target in targets)
            {
                DealDamage(attacker.RuntimeId, target.RuntimeId, attacker.Attack, ignoreArmor: false);
            }
        }
    }

    private void ResolveTurnAbilities(int seatIndex, TriggerKind trigger)
    {
        foreach (var slot in _seats[seatIndex].Board.Values)
        {
            if (slot != null)
            {
                ResolveTriggeredAbilities(seatIndex, slot, trigger);
            }
        }
    }

    private void ResolveTriggeredAbilities(int sourceSeatIndex, RuntimeBoardCard source, TriggerKind trigger)
    {
        foreach (var ability in source.Definition.Abilities.Where(x => x.Trigger == trigger))
        {
            var targets = SelectTargets(sourceSeatIndex, 1 - sourceSeatIndex, source.RuntimeId, ability.Selector);
            if (targets.Count == 0)
            {
                ApplyEffects(sourceSeatIndex, sourceSeatIndex, source.RuntimeId, null, ability.Effects);
                continue;
            }

            foreach (var target in targets)
            {
                ApplyEffects(sourceSeatIndex, target.OwnerSeatIndex, source.RuntimeId, target, ability.Effects);
            }
        }
    }

    private void ApplyEffects(int sourceSeatIndex, int targetSeatIndex, string sourceRuntimeId, RuntimeBoardCard? target, IReadOnlyList<ServerEffectDefinition> effects)
    {
        foreach (var effect in effects)
        {
            switch (effect.Kind)
            {
                case EffectKind.Damage when target != null:
                    DealDamage(sourceRuntimeId, target.RuntimeId, effect.Amount, ignoreArmor: false);
                    break;
                case EffectKind.Heal when target != null:
                    target.CurrentHealth = Math.Min(target.MaxHealth, target.CurrentHealth + effect.Amount);
                    _logs.Add($"{target.Definition.DisplayName} healed {effect.Amount}.");
                    break;
                case EffectKind.GainArmor when target != null:
                    target.Armor += effect.Amount;
                    break;
                case EffectKind.BuffAttack when target != null:
                    target.Attack = Math.Max(0, target.Attack + effect.Amount);
                    break;
                case EffectKind.HitHero:
                    DamageHero(targetSeatIndex, effect.Amount);
                    break;
            }
        }
    }

    private List<RuntimeBoardCard> SelectTargets(int sourceSeatIndex, int enemySeatIndex, string sourceRuntimeId, TargetSelectorKind selector)
    {
        var targets = new List<RuntimeBoardCard>();
        switch (selector)
        {
            case TargetSelectorKind.Self:
                var self = FindCard(sourceRuntimeId);
                if (self != null) targets.Add(self);
                break;
            case TargetSelectorKind.FrontlineFirst:
                var front = _seats[enemySeatIndex].Board[BoardSlot.Front];
                if (front != null) targets.Add(front);
                else targets.AddRange(_seats[enemySeatIndex].Board.Values.Where(x => x != null)!);
                break;
            case TargetSelectorKind.BacklineFirst:
                if (_seats[enemySeatIndex].Board[BoardSlot.BackLeft] != null) targets.Add(_seats[enemySeatIndex].Board[BoardSlot.BackLeft]!);
                else if (_seats[enemySeatIndex].Board[BoardSlot.BackRight] != null) targets.Add(_seats[enemySeatIndex].Board[BoardSlot.BackRight]!);
                else if (_seats[enemySeatIndex].Board[BoardSlot.Front] != null) targets.Add(_seats[enemySeatIndex].Board[BoardSlot.Front]!);
                break;
            case TargetSelectorKind.AllEnemies:
                targets.AddRange(_seats[enemySeatIndex].Board.Values.Where(x => x != null)!);
                break;
            case TargetSelectorKind.LowestHealthAlly:
                var lowest = _seats[sourceSeatIndex].Board.Values.Where(x => x != null).OrderBy(x => x!.CurrentHealth).FirstOrDefault();
                if (lowest != null) targets.Add(lowest);
                break;
        }

        return targets;
    }

    private RuntimeBoardCard? FindCard(string runtimeId)
    {
        return _seats.SelectMany(seat => seat.Board.Values).FirstOrDefault(card => card?.RuntimeId == runtimeId);
    }

    private void DealDamage(string sourceRuntimeId, string targetRuntimeId, int amount, bool ignoreArmor)
    {
        var target = FindCard(targetRuntimeId);
        if (target == null || amount <= 0)
        {
            return;
        }

        var pending = amount;
        if (!ignoreArmor && target.Armor > 0)
        {
            var absorbed = Math.Min(target.Armor, pending);
            target.Armor -= absorbed;
            pending -= absorbed;
        }

        if (pending > 0)
        {
            target.CurrentHealth -= pending;
        }

        _logs.Add($"{sourceRuntimeId} hit {target.Definition.DisplayName} for {amount}.");
        CleanupDeaths();
    }

    private void DamageHero(int targetSeatIndex, int amount)
    {
        var seat = _seats[targetSeatIndex];
        seat.HeroHealth = Math.Max(0, seat.HeroHealth - amount);
        if (seat.HeroHealth <= 0)
        {
            WinnerSeatIndex = 1 - targetSeatIndex;
            DuelEnded = true;
            Phase = MatchPhase.Completed;
        }
    }

    private void CleanupDeaths()
    {
        foreach (var seat in _seats)
        {
            foreach (var slot in seat.Board.Keys.ToList())
            {
                var occupant = seat.Board[slot];
                if (occupant != null && occupant.IsDead)
                {
                    _logs.Add($"{occupant.Definition.DisplayName} died.");
                    seat.Board[slot] = null;
                }
            }
        }
    }

    private RuntimeSeatState GetSeat(string playerId)
    {
        return _seats.FirstOrDefault(x => x.PlayerId == playerId)
            ?? throw new InvalidOperationException("Player is not part of this match.");
    }

    private bool BothSeatsFilled() => _seats.All(x => !string.IsNullOrWhiteSpace(x.PlayerId));

    private MatchReservationDto ToReservation(RuntimeSeatState seat, bool waitingForOpponent)
    {
        return new MatchReservationDto(
            _matchId,
            _roomCode,
            seat.ReconnectToken,
            seat.SeatIndex,
            _mode,
            waitingForOpponent,
            waitingForOpponent ? "waiting_for_opponent" : "joined");
    }

    private string BuildStatus(int localSeatIndex)
    {
        var remote = localSeatIndex is 0 or 1 ? _seats[1 - localSeatIndex] : null;
        return Phase switch
        {
            MatchPhase.WaitingForPlayers => "Waiting for second player.",
            MatchPhase.WaitingForReady => $"Ready up. Opponent ready: {(remote?.Ready == true ? "yes" : "no")}.",
            MatchPhase.InProgress when remote is { Connected: false } => $"Opponent disconnected. Rejoin window: {Math.Max(0d, (_disconnectGrace - (DateTimeOffset.UtcNow - remote.DisconnectedAt!.Value)).TotalSeconds):0}s.",
            MatchPhase.InProgress => "Match in progress.",
            MatchPhase.Completed when WinnerSeatIndex == localSeatIndex => "Victory.",
            MatchPhase.Completed => "Defeat.",
            _ => string.Empty
        };
    }

    public static string ComputeDeckHash(IReadOnlyList<ServerCardDefinition> cards)
    {
        var raw = string.Join('|', cards.Select(card => $"{card.CardId}:{card.ManaCost}:{card.Attack}:{card.Health}:{card.Armor}:{card.AllowedRow}"));
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return string.Concat(bytes.Select(b => b.ToString("x2")));
    }
}
