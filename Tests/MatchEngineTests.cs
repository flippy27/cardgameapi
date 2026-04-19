using Xunit;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Game;

namespace CardDuel.ServerApi.Tests;

public class MatchEngineTests
{
    private ServerCardDefinition CreateCard(string id, string name, int mana = 2, int attack = 2, int health = 2)
    {
        return new ServerCardDefinition(
            id, name, "", mana, attack, health, 0, 0, 0, 0, null, AllowedRow.Flexible, TargetSelectorKind.FrontlineFirst, 1, Array.Empty<ServerAbilityDefinition>());
    }

    [Fact]
    public void ReserveSeat_FilledThenTransitionsToWaitingForReady()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20));
        var cards = new[] { CreateCard("card1", "Card 1"), CreateCard("card2", "Card 2") };

        var res = engine.ReserveSeat("player1", "deck1", cards);
        Assert.Equal(MatchPhase.WaitingForPlayers, engine.Phase);

        engine.ReserveSeat("player2", "deck2", cards);
        Assert.Equal(MatchPhase.WaitingForReady, engine.Phase);
    }

    [Fact]
    public void SetReady_StartsMatchWhenBothReady()
    {
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20));
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
        var engine = new MatchEngine("match1", "ABC123", QueueMode.Casual, TimeSpan.FromSeconds(20));
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
}
