namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class TriggerKindDefinition
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? IconAssetRef { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
