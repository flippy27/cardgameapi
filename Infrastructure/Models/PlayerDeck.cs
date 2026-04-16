namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class PlayerDeck
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string DeckId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> CardIds { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
