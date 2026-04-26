using Xunit;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Game;

namespace CardDuel.ServerApi.Tests;

public class MatchEngineTests
{
    private static GameRules CreateRules(
        ManaGrantTiming manaGrantTiming = ManaGrantTiming.StartOfTurn,
        IReadOnlyList<GameRulesSeatOverride>? seatOverrides = null,
        int initialDrawCount = 4)
    {
        return new GameRules(
            RulesetId: "rules-default",
            RulesetKey: "default",
            DisplayName: "Default Rules",
            Description: null,
            IsActive: true,
            IsDefault: true,
            StartingHeroHealth: 20,
            MaxHeroHealth: 20,
            StartingMana: 1,
            MaxMana: 10,
            ManaGrantedPerTurn: 1,
            ManaGrantTiming: manaGrantTiming,
            InitialDrawCount: initialDrawCount,
            CardsDrawnOnTurnStart: 1,
            StartingSeatIndex: 0,
            SeatOverrides: seatOverrides ?? Array.Empty<GameRulesSeatOverride>());
    }

    private ServerCardDefinition CreateCard(string id, string name, int mana = 2, int attack = 2, int health = 2, UnitType unitType = UnitType.Melee)
    {
        return new ServerCardDefinition(
            id, name, "", mana, attack, health, 0, 0, 0, 0, (int)unitType, AllowedRow.Flexible, TargetSelectorKind.FrontlineFirst, 1, Array.Empty<ServerAbilityDefinition>());
    }

    private ServerCardDefinition CreateCardWithAbilities(
        string id,
        string name,
        IReadOnlyList<ServerAbilityDefinition> abilities,
        int mana = 1,
        int attack = 2,
        int health = 4,
        int armor = 0)
    {
        return new ServerCardDefinition(
            id, name, "", mana, attack, health, armor, 0, 0, 0, (int)UnitType.Melee, AllowedRow.Flexible,
            TargetSelectorKind.FrontlineFirst, 1, abilities);
    }

    private static ServerAbilityDefinition Ability(
        string id,
        TriggerKind trigger,
        TargetSelectorKind selector,
        params ServerEffectDefinition[] effects)
    {
        return new ServerAbilityDefinition(
            id,
            id,
            trigger,
            selector,
            effects,
            SkillType.Offensive,
            $"anim_{id}",
            "{}",
            "{\"normalAttackModifier\":true}");
    }

    [Fact]
    public void ReserveSeat_FilledThenTransitionsToWaitingForReady()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[] { CreateCard("card1", "Card 1"), CreateCard("card2", "Card 2") };

        var res = engine.ReserveSeat("player1", "deck1", cards);
        Assert.Equal(MatchPhase.WaitingForPlayers, engine.Phase);

        engine.ReserveSeat("player2", "deck2", cards);
        Assert.Equal(MatchPhase.WaitingForReady, engine.Phase);
    }

    [Fact]
    public void SetReady_StartsMatchWhenBothReady()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[] { CreateCard("card1", "Card 1"), CreateCard("card2", "Card 2") };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);

        engine.SetReady("player1", true);
        Assert.Equal(MatchPhase.WaitingForReady, engine.Phase);

        engine.SetReady("player2", true);
        Assert.Equal(MatchPhase.InProgress, engine.Phase);
    }

    [Fact]
    public void PlayCard_ManaCostIsDeducted()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[] { CreateCard("card1", "Card 1", mana: 1) };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        var seat = engine.Seats[0];
        Assert.Equal(1, seat.Mana); // Initial mana
        Assert.True(seat.Hand.Count > 0, "Player should have cards");

        var card = seat.Hand[0];
        engine.PlayCard("player1", card.RuntimeHandKey, BoardSlot.Front);

        // Mana reduced, card removed from hand
        Assert.Equal(0, seat.Mana);
        Assert.DoesNotContain(card, seat.Hand);
    }

    [Fact]
    public void CreateSnapshot_ExposesWhoseTurnItIsForTheLocalPlayer()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[] { CreateCard("card1", "Card 1", mana: 1) };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        var playerOneSnapshot = engine.CreateSnapshotForSeat(0);
        var playerTwoSnapshot = engine.CreateSnapshotForSeat(1);

        Assert.Equal("player1", playerOneSnapshot.ActivePlayerId);
        Assert.True(playerOneSnapshot.IsLocalPlayersTurn);
        Assert.False(playerTwoSnapshot.IsLocalPlayersTurn);
        Assert.Equal("Your turn.", playerOneSnapshot.StatusMessage);
        Assert.Equal("Opponent's turn.", playerTwoSnapshot.StatusMessage);
    }

    [Fact]
    public void EndTurn_WhenWrongPlayer_ThrowsMessageWithActivePlayer()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[] { CreateCard("card1", "Card 1", mana: 1) };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        var exception = Assert.Throws<GameActionException>(() => engine.EndTurn("player2"));
        Assert.Equal("not_your_turn", exception.Code);
        Assert.Contains("player1", exception.Message);
    }

    [Fact]
    public void CreateSnapshot_ExposesRulesMetadata()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[] { CreateCard("card1", "Card 1", mana: 1) };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        var snapshot = engine.CreateSnapshotForSeat(0);

        Assert.Equal("rules-default", snapshot.RulesetId);
        Assert.Equal("rules-default", snapshot.Rules.RulesetId);
        Assert.Equal("Default Rules", snapshot.Rules.DisplayName);
    }

    [Fact]
    public void SeatOverrides_AffectInitialSeatState()
    {
        var rules = CreateRules(seatOverrides: new[]
        {
            new GameRulesSeatOverride(
                SeatIndex: 1,
                AdditionalHeroHealth: 5,
                AdditionalMaxHeroHealth: 5,
                AdditionalStartingMana: 2,
                AdditionalMaxMana: 2,
                AdditionalManaPerTurn: 1,
                AdditionalCardsDrawnOnTurnStart: 1)
        });
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), rules);
        var cards = new[] { CreateCard("card1", "Card 1", mana: 1) };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);

        Assert.Equal(20, engine.Seats[0].HeroHealth);
        Assert.Equal(1, engine.Seats[0].Mana);
        Assert.Equal(25, engine.Seats[1].HeroHealth);
        Assert.Equal(3, engine.Seats[1].Mana);
        Assert.Equal(3, engine.Seats[1].MaxMana);
    }

    [Fact]
    public void EndTurn_WithEndOfTurnManaGrant_ChargesEndingPlayerInsteadOfNextPlayer()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules(manaGrantTiming: ManaGrantTiming.EndOfTurn));
        var cards = new[] { CreateCard("card1", "Card 1", mana: 1) };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        engine.EndTurn("player1");

        Assert.Equal(2, engine.Seats[0].Mana);
        Assert.Equal(2, engine.Seats[0].MaxMana);
        Assert.Equal(1, engine.Seats[1].Mana);
        Assert.Equal(1, engine.Seats[1].MaxMana);
        Assert.Equal(1, engine.ActiveSeatIndex);
    }

    [Fact]
    public void PlayCard_OnOccupiedFront_ShiftsExistingCardsForwardInPriorityOrder()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[]
        {
            CreateCard("card1", "Card 1", mana: 1),
            CreateCard("card2", "Card 2", mana: 1),
            CreateCard("card3", "Card 3", mana: 1)
        };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        var first = engine.Seats[0].Hand.Single(card => card.Definition.CardId == "card1");
        engine.PlayCard("player1", first.RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player1");
        engine.EndTurn("player2");

        var second = engine.Seats[0].Hand.Single(card => card.Definition.CardId == "card2");
        engine.PlayCard("player1", second.RuntimeHandKey, BoardSlot.Front);

        Assert.Equal("card2", engine.Seats[0].Board[BoardSlot.Front]!.Definition.CardId);
        Assert.Equal("card1", engine.Seats[0].Board[BoardSlot.BackLeft]!.Definition.CardId);
    }

    [Fact]
    public void PlayCard_OnOccupiedBackLeft_ShiftsExistingLeftCardToRight()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[]
        {
            CreateCard("card1", "Card 1", mana: 1),
            CreateCard("card2", "Card 2", mana: 1),
            CreateCard("card3", "Card 3", mana: 1)
        };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "card1").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player1");
        engine.EndTurn("player2");

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "card2").RuntimeHandKey, BoardSlot.BackLeft);
        engine.EndTurn("player1");
        engine.EndTurn("player2");

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "card3").RuntimeHandKey, BoardSlot.BackLeft);

        Assert.Equal("card1", engine.Seats[0].Board[BoardSlot.Front]!.Definition.CardId);
        Assert.Equal("card3", engine.Seats[0].Board[BoardSlot.BackLeft]!.Definition.CardId);
        Assert.Equal("card2", engine.Seats[0].Board[BoardSlot.BackRight]!.Definition.CardId);
    }

    [Fact]
    public void PlayCard_WhenManaIsInsufficient_ThrowsParseableGameActionException()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[] { CreateCard("card1", "Card 1", mana: 2) };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        var card = engine.Seats[0].Hand[0];
        var exception = Assert.Throws<GameActionException>(() => engine.PlayCard("player1", card.RuntimeHandKey, BoardSlot.Front));

        Assert.Equal("not_enough_mana", exception.Code);
    }

    [Fact]
    public void BattlePhase_ShieldBlocksNextDamageAndEmitsOrderedEvents()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var shield = new ServerAbilityDefinition(
            "shield",
            "Shield",
            TriggerKind.OnPlay,
            TargetSelectorKind.Self,
            new[] { new ServerEffectDefinition(EffectKind.AddShield, 1, DurationTurns: 99) },
            SkillType.Defensive);
        var cards = new[]
        {
            CreateCard("attacker", "Attacker", mana: 1, attack: 3, health: 4),
            CreateCardWithAbilities("shield_target", "Shield Target", new[] { shield }, attack: 0, health: 5)
        };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "attacker").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player1");
        engine.PlayCard("player2", engine.Seats[1].Hand.Single(card => card.Definition.CardId == "shield_target").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player2");
        engine.EndTurn("player1");

        var target = engine.Seats[1].Board[BoardSlot.Front]!;
        var events = engine.CreateSnapshotForSeat(0).BattleEvents;

        Assert.Equal(5, target.CurrentHealth);
        Assert.Contains(events, e => e.Kind == "status_applied" && e.StatusKind == StatusEffectKind.Shield);
        Assert.True(events.Single(e => e.Kind == "card_attack").Sequence < events.Single(e => e.Kind == "shield_block").Sequence);
    }

    [Fact]
    public void BattlePhase_PoisonAppliesAfterAttackAndTicksBeforeOwnersAttack()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var poison = Ability(
            "poison",
            TriggerKind.OnBattlePhase,
            TargetSelectorKind.Self,
            new ServerEffectDefinition(EffectKind.ApplyPoison, 1, DurationTurns: 2));
        var cards = new[]
        {
            CreateCardWithAbilities("poisoner", "Poisoner", new[] { poison }, attack: 1, health: 4),
            CreateCard("target", "Target", mana: 1, attack: 0, health: 5)
        };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "poisoner").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player1");
        engine.PlayCard("player2", engine.Seats[1].Hand.Single(card => card.Definition.CardId == "target").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player2");
        engine.EndTurn("player1");

        var poisoned = engine.Seats[1].Board[BoardSlot.Front]!;
        Assert.Equal(4, poisoned.CurrentHealth);
        Assert.Contains(poisoned.StatusEffects, status => status.Kind == StatusEffectKind.Poison && status.RemainingTurns == 2);

        engine.EndTurn("player2");

        Assert.True(poisoned.CurrentHealth < 4);
        Assert.Contains(engine.CreateSnapshotForSeat(0).BattleEvents, e => e.Kind == "card_damage" && e.EffectKind == EffectKind.ApplyPoison);
    }

    [Fact]
    public void BattlePhase_StunIsConsumedAndTargetSkipsNextAttack()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var stun = Ability(
            "stun",
            TriggerKind.OnBattlePhase,
            TargetSelectorKind.Self,
            new ServerEffectDefinition(EffectKind.ApplyStun, 0, DurationTurns: 1));
        var cards = new[]
        {
            CreateCardWithAbilities("stunner", "Stunner", new[] { stun }, attack: 1, health: 4),
            CreateCard("target", "Target", mana: 1, attack: 4, health: 5)
        };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "stunner").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player1");
        engine.PlayCard("player2", engine.Seats[1].Hand.Single(card => card.Definition.CardId == "target").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player2");
        engine.EndTurn("player1");
        engine.EndTurn("player2");

        var events = engine.CreateSnapshotForSeat(0).BattleEvents;
        Assert.Contains(events, e => e.Kind == "status_applied" && e.StatusKind == StatusEffectKind.Stun);
        Assert.Contains(events, e => e.Kind == "stun_skip");
    }

    [Fact]
    public void BattlePhase_NormalAttackExchangesDamageWithDefender()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[]
        {
            CreateCard("attacker", "Attacker", mana: 1, attack: 1, health: 3),
            CreateCard("defender", "Defender", mana: 1, attack: 1, health: 1)
        };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "attacker").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player1");
        engine.PlayCard("player2", engine.Seats[1].Hand.Single(card => card.Definition.CardId == "defender").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player2");
        engine.EndTurn("player1");

        Assert.Null(engine.Seats[1].Board[BoardSlot.Front]);
        Assert.Equal(2, engine.Seats[0].Board[BoardSlot.Front]!.CurrentHealth);
        Assert.Contains(engine.CreateSnapshotForSeat(0).BattleEvents, evt => evt.Kind == "card_counterattack");
    }

    [Fact]
    public void BattlePhase_EnrageAttacksTwiceThenSkipsNextAttack()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var enrage = Ability("enrage", TriggerKind.OnBattlePhase, TargetSelectorKind.Self);
        var cards = new[]
        {
            CreateCardWithAbilities("enraged", "Enraged", new[] { enrage }, attack: 1, health: 4),
            CreateCard("target", "Target", mana: 1, attack: 0, health: 8)
        };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "enraged").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player1");
        engine.PlayCard("player2", engine.Seats[1].Hand.Single(card => card.Definition.CardId == "target").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player2");
        engine.EndTurn("player1");

        var target = engine.Seats[1].Board[BoardSlot.Front]!;
        Assert.Equal(6, target.CurrentHealth);

        engine.EndTurn("player2");
        engine.EndTurn("player1");

        Assert.Contains(engine.CreateSnapshotForSeat(0).BattleEvents, e => e.Kind == "enrage_cooldown_skip");
    }

    [Fact]
    public void BattlePhase_RangedCardInFrontDoesNotAttack()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[]
        {
            CreateCard("ranged", "Ranged", mana: 1, attack: 3, health: 4, unitType: UnitType.Ranged),
            CreateCard("target", "Target", mana: 1, attack: 0, health: 5)
        };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "ranged").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player1");
        engine.PlayCard("player2", engine.Seats[1].Hand.Single(card => card.Definition.CardId == "target").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player2");
        engine.EndTurn("player1");

        Assert.Equal(5, engine.Seats[1].Board[BoardSlot.Front]!.CurrentHealth);
        Assert.Contains(engine.CreateSnapshotForSeat(0).BattleEvents, e => e.Kind == "attack_position_blocked");
    }

    [Fact]
    public void BattlePhase_RangedCardAttacksStraightLaneBeforeFront()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[]
        {
            CreateCard("p1_front", "P1 Front", mana: 1, attack: 0, health: 5),
            CreateCard("p1_ranged", "P1 Ranged", mana: 1, attack: 2, health: 4, unitType: UnitType.Ranged),
            CreateCard("p2_front", "P2 Front", mana: 1, attack: 0, health: 8),
            CreateCard("p2_left", "P2 Left", mana: 1, attack: 0, health: 8, unitType: UnitType.Ranged)
        };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "p1_front").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player1");
        engine.PlayCard("player2", engine.Seats[1].Hand.Single(card => card.Definition.CardId == "p2_front").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player2");
        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "p1_ranged").RuntimeHandKey, BoardSlot.BackLeft);
        engine.EndTurn("player1");
        engine.PlayCard("player2", engine.Seats[1].Hand.Single(card => card.Definition.CardId == "p2_left").RuntimeHandKey, BoardSlot.BackLeft);
        engine.EndTurn("player2");
        engine.EndTurn("player1");

        Assert.Equal(8, engine.Seats[1].Board[BoardSlot.Front]!.CurrentHealth);
        Assert.Equal(6, engine.Seats[1].Board[BoardSlot.BackLeft]!.CurrentHealth);
    }

    [Fact]
    public void BattlePhase_MagicCardAttacksDiagonalLaneBeforeFront()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules(initialDrawCount: 5));
        var cards = new[]
        {
            CreateCard("p1_front", "P1 Front", mana: 1, attack: 0, health: 5),
            CreateCard("p1_magic", "P1 Magic", mana: 1, attack: 2, health: 4, unitType: UnitType.Magic),
            CreateCard("p2_front", "P2 Front", mana: 1, attack: 0, health: 8),
            CreateCard("p2_left", "P2 Left", mana: 1, attack: 0, health: 8, unitType: UnitType.Ranged),
            CreateCard("p2_right", "P2 Right", mana: 1, attack: 0, health: 8, unitType: UnitType.Ranged)
        };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "p1_front").RuntimeHandKey, BoardSlot.Front);
        engine.EndTurn("player1");
        engine.PlayCard("player2", engine.Seats[1].Hand.Single(card => card.Definition.CardId == "p2_front").RuntimeHandKey, BoardSlot.Front);
        engine.PlayCard("player2", engine.Seats[1].Hand.Single(card => card.Definition.CardId == "p2_left").RuntimeHandKey, BoardSlot.BackLeft);
        engine.EndTurn("player2");
        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "p1_magic").RuntimeHandKey, BoardSlot.BackLeft);
        engine.EndTurn("player1");
        engine.PlayCard("player2", engine.Seats[1].Hand.Single(card => card.Definition.CardId == "p2_right").RuntimeHandKey, BoardSlot.BackRight);
        engine.EndTurn("player2");
        engine.EndTurn("player1");

        Assert.Equal(8, engine.Seats[1].Board[BoardSlot.Front]!.CurrentHealth);
        Assert.Contains(new[] { BoardSlot.BackLeft, BoardSlot.BackRight }, slot => engine.Seats[1].Board[slot]!.CurrentHealth == 6);
    }

    [Fact]
    public void DestroyCard_RemovesOwnedBoardCardAndCompactsBoard()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20), CreateRules());
        var cards = new[]
        {
            CreateCard("front", "Front", mana: 0, attack: 1, health: 3),
            CreateCard("back", "Back", mana: 0, attack: 1, health: 3)
        };

        engine.ReserveSeat("player1", "deck1", cards);
        engine.ReserveSeat("player2", "deck2", cards);
        engine.SetReady("player1", true);
        engine.SetReady("player2", true);

        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "front").RuntimeHandKey, BoardSlot.Front);
        engine.PlayCard("player1", engine.Seats[0].Hand.Single(card => card.Definition.CardId == "back").RuntimeHandKey, BoardSlot.BackLeft);

        var runtimeCardId = engine.Seats[0].Board[BoardSlot.Front]!.RuntimeId;
        engine.DestroyCard("player1", runtimeCardId);

        Assert.Equal("back", engine.Seats[0].Board[BoardSlot.Front]!.Definition.CardId);
        Assert.Null(engine.Seats[0].Board[BoardSlot.BackLeft]);
        Assert.Contains(engine.CreateSnapshotForSeat(0).BattleEvents, evt => evt.Kind == "card_destroyed");
    }
}
