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

public enum CardType
{
    Unit = 0,
    Utility = 1,
    Equipment = 2,
    Spell = 3
}

public enum CardRarity
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3
}

public enum CardFaction
{
    Ember = 0,
    Tidal = 1,
    Grove = 2,
    Alloy = 3,
    Void = 4
}

public enum UnitType
{
    Melee = 0,
    Ranged = 1,
    Magic = 2
}

public enum SkillType
{
    Defensive = 0,
    Offensive = 1,
    Equipable = 2,
    Utility = 3,
    Modifier = 4
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
    LowestHealthAlly = 4,
    AllyFront = 5,
    AllyBackLeft = 6,
    AllyBackRight = 7,
    SourceOpponent = 8
}

public enum EffectKind
{
    Damage = 0,
    Heal = 1,
    GainArmor = 2,
    BuffAttack = 3,
    HitHero = 4,
    Stun = 5,
    Poison = 6,
    Leech = 7,
    Evasion = 8,
    Shield = 9,
    Reflection = 10,
    Dodge = 11,
    Enrage = 12,
    ManaBurn = 13,
    Regenerate = 14,
    Execute = 15,
    DiagonalAttack = 16,
    Fly = 17,
    Armor = 18,
    Chain = 19,
    Charge = 20,
    Cleave = 21,
    LastStand = 22,
    MeleeRange = 23,
    Ricochet = 24,
    Taunt = 25,
    Trample = 26,
    Haste = 27,
    AddShield = 28,
    ApplyPoison = 29,
    ApplyStun = 30
}

public enum StatusEffectKind
{
    Poison = 0,
    Stun = 1,
    Shield = 2,
    EnrageCooldown = 3
}

public enum BoardSlot
{
    Front = 0,
    BackLeft = 1,
    BackRight = 2
}

public sealed record ServerEffectDefinition(
    EffectKind Kind,
    int Amount,
    int? SecondaryAmount = null,
    int? DurationTurns = null,
    TargetSelectorKind? TargetSelectorOverride = null,
    string? MetadataJson = null);

public sealed record ServerAbilityDefinition(
    string AbilityId,
    string DisplayName,
    TriggerKind Trigger,
    TargetSelectorKind Selector,
    IReadOnlyList<ServerEffectDefinition> Effects,
    SkillType SkillType = SkillType.Utility,
    string? AnimationCueId = null,
    string? ConditionsJson = null,
    string? MetadataJson = null);

public sealed record ServerCardVisualLayer(
    string Surface,
    string Layer,
    string SourceKind,
    string AssetRef,
    int SortOrder,
    string? MetadataJson);

public sealed record ServerCardVisualProfile(
    string ProfileKey,
    string DisplayName,
    bool IsDefault,
    IReadOnlyList<ServerCardVisualLayer> Layers);

public sealed record ServerCardDefinition(
    string CardId,
    string DisplayName,
    string Description,
    int ManaCost,
    int Attack,
    int Health,
    int Armor,
    int CardType,
    int CardRarity,
    int CardFaction,
    int? UnitType,
    AllowedRow AllowedRow,
    TargetSelectorKind DefaultAttackSelector,
    int TurnsUntilCanAttack,
    IReadOnlyList<ServerAbilityDefinition> Abilities,
    int AttackMotionLevel = 0,
    int AttackShakeLevel = 0,
    string? AttackDeliveryType = null,
    IReadOnlyList<ServerCardVisualProfile>? VisualProfiles = null);

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
    public int SummonedTurnNumber { get; init; }
    public List<RuntimeStatusEffect> StatusEffects { get; } = new();
    public HashSet<string> ConsumedOneShotAbilityIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsDead => CurrentHealth <= 0;
}

public sealed class RuntimeStatusEffect
{
    public required StatusEffectKind Kind { get; init; }
    public required int Amount { get; set; }
    public required int RemainingTurns { get; set; }
    public string? SourceRuntimeId { get; init; }
    public string? AbilityId { get; init; }
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
    int AttackMotionLevel,
    int AttackShakeLevel,
    string? AttackDeliveryType,
    IReadOnlyList<StatusEffectSnapshot> StatusEffects,
    BoardSlot Slot);

public sealed record StatusEffectSnapshot(
    StatusEffectKind Kind,
    int Amount,
    int RemainingTurns,
    string? SourceRuntimeId,
    string? AbilityId);

public sealed record BoardSlotSnapshot(BoardSlot Slot, bool Occupied, BoardCardSnapshot? Occupant);

public sealed record SeatSnapshot(
    int SeatIndex,
    string PlayerId,
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
    string RulesetId,
    GameRulesDto Rules,
    MatchPhase Phase,
    int LocalSeatIndex,
    int ActiveSeatIndex,
    string? ActivePlayerId,
    bool IsLocalPlayersTurn,
    int TurnNumber,
    int ConnectedPlayers,
    int? WinnerSeatIndex,
    int MatchSeed,
    double ReconnectGraceRemainingSeconds,
    string StatusMessage,
    IReadOnlyList<SeatSnapshot> Seats,
    IReadOnlyList<string> Logs,
    IReadOnlyList<BattleEventDto> BattleEvents,
    bool DuelEnded);

public sealed record BattleEventDto(
    string EventId,
    int Sequence,
    string Kind,
    int? SourceSeatIndex,
    string? SourceRuntimeId,
    int? TargetSeatIndex,
    string? TargetRuntimeId,
    string? AbilityId,
    EffectKind? EffectKind,
    int Amount,
    int? HpBefore,
    int? HpAfter,
    int? ArmorBefore,
    int? ArmorAfter,
    StatusEffectKind? StatusKind,
    int? DurationTurns,
    string Message);

public sealed class MatchEngine
{
    private readonly TimeSpan _disconnectGrace;
    private readonly QueueMode _mode;
    private readonly string _matchId;
    private readonly string _roomCode;
    private readonly GameRules _rules;
    private readonly GameRulesDto _rulesDto;
    private readonly RuntimeSeatState[] _seats = { new() { SeatIndex = 0 }, new() { SeatIndex = 1 } };
    private readonly List<string> _logs = new();
    private readonly List<BattleEventDto> _battleEvents = new();
    private readonly ConcurrentDictionary<string, string> _spectators = new();
    private int _battleEventSequence;

    public MatchEngine(string matchId, string roomCode, QueueMode mode, TimeSpan disconnectGrace, GameRules rules)
    {
        _matchId = matchId;
        _roomCode = roomCode;
        _mode = mode;
        _disconnectGrace = disconnectGrace;
        _rules = rules;
        _rulesDto = rules.ToDto();
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
    public GameRules Rules => _rules;
    public GameRulesDto RulesDto => _rulesDto;

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
            ?? throw GameActionException.CardNotFoundInHand();

        EnsureLegalPlacement(seat, card.Definition, slot);
        ShiftBoardForPlacement(seat, slot);

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
            ,
            SummonedTurnNumber = TurnNumber
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
            if (_rules.ManaGrantTiming == ManaGrantTiming.EndOfTurn)
            {
                GrantTurnMana(seat);
            }

            ActiveSeatIndex = 1 - ActiveSeatIndex;
            TurnNumber += 1;
            var next = _seats[ActiveSeatIndex];

            if (_rules.ManaGrantTiming == ManaGrantTiming.StartOfTurn)
            {
                GrantTurnMana(next);
            }

            var cardsToDraw = _rules.GetSeatRules(next.SeatIndex).CardsDrawnOnTurnStart;
            for (var drawIndex = 0; drawIndex < cardsToDraw; drawIndex++)
            {
                DrawCard(next);
            }

            ResolveTurnAbilities(next.SeatIndex, TriggerKind.OnTurnStart);
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void DestroyCard(string playerId, string runtimeCardId)
    {
        var seat = EnsureMatchInProgress(playerId);
        var card = FindCard(runtimeCardId) ?? throw GameActionException.RuntimeCardNotFound();
        if (card.OwnerSeatIndex != seat.SeatIndex)
        {
            throw GameActionException.CannotDestroyOpponentsCard();
        }

        var hpBefore = card.CurrentHealth;
        var armorBefore = card.Armor;
        card.CurrentHealth = 0;
        _logs.Add($"{card.Definition.DisplayName} was destroyed by its owner.");
        AddBattleEvent(
            "card_destroyed",
            seat.SeatIndex,
            card.RuntimeId,
            card.OwnerSeatIndex,
            card.RuntimeId,
            null,
            null,
            0,
            hpBefore,
            card.CurrentHealth,
            armorBefore,
            card.Armor,
            null,
            null,
            $"{card.Definition.DisplayName} was destroyed by its owner.");
        CleanupDeaths();
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
                seat.PlayerId,
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
                            pair.Value.Definition.AttackMotionLevel,
                            pair.Value.Definition.AttackShakeLevel,
                            pair.Value.Definition.AttackDeliveryType,
                            pair.Value.StatusEffects.Select(status => new StatusEffectSnapshot(
                                status.Kind,
                                status.Amount,
                                status.RemainingTurns,
                                status.SourceRuntimeId,
                                status.AbilityId)).ToArray(),
                            pair.Value.Slot))).ToArray()));
        }

        var otherSeat = localSeatIndex is 0 or 1 ? _seats[1 - localSeatIndex] : null;
        var reconnectGrace = otherSeat is { Connected: false, DisconnectedAt: not null } && Phase == MatchPhase.InProgress
            ? Math.Max(0d, (_disconnectGrace - (DateTimeOffset.UtcNow - otherSeat.DisconnectedAt.Value)).TotalSeconds)
            : 0d;
        var activePlayerId = Phase == MatchPhase.InProgress && ActiveSeatIndex is 0 or 1
            ? _seats[ActiveSeatIndex].PlayerId
            : null;
        var isLocalPlayersTurn = Phase == MatchPhase.InProgress
            && !DuelEnded
            && localSeatIndex is 0 or 1
            && localSeatIndex == ActiveSeatIndex;

        return new MatchSnapshot(
            _matchId,
            _roomCode,
            _mode,
            _rules.RulesetId,
            _rulesDto,
            Phase,
            localSeatIndex,
            ActiveSeatIndex,
            activePlayerId,
            isLocalPlayersTurn,
            TurnNumber,
            _seats.Count(x => x.Connected),
            WinnerSeatIndex,
            MatchSeed,
            reconnectGrace,
            BuildStatus(localSeatIndex),
            seats,
            _logs.TakeLast(20).ToArray(),
            _battleEvents.TakeLast(80).ToArray(),
            DuelEnded);
    }

    public MatchSummaryDto ToSummary()
    {
        return new MatchSummaryDto(
            _matchId,
            _roomCode,
            _mode,
            _seats.Count(x => x.Connected),
            Phase == MatchPhase.InProgress,
            DuelEnded,
            WinnerSeatIndex,
            _rules.RulesetId,
            _rules.DisplayName);
    }

    private void StartMatch()
    {
        MatchSeed = RandomNumberGenerator.GetInt32(1, int.MaxValue);
        foreach (var seat in _seats)
        {
            ResetSeatState(seat);
            ShuffleDeck(seat, MatchSeed ^ ((seat.SeatIndex + 1) * 7919));
        }

        ActiveSeatIndex = _rules.StartingSeatIndex;
        TurnNumber = 1;
        Phase = MatchPhase.InProgress;
        DuelEnded = false;
        WinnerSeatIndex = null;

        for (var i = 0; i < _rules.InitialDrawCount; i++)
        {
            DrawCard(_seats[0]);
            DrawCard(_seats[1]);
        }

        _logs.Add($"Match started with seed {MatchSeed}.");
    }

    private void ResetSeatState(RuntimeSeatState seat)
    {
        var seatRules = _rules.GetSeatRules(seat.SeatIndex);
        seat.HeroHealth = seatRules.HeroHealth;
        seat.Mana = seatRules.StartingMana;
        seat.MaxMana = Math.Min(seatRules.MaxMana, seatRules.StartingMana);
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

    private RuntimeSeatState EnsureMatchInProgress(string playerId)
    {
        if (Phase != MatchPhase.InProgress || DuelEnded)
        {
            throw GameActionException.MatchNotPlayable();
        }

        return GetSeat(playerId);
    }

    private void EnsurePlayable(string playerId)
    {
        var seat = EnsureMatchInProgress(playerId);
        if (seat.SeatIndex != ActiveSeatIndex)
        {
            var activePlayerId = ActiveSeatIndex is 0 or 1 ? _seats[ActiveSeatIndex].PlayerId : "unknown";
            throw GameActionException.NotYourTurn(activePlayerId, ActiveSeatIndex);
        }
    }

    private void EnsureLegalPlacement(RuntimeSeatState seat, ServerCardDefinition card, BoardSlot slot)
    {
        if (card.ManaCost > seat.Mana)
        {
            throw GameActionException.NotEnoughMana();
        }

        // if (slot == BoardSlot.Front && card.AllowedRow == AllowedRow.BackOnly)
        // {
        //     throw GameActionException.BackOnlyCardRequired();
        // }

        // if ((slot == BoardSlot.BackLeft || slot == BoardSlot.BackRight) && card.AllowedRow == AllowedRow.FrontOnly)
        // {
        //     throw GameActionException.FrontOnlyCardRequired();
        // }

        var front = seat.Board[BoardSlot.Front];
        var left = seat.Board[BoardSlot.BackLeft];
        var right = seat.Board[BoardSlot.BackRight];

        switch (slot)
        {
            case BoardSlot.Front when front != null && left != null && right != null:
                throw GameActionException.BoardLaneFull(slot);
            case BoardSlot.BackLeft when front == null:
                throw GameActionException.FrontSlotRequired();
            case BoardSlot.BackLeft when left != null && right != null:
                throw GameActionException.BoardLaneFull(slot);
            case BoardSlot.BackRight when front == null:
                throw GameActionException.FrontSlotRequired();
            case BoardSlot.BackRight when left == null:
                throw GameActionException.LeftSlotRequired();
            case BoardSlot.BackRight when right != null:
                throw GameActionException.BoardLaneFull(slot);
        }
    }

    private static void ShiftBoardForPlacement(RuntimeSeatState seat, BoardSlot slot)
    {
        switch (slot)
        {
            case BoardSlot.Front:
                if (seat.Board[BoardSlot.Front] == null)
                {
                    return;
                }

                if (seat.Board[BoardSlot.BackLeft] == null)
                {
                    MoveCard(seat, BoardSlot.Front, BoardSlot.BackLeft);
                    return;
                }

                if (seat.Board[BoardSlot.BackRight] == null)
                {
                    MoveCard(seat, BoardSlot.BackLeft, BoardSlot.BackRight);
                    MoveCard(seat, BoardSlot.Front, BoardSlot.BackLeft);
                }
                return;

            case BoardSlot.BackLeft:
                if (seat.Board[BoardSlot.BackLeft] != null && seat.Board[BoardSlot.BackRight] == null)
                {
                    MoveCard(seat, BoardSlot.BackLeft, BoardSlot.BackRight);
                }
                return;
        }
    }

    private static void MoveCard(RuntimeSeatState seat, BoardSlot from, BoardSlot to)
    {
        var occupant = seat.Board[from];
        seat.Board[from] = null;
        seat.Board[to] = occupant;
        if (occupant != null)
        {
            occupant.Slot = to;
        }
    }

    private void ExecuteBattlePhase(int sourceSeatIndex)
    {
        foreach (var card in _seats[sourceSeatIndex].Board.Values.Where(card => card != null).ToArray())
        {
            ApplyBattleStartStatuses(card!);
        }

        CleanupDeaths();
        if (DuelEnded)
        {
            return;
        }

        foreach (var slot in new[] { BoardSlot.Front, BoardSlot.BackLeft, BoardSlot.BackRight })
        {
            var attacker = _seats[sourceSeatIndex].Board[slot];
            if (attacker == null || attacker.IsDead)
            {
                continue;
            }

            if (ConsumeSkipAttackStatus(attacker, StatusEffectKind.Stun, "stun_skip"))
            {
                continue;
            }

            if (ConsumeSkipAttackStatus(attacker, StatusEffectKind.EnrageCooldown, "enrage_cooldown_skip"))
            {
                continue;
            }

            if (!CanAttackThisTurn(attacker))
            {
                AddBattleEvent("attack_not_ready", attacker.OwnerSeatIndex, attacker.RuntimeId, null, null, null, null, 0, null, null, null, null, null, null, $"{attacker.Definition.DisplayName} is not ready to attack.");
                continue;
            }

            if (!CanAttackFromCurrentSlot(attacker))
            {
                AddBattleEvent("attack_position_blocked", attacker.OwnerSeatIndex, attacker.RuntimeId, null, null, null, null, 0, null, null, null, null, null, null, $"{attacker.Definition.DisplayName} cannot attack from {attacker.Slot}.");
                continue;
            }

            ResolveTriggeredAbilities(sourceSeatIndex, attacker, TriggerKind.OnBattlePhase);
            if (attacker.IsDead || DuelEnded)
            {
                continue;
            }

            var attacks = HasAbility(attacker, "enrage") ? 2 : 1;
            for (var attackIndex = 0; attackIndex < attacks; attackIndex++)
            {
                ExecuteNormalAttack(attacker, 1 - sourceSeatIndex);
                if (attacker.IsDead || DuelEnded)
                {
                    break;
                }
            }

            if (attacks > 1 && !attacker.IsDead)
            {
                AddOrRefreshStatus(attacker, StatusEffectKind.EnrageCooldown, 0, 1, attacker.RuntimeId, "enrage");
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
            if (source.ConsumedOneShotAbilityIds.Contains(ability.AbilityId))
            {
                continue;
            }

            AddBattleEvent("skill_begin", source.OwnerSeatIndex, source.RuntimeId, null, null, ability.AbilityId, null, 0, null, null, null, null, null, null, $"{source.Definition.DisplayName} used {ability.DisplayName}.");
            if (IsNormalAttackModifierAbility(ability))
            {
                continue;
            }

            var targets = SelectTargets(sourceSeatIndex, 1 - sourceSeatIndex, source.RuntimeId, ability.Selector);
            if (targets.Count == 0)
            {
                ApplyEffects(sourceSeatIndex, sourceSeatIndex, source.RuntimeId, null, ability, ability.Effects);
                continue;
            }

            foreach (var target in targets)
            {
                ApplyEffects(sourceSeatIndex, target.OwnerSeatIndex, source.RuntimeId, target, ability, ability.Effects);
            }
        }
    }

    private void ApplyEffects(int sourceSeatIndex, int targetSeatIndex, string sourceRuntimeId, RuntimeBoardCard? target, ServerAbilityDefinition ability, IReadOnlyList<ServerEffectDefinition> effects)
    {
        foreach (var effect in effects)
        {
            var resolvedTarget = target;
            if (effect.TargetSelectorOverride.HasValue)
            {
                resolvedTarget = SelectTargets(sourceSeatIndex, 1 - sourceSeatIndex, sourceRuntimeId, effect.TargetSelectorOverride.Value).FirstOrDefault();
                targetSeatIndex = resolvedTarget?.OwnerSeatIndex ?? targetSeatIndex;
            }

            switch (effect.Kind)
            {
                case EffectKind.Damage when resolvedTarget != null:
                    DealDamage(sourceRuntimeId, resolvedTarget.RuntimeId, effect.Amount, ignoreArmor: false, ability.AbilityId, effect.Kind);
                    break;
                case EffectKind.Heal when resolvedTarget != null:
                    HealCard(resolvedTarget, effect.Amount, capAtMaxHealth: !HasAbility(FindCard(sourceRuntimeId), "leech"), ability.AbilityId, effect.Kind);
                    break;
                case EffectKind.GainArmor when resolvedTarget != null:
                    var armorBefore = resolvedTarget.Armor;
                    resolvedTarget.Armor += effect.Amount;
                    AddBattleEvent("armor_gain", sourceSeatIndex, sourceRuntimeId, resolvedTarget.OwnerSeatIndex, resolvedTarget.RuntimeId, ability.AbilityId, effect.Kind, effect.Amount, resolvedTarget.CurrentHealth, resolvedTarget.CurrentHealth, armorBefore, resolvedTarget.Armor, null, null, $"{resolvedTarget.Definition.DisplayName} gained {effect.Amount} armor.");
                    break;
                case EffectKind.BuffAttack when resolvedTarget != null:
                    resolvedTarget.Attack = Math.Max(0, resolvedTarget.Attack + effect.Amount);
                    AddBattleEvent("attack_buff", sourceSeatIndex, sourceRuntimeId, resolvedTarget.OwnerSeatIndex, resolvedTarget.RuntimeId, ability.AbilityId, effect.Kind, effect.Amount, null, null, null, null, null, null, $"{resolvedTarget.Definition.DisplayName} attack changed by {effect.Amount}.");
                    break;
                case EffectKind.HitHero:
                    DamageHero(targetSeatIndex, effect.Amount, sourceRuntimeId, ability.AbilityId, effect.Kind);
                    break;
                case EffectKind.AddShield when resolvedTarget != null:
                    AddOrRefreshStatus(resolvedTarget, StatusEffectKind.Shield, effect.Amount <= 0 ? 1 : effect.Amount, effect.DurationTurns ?? 99, sourceRuntimeId, ability.AbilityId);
                    break;
                case EffectKind.ApplyPoison when resolvedTarget != null:
                    AddOrRefreshStatus(resolvedTarget, StatusEffectKind.Poison, effect.Amount, effect.DurationTurns ?? effect.SecondaryAmount ?? 1, sourceRuntimeId, ability.AbilityId);
                    break;
                case EffectKind.ApplyStun when resolvedTarget != null:
                    AddOrRefreshStatus(resolvedTarget, StatusEffectKind.Stun, effect.Amount, effect.DurationTurns ?? 1, sourceRuntimeId, ability.AbilityId);
                    if (ability.AbilityId.Equals("stun", StringComparison.OrdinalIgnoreCase))
                    {
                        FindCard(sourceRuntimeId)?.ConsumedOneShotAbilityIds.Add(ability.AbilityId);
                    }
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
                var taunt = _seats[enemySeatIndex].Board.Values.FirstOrDefault(card => card != null && HasAbility(card, "taunt"));
                if (taunt != null)
                {
                    targets.Add(taunt);
                    break;
                }

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
            case TargetSelectorKind.AllyFront:
                if (_seats[sourceSeatIndex].Board[BoardSlot.Front] != null) targets.Add(_seats[sourceSeatIndex].Board[BoardSlot.Front]!);
                break;
            case TargetSelectorKind.AllyBackLeft:
                if (_seats[sourceSeatIndex].Board[BoardSlot.BackLeft] != null) targets.Add(_seats[sourceSeatIndex].Board[BoardSlot.BackLeft]!);
                break;
            case TargetSelectorKind.AllyBackRight:
                if (_seats[sourceSeatIndex].Board[BoardSlot.BackRight] != null) targets.Add(_seats[sourceSeatIndex].Board[BoardSlot.BackRight]!);
                break;
            case TargetSelectorKind.SourceOpponent:
                var source = FindCard(sourceRuntimeId);
                if (source != null)
                {
                    var mirroredSlot = source.Slot;
                    if (_seats[enemySeatIndex].Board[mirroredSlot] != null) targets.Add(_seats[enemySeatIndex].Board[mirroredSlot]!);
                    else if (_seats[enemySeatIndex].Board[BoardSlot.Front] != null) targets.Add(_seats[enemySeatIndex].Board[BoardSlot.Front]!);
                }
                break;
        }

        return targets;
    }

    private RuntimeBoardCard? FindCard(string runtimeId)
    {
        return _seats.SelectMany(seat => seat.Board.Values).FirstOrDefault(card => card?.RuntimeId == runtimeId);
    }

    private int? FindOwnerSeatIndex(string? runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
        {
            return null;
        }

        return FindCard(runtimeId)?.OwnerSeatIndex;
    }

    private bool HasAbility(RuntimeBoardCard? card, string abilityId)
    {
        return card?.Definition.Abilities.Any(ability => ability.AbilityId.Equals(abilityId, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static bool IsNormalAttackModifierAbility(ServerAbilityDefinition ability)
    {
        return ability.MetadataJson?.Contains("normalAttackModifier", StringComparison.OrdinalIgnoreCase) == true
            || ability.MetadataJson?.Contains("targetingModifier", StringComparison.OrdinalIgnoreCase) == true
            || ability.AbilityId.Equals("fly", StringComparison.OrdinalIgnoreCase)
            || ability.AbilityId.Equals("trample", StringComparison.OrdinalIgnoreCase)
            || ability.AbilityId.Equals("poison", StringComparison.OrdinalIgnoreCase)
            || ability.AbilityId.Equals("stun", StringComparison.OrdinalIgnoreCase)
            || ability.AbilityId.Equals("leech", StringComparison.OrdinalIgnoreCase)
            || ability.AbilityId.Equals("enrage", StringComparison.OrdinalIgnoreCase)
            || ability.AbilityId.Equals("taunt", StringComparison.OrdinalIgnoreCase)
            || ability.AbilityId.Equals("haste", StringComparison.OrdinalIgnoreCase);
    }

    private bool CanAttackThisTurn(RuntimeBoardCard attacker)
    {
        if (HasAbility(attacker, "haste") || attacker.Definition.Abilities.Any(ability => ability.Effects.Any(effect => effect.Kind == EffectKind.Haste)))
        {
            return true;
        }

        return TurnNumber - attacker.SummonedTurnNumber >= attacker.Definition.TurnsUntilCanAttack;
    }

    private static bool CanAttackFromCurrentSlot(RuntimeBoardCard attacker)
    {
        if (attacker.Definition.CardType != (int)CardType.Unit || !attacker.Definition.UnitType.HasValue)
        {
            return false;
        }

        return (UnitType)attacker.Definition.UnitType.Value switch
        {
            UnitType.Melee => attacker.Slot == BoardSlot.Front,
            UnitType.Ranged => attacker.Slot is BoardSlot.BackLeft or BoardSlot.BackRight,
            UnitType.Magic => attacker.Slot is BoardSlot.BackLeft or BoardSlot.BackRight,
            _ => false
        };
    }

    private void ExecuteNormalAttack(RuntimeBoardCard attacker, int enemySeatIndex)
    {
        var target = SelectNormalAttackTarget(attacker, enemySeatIndex);
        if (target == null)
        {
            AddBattleEvent("card_attack", attacker.OwnerSeatIndex, attacker.RuntimeId, enemySeatIndex, null, null, null, attacker.Attack, null, null, null, null, null, null, $"{attacker.Definition.DisplayName} attacked the enemy hero.");
            DamageHero(enemySeatIndex, attacker.Attack, attacker.RuntimeId, null, null);
            return;
        }

        if (HasAbility(attacker, "fly") && !HasAbility(target, "fly"))
        {
            AddBattleEvent("fly_bypass", attacker.OwnerSeatIndex, attacker.RuntimeId, target.OwnerSeatIndex, target.RuntimeId, "fly", null, 0, null, null, null, null, null, null, $"{attacker.Definition.DisplayName} bypassed {target.Definition.DisplayName} with Fly.");
            DamageHero(enemySeatIndex, attacker.Attack, attacker.RuntimeId, "fly", null);
            return;
        }

        var ignoreArmor = HasAbility(attacker, "trample");
        var targetAttack = Math.Max(0, target.Attack);
        var targetCounterIgnoresArmor = HasAbility(target, "trample");
        AddBattleEvent("card_attack", attacker.OwnerSeatIndex, attacker.RuntimeId, target.OwnerSeatIndex, target.RuntimeId, null, null, attacker.Attack, null, null, null, null, null, null, $"{attacker.Definition.DisplayName} attacked {target.Definition.DisplayName}.");
        var result = DealDamage(attacker.RuntimeId, target.RuntimeId, attacker.Attack, ignoreArmor, null, null, attacker.OwnerSeatIndex);
        ApplyCombatContactEffects(attacker, target, result);

        if (DuelEnded)
        {
            return;
        }

        if (targetAttack <= 0)
        {
            return;
        }

        AddBattleEvent("card_counterattack", target.OwnerSeatIndex, target.RuntimeId, attacker.OwnerSeatIndex, attacker.RuntimeId, null, null, targetAttack, null, null, null, null, null, null, $"{target.Definition.DisplayName} struck back at {attacker.Definition.DisplayName}.");
        var counterResult = DealDamage(target.RuntimeId, attacker.RuntimeId, targetAttack, targetCounterIgnoresArmor, null, null, target.OwnerSeatIndex);
        ApplyCombatContactEffects(target, attacker, counterResult);
    }

    private void ApplyCombatContactEffects(RuntimeBoardCard source, RuntimeBoardCard target, DamageResult result)
    {
        if (result.HealthDamage <= 0 || source.IsDead)
        {
            return;
        }

        if (HasAbility(source, "leech"))
        {
            HealCard(source, result.HealthDamage, capAtMaxHealth: false, "leech", EffectKind.Heal);
        }

        if (!target.IsDead && HasAbility(source, "poison"))
        {
            var poisonEffect = source.Definition.Abilities
                .Where(ability => ability.AbilityId.Equals("poison", StringComparison.OrdinalIgnoreCase))
                .SelectMany(ability => ability.Effects)
                .FirstOrDefault(effect => effect.Kind == EffectKind.ApplyPoison);
            AddOrRefreshStatus(target, StatusEffectKind.Poison, poisonEffect?.Amount ?? 1, poisonEffect?.DurationTurns ?? poisonEffect?.SecondaryAmount ?? 2, source.RuntimeId, "poison");
        }

        if (!target.IsDead && HasAbility(source, "stun") && source.ConsumedOneShotAbilityIds.Add("stun"))
        {
            AddOrRefreshStatus(target, StatusEffectKind.Stun, 0, 1, source.RuntimeId, "stun");
        }
    }

    private RuntimeBoardCard? SelectNormalAttackTarget(RuntimeBoardCard attacker, int enemySeatIndex)
    {
        var taunt = _seats[enemySeatIndex].Board.Values
            .Where(card => card != null && !card.IsDead && HasAbility(card, "taunt"))
            .OrderBy(card => SlotPriority(card!.Slot))
            .FirstOrDefault();
        if (taunt != null)
        {
            return taunt;
        }

        if (attacker.Definition.UnitType == (int)UnitType.Melee)
        {
            return _seats[enemySeatIndex].Board[BoardSlot.Front];
        }

        if (attacker.Definition.UnitType == (int)UnitType.Ranged)
        {
            var straightSlot = attacker.Slot;
            return _seats[enemySeatIndex].Board[straightSlot]
                ?? _seats[enemySeatIndex].Board[BoardSlot.Front];
        }

        if (attacker.Definition.UnitType == (int)UnitType.Magic)
        {
            var diagonalSlot = attacker.Slot == BoardSlot.BackLeft
                ? BoardSlot.BackRight
                : BoardSlot.BackLeft;
            return _seats[enemySeatIndex].Board[diagonalSlot]
                ?? _seats[enemySeatIndex].Board[BoardSlot.Front];
        }

        return null;
    }

    private static int SlotPriority(BoardSlot slot) =>
        slot switch
        {
            BoardSlot.Front => 0,
            BoardSlot.BackLeft => 1,
            BoardSlot.BackRight => 2,
            _ => 99
        };

    private void ApplyBattleStartStatuses(RuntimeBoardCard target)
    {
        foreach (var status in target.StatusEffects.Where(status => status.Kind == StatusEffectKind.Poison).ToArray())
        {
            DealDamage(status.SourceRuntimeId ?? target.RuntimeId, target.RuntimeId, status.Amount, ignoreArmor: true, status.AbilityId, EffectKind.ApplyPoison);
            status.RemainingTurns -= 1;
            if (status.RemainingTurns <= 0)
            {
                target.StatusEffects.Remove(status);
                AddBattleEvent("status_expired", null, status.SourceRuntimeId, target.OwnerSeatIndex, target.RuntimeId, status.AbilityId, null, 0, null, null, null, null, status.Kind, 0, $"{status.Kind} expired on {target.Definition.DisplayName}.");
            }
        }
    }

    private bool ConsumeSkipAttackStatus(RuntimeBoardCard target, StatusEffectKind kind, string eventKind)
    {
        var status = target.StatusEffects.FirstOrDefault(status => status.Kind == kind);
        if (status == null)
        {
            return false;
        }

        target.StatusEffects.Remove(status);
        AddBattleEvent(eventKind, null, status.SourceRuntimeId, target.OwnerSeatIndex, target.RuntimeId, status.AbilityId, null, 0, null, null, null, null, kind, 0, $"{target.Definition.DisplayName} skipped its attack due to {kind}.");
        return true;
    }

    private void AddOrRefreshStatus(RuntimeBoardCard target, StatusEffectKind kind, int amount, int durationTurns, string? sourceRuntimeId, string? abilityId)
    {
        var existing = target.StatusEffects.FirstOrDefault(status => status.Kind == kind && string.Equals(status.AbilityId, abilityId, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            target.StatusEffects.Add(new RuntimeStatusEffect
            {
                Kind = kind,
                Amount = amount,
                RemainingTurns = durationTurns,
                SourceRuntimeId = sourceRuntimeId,
                AbilityId = abilityId
            });
        }
        else
        {
            existing.Amount = Math.Max(existing.Amount, amount);
            existing.RemainingTurns = Math.Max(existing.RemainingTurns, durationTurns);
        }

        AddBattleEvent("status_applied", null, sourceRuntimeId, target.OwnerSeatIndex, target.RuntimeId, abilityId, null, amount, null, null, null, null, kind, durationTurns, $"{kind} applied to {target.Definition.DisplayName}.");
    }

    private void HealCard(RuntimeBoardCard target, int amount, bool capAtMaxHealth, string? abilityId, EffectKind? effectKind)
    {
        var before = target.CurrentHealth;
        target.CurrentHealth = capAtMaxHealth
            ? Math.Min(target.MaxHealth, target.CurrentHealth + amount)
            : target.CurrentHealth + amount;
        _logs.Add($"{target.Definition.DisplayName} healed {target.CurrentHealth - before}.");
        AddBattleEvent("heal", target.OwnerSeatIndex, target.RuntimeId, target.OwnerSeatIndex, target.RuntimeId, abilityId, effectKind, target.CurrentHealth - before, before, target.CurrentHealth, target.Armor, target.Armor, null, null, $"{target.Definition.DisplayName} healed {target.CurrentHealth - before}.");
    }

    private DamageResult DealDamage(string sourceRuntimeId, string targetRuntimeId, int amount, bool ignoreArmor, string? abilityId, EffectKind? effectKind, int? sourceSeatIndexOverride = null)
    {
        var target = FindCard(targetRuntimeId);
        if (target == null || amount <= 0)
        {
            return new DamageResult(0, 0);
        }

        var hpBefore = target.CurrentHealth;
        var armorBefore = target.Armor;
        var shield = target.StatusEffects.FirstOrDefault(status => status.Kind == StatusEffectKind.Shield);
        if (shield != null)
        {
            shield.Amount -= 1;
            if (shield.Amount <= 0)
            {
                target.StatusEffects.Remove(shield);
            }

            AddBattleEvent("shield_block", sourceSeatIndexOverride ?? FindOwnerSeatIndex(sourceRuntimeId), sourceRuntimeId, target.OwnerSeatIndex, target.RuntimeId, shield.AbilityId ?? abilityId, effectKind, amount, hpBefore, target.CurrentHealth, armorBefore, target.Armor, StatusEffectKind.Shield, shield.RemainingTurns, $"{target.Definition.DisplayName} shield blocked {amount} damage.");
            return new DamageResult(0, 0);
        }

        var pending = amount;
        var armorDamage = 0;
        if (!ignoreArmor && target.Armor > 0)
        {
            var absorbed = Math.Min(target.Armor, pending);
            target.Armor -= absorbed;
            pending -= absorbed;
            armorDamage = absorbed;
        }

        var healthDamage = 0;
        if (pending > 0)
        {
            target.CurrentHealth -= pending;
            healthDamage = pending;
        }

        _logs.Add($"{sourceRuntimeId} hit {target.Definition.DisplayName} for {amount}.");
        AddBattleEvent("card_damage", sourceSeatIndexOverride ?? FindOwnerSeatIndex(sourceRuntimeId), sourceRuntimeId, target.OwnerSeatIndex, target.RuntimeId, abilityId, effectKind, amount, hpBefore, target.CurrentHealth, armorBefore, target.Armor, null, null, $"{target.Definition.DisplayName} took {amount} damage.");
        CleanupDeaths();
        return new DamageResult(healthDamage, armorDamage);
    }

    private void DamageHero(int targetSeatIndex, int amount, string? sourceRuntimeId, string? abilityId, EffectKind? effectKind)
    {
        var seat = _seats[targetSeatIndex];
        var hpBefore = seat.HeroHealth;
        seat.HeroHealth = Math.Max(0, seat.HeroHealth - amount);
        AddBattleEvent("hero_damage", FindOwnerSeatIndex(sourceRuntimeId), sourceRuntimeId, targetSeatIndex, null, abilityId, effectKind, amount, hpBefore, seat.HeroHealth, null, null, null, null, $"Seat {targetSeatIndex + 1} hero took {amount} damage.");
        if (seat.HeroHealth <= 0)
        {
            WinnerSeatIndex = 1 - targetSeatIndex;
            DuelEnded = true;
            Phase = MatchPhase.Completed;
        }
    }

    private void AddBattleEvent(
        string kind,
        int? sourceSeatIndex,
        string? sourceRuntimeId,
        int? targetSeatIndex,
        string? targetRuntimeId,
        string? abilityId,
        EffectKind? effectKind,
        int amount,
        int? hpBefore,
        int? hpAfter,
        int? armorBefore,
        int? armorAfter,
        StatusEffectKind? statusKind,
        int? durationTurns,
        string message)
    {
        _battleEventSequence += 1;
        _battleEvents.Add(new BattleEventDto(
            $"evt-{_battleEventSequence:D6}",
            _battleEventSequence,
            kind,
            sourceSeatIndex,
            sourceRuntimeId,
            targetSeatIndex,
            targetRuntimeId,
            abilityId,
            effectKind,
            amount,
            hpBefore,
            hpAfter,
            armorBefore,
            armorAfter,
            statusKind,
            durationTurns,
            message));
    }

    private readonly record struct DamageResult(int HealthDamage, int ArmorDamage);

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
                    AddBattleEvent("death", null, null, seat.SeatIndex, occupant.RuntimeId, null, null, 0, occupant.CurrentHealth, occupant.CurrentHealth, occupant.Armor, occupant.Armor, null, null, $"{occupant.Definition.DisplayName} died.");
                    seat.Board[slot] = null;
                }
            }

            CompactBoard(seat);
        }
    }

    private static void CompactBoard(RuntimeSeatState seat)
    {
        if (seat.Board[BoardSlot.Front] == null)
        {
            MoveCard(seat, BoardSlot.BackLeft, BoardSlot.Front);
            MoveCard(seat, BoardSlot.BackRight, BoardSlot.BackLeft);
        }
        else if (seat.Board[BoardSlot.BackLeft] == null)
        {
            MoveCard(seat, BoardSlot.BackRight, BoardSlot.BackLeft);
        }
    }

    private RuntimeSeatState GetSeat(string playerId)
    {
        return _seats.FirstOrDefault(x => x.PlayerId == playerId)
            ?? throw GameActionException.PlayerNotInMatch();
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
            waitingForOpponent ? "waiting_for_opponent" : "joined",
            _rules.RulesetId,
            _rulesDto);
    }

    private string BuildStatus(int localSeatIndex)
    {
        var remote = localSeatIndex is 0 or 1 ? _seats[1 - localSeatIndex] : null;
        return Phase switch
        {
            MatchPhase.WaitingForPlayers => "Waiting for second player.",
            MatchPhase.WaitingForReady => $"Ready up. Opponent ready: {(remote?.Ready == true ? "yes" : "no")}.",
            MatchPhase.InProgress when remote is { Connected: false } => $"Opponent disconnected. Rejoin window: {Math.Max(0d, (_disconnectGrace - (DateTimeOffset.UtcNow - remote.DisconnectedAt!.Value)).TotalSeconds):0}s.",
            MatchPhase.InProgress when localSeatIndex == ActiveSeatIndex => "Your turn.",
            MatchPhase.InProgress => "Opponent's turn.",
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

    private void GrantTurnMana(RuntimeSeatState seat)
    {
        var seatRules = _rules.GetSeatRules(seat.SeatIndex);
        seat.MaxMana = Math.Min(seatRules.MaxMana, seat.MaxMana + seatRules.ManaGrantedPerTurn);
        seat.Mana = seat.MaxMana;
    }
}
