namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class StatusEffectKindDefinition
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? IconAssetRef { get; set; }
    public string? VfxCueId { get; set; }
    public string? UiColorHex { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
