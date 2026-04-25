namespace CardDuel.ServerApi.Infrastructure.Models;

/// <summary>
/// Crafting requirement for a base card definition.
/// One row per item type needed. A card can have N requirements (N rows).
/// </summary>
public sealed class CardCraftingRequirement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CardDefinitionId { get; set; } = string.Empty;
    public int ItemTypeId { get; set; }
    public int QuantityRequired { get; set; }

    public CardDefinition CardDefinition { get; set; } = null!;
    public ItemTypeDefinition ItemType { get; set; } = null!;
}
