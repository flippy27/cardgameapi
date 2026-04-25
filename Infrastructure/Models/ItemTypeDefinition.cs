namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class ItemTypeDefinition
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "crafting", "currency", "material"
    public int MaxStack { get; set; } = -1; // -1 = unlimited
    public bool IsActive { get; set; } = true;
    public string? IconAssetRef { get; set; }
    public string MetadataJson { get; set; } = "{}";

    public ICollection<PlayerItem> PlayerItems { get; set; } = new List<PlayerItem>();
    public ICollection<CardCraftingRequirement> CraftingRequirements { get; set; } = new List<CardCraftingRequirement>();
}
