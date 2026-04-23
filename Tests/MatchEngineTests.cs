using Xunit;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Game;

namespace CardDuel.ServerApi.Tests;

public class MatchEngineTests
{
    private static GameRules CreateRules(
        ManaGrantTiming manaGrantTiming = ManaGrantTiming.StartOfTurn,
        IReadOnlyList<GameRulesSeatOverride>? seatOverrides = null)
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
            InitialDrawCount: 4,
            CardsDrawnOnTurnStart: 1,
            StartingSeatIndex: 0,
            SeatOverrides: seatOverrides ?? Array.Empty<GameRulesSeatOverride>());
    }

    private ServerCardDefinition CreateCard(string id, string name, int mana = 2, int attack = 2, int health = 2)
    {
        return new ServerCardDefinition(
            id, name, "", mana, attack, health, 0, 0, 0, 0, null, AllowedRow.Flexible, TargetSelectorKind.FrontlineFirst, 1, Array.Empty<ServerAbilityDefinition>());
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
}
