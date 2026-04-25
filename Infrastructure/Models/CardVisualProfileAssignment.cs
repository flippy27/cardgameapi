namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class CardVisualProfileAssignment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CardDefinitionId { get; set; } = string.Empty;
    public CardDefinition CardDefinition { get; set; } = null!;
    public string TemplateId { get; set; } = string.Empty;
    public CardVisualProfileTemplate Template { get; set; } = null!;
    public bool IsDefault { get; set; }
    public string? OverrideDisplayName { get; set; }
    public string? OverrideLayersJson { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
