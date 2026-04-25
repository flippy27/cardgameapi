namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class CardVisualProfileTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProfileKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string LayersJson { get; set; } = "[]";
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
