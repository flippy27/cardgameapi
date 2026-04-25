namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class PlayerCard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string CardDefinitionId { get; set; } = string.Empty;
    public string AcquiredFrom { get; set; } = string.Empty; // "match_reward", "crafted", "admin_grant", "starter_pack"
    public DateTimeOffset AcquiredAt { get; set; } = DateTimeOffset.UtcNow;

    public UserAccount User { get; set; } = null!;
    public CardDefinition CardDefinition { get; set; } = null!;
    public ICollection<PlayerCardUpgrade> Upgrades { get; set; } = new List<PlayerCardUpgrade>();
}
