namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class DeckCard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DeckId { get; set; } = string.Empty;
    public string CardDefinitionId { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// References the specific player-owned card instance used in this deck slot.
    /// Null for legacy deck entries created before player card ownership was enforced.
    /// </summary>
    public string? PlayerCardId { get; set; }

    public PlayerDeck Deck { get; set; } = null!;
    public CardDefinition CardDefinition { get; set; } = null!;
    public PlayerCard? PlayerCard { get; set; }
}
