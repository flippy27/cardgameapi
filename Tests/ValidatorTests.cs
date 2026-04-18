using Xunit;
using CardDuel.ServerApi.Contracts;

namespace CardDuel.ServerApi.Tests;

public class ValidatorTests
{
    [Fact]
    public void PlayCardRequestValidator_Valid()
    {
        var validator = new PlayCardRequestValidator();
        var request = new PlayCardRequest("match1", "player1", "card_key", 0);

        var result = validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void PlayCardRequestValidator_InvalidSlot()
    {
        var validator = new PlayCardRequestValidator();
        var request = new PlayCardRequest("match1", "player1", "card_key", 5);

        var result = validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains("SlotIndex", result.ToString());
    }

    [Fact]
    public void DeckUpsertRequestValidator_Valid()
    {
        var validator = new DeckUpsertRequestValidator();
        var cardIds = Enumerable.Range(0, 25).Select(i => $"card{i}").ToList();
        var request = new DeckUpsertRequest("player1", "deck1", "My Deck", cardIds);

        var result = validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void DeckUpsertRequestValidator_TooFewCards()
    {
        var validator = new DeckUpsertRequestValidator();
        var cardIds = new[] { "card1", "card2" }.ToList();
        var request = new DeckUpsertRequest("player1", "deck1", "My Deck", cardIds);

        var result = validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains("20-30", result.ToString());
    }

    [Fact]
    public void DeckUpsertRequestValidator_TooManyCopies()
    {
        var validator = new DeckUpsertRequestValidator();
        var cardIds = Enumerable.Range(0, 20).Select(i => "card1").ToList();
        cardIds.AddRange(Enumerable.Range(0, 5).Select(i => $"card{i + 2}"));
        var request = new DeckUpsertRequest("player1", "deck1", "My Deck", cardIds);

        var result = validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains("3 copies", result.ToString());
    }
}
