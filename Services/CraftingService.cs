using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace CardDuel.ServerApi.Services;

public interface ICraftingService
{
    Task<CardCraftingInfoDto?> GetCraftingInfoAsync(string cardId);
    Task<IReadOnlyList<CardCraftingInfoDto>> GetAllCraftableCardsAsync();
    Task<CraftCardResponse> CraftCardAsync(string userId, string cardId);
    Task<IReadOnlyList<CraftingRequirementDto>> SetCraftingRequirementsAsync(string cardId, SetCraftingRequirementsRequest request);
    Task<bool> DeleteCraftingRequirementAsync(string cardId, string requirementId);
}

public sealed class CraftingService(
    AppDbContext db,
    IPlayerInventoryService inventoryService,
    IPlayerCardService playerCardService) : ICraftingService
{
    public async Task<CardCraftingInfoDto?> GetCraftingInfoAsync(string cardId)
    {
        var card = await db.Cards
            .Include(c => c.CraftingRequirements)
            .ThenInclude(r => r.ItemType)
            .FirstOrDefaultAsync(c => c.CardId == cardId);

        return card == null ? null : MapToCraftingInfo(card);
    }

    public async Task<IReadOnlyList<CardCraftingInfoDto>> GetAllCraftableCardsAsync()
    {
        var cards = await db.Cards
            .Include(c => c.CraftingRequirements)
            .ThenInclude(r => r.ItemType)
            .Where(c => c.CraftingRequirements.Any())
            .OrderBy(c => c.CardId)
            .ToListAsync();

        return cards.Select(MapToCraftingInfo).ToArray();
    }

    public async Task<CraftCardResponse> CraftCardAsync(string userId, string cardId)
    {
        var card = await db.Cards
            .Include(c => c.CraftingRequirements)
            .ThenInclude(r => r.ItemType)
            .FirstOrDefaultAsync(c => c.CardId == cardId);

        if (card == null)
            return new CraftCardResponse(false, $"Card '{cardId}' not found.");

        if (!card.CraftingRequirements.Any())
            return new CraftCardResponse(false, $"Card '{cardId}' has no crafting requirements defined.");

        // Validate sufficient items for all requirements
        foreach (var req in card.CraftingRequirements)
        {
            var sufficient = await inventoryService.HasSufficientItemsAsync(userId, req.ItemType.Key, req.QuantityRequired);
            if (!sufficient)
            {
                var balance = await inventoryService.GetItemBalanceAsync(userId, req.ItemType.Key);
                var have = balance?.Quantity ?? 0;
                return new CraftCardResponse(
                    false,
                    $"Insufficient '{req.ItemType.DisplayName}': need {req.QuantityRequired}, have {have}.");
            }
        }

        // Deduct all items
        var updatedItems = new List<PlayerItemDto>();
        foreach (var req in card.CraftingRequirements)
        {
            var updated = await inventoryService.ConsumeItemsAsync(userId, req.ItemType.Key, req.QuantityRequired);
            updatedItems.Add(updated);
        }

        // Grant the card
        var playerCard = await playerCardService.GrantCardAsync(userId, cardId, "crafted");

        return new CraftCardResponse(
            true,
            $"Card '{card.DisplayName}' crafted successfully.",
            playerCard,
            updatedItems);
    }

    public async Task<IReadOnlyList<CraftingRequirementDto>> SetCraftingRequirementsAsync(
        string cardId, SetCraftingRequirementsRequest request)
    {
        var card = await db.Cards
            .Include(c => c.CraftingRequirements)
            .FirstOrDefaultAsync(c => c.CardId == cardId)
            ?? throw new KeyNotFoundException($"Card '{cardId}' not found.");

        // Validate all item type keys exist
        var keys = request.Requirements.Select(r => r.ItemTypeKey).Distinct().ToList();
        var itemTypes = await db.ItemTypeDefinitions
            .Where(x => keys.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, StringComparer.OrdinalIgnoreCase);

        var missing = keys.FirstOrDefault(k => !itemTypes.ContainsKey(k));
        if (missing != null)
            throw new KeyNotFoundException($"Item type '{missing}' not found.");

        // Replace all requirements
        db.CraftingRequirements.RemoveRange(card.CraftingRequirements);

        var newRequirements = request.Requirements
            .DistinctBy(r => r.ItemTypeKey, StringComparer.OrdinalIgnoreCase)
            .Select(r => new CardCraftingRequirement
            {
                CardDefinitionId = card.Id,
                ItemTypeId = itemTypes[r.ItemTypeKey].Id,
                QuantityRequired = r.QuantityRequired
            })
            .ToList();

        db.CraftingRequirements.AddRange(newRequirements);
        await db.SaveChangesAsync();

        // Re-query with nav loaded
        var saved = await db.CraftingRequirements
            .Include(r => r.ItemType)
            .Where(r => r.CardDefinitionId == card.Id)
            .ToListAsync();

        return saved.Select(MapRequirementToDto).ToArray();
    }

    public async Task<bool> DeleteCraftingRequirementAsync(string cardId, string requirementId)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.CardId == cardId);
        if (card == null) return false;

        var req = await db.CraftingRequirements
            .FirstOrDefaultAsync(r => r.Id == requirementId && r.CardDefinitionId == card.Id);
        if (req == null) return false;

        db.CraftingRequirements.Remove(req);
        await db.SaveChangesAsync();
        return true;
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static CardCraftingInfoDto MapToCraftingInfo(CardDefinition card) => new(
        card.CardId,
        card.DisplayName,
        card.CardRarity,
        card.CraftingRequirements.Any(),
        card.CraftingRequirements
            .OrderBy(r => r.ItemTypeId)
            .Select(MapRequirementToDto)
            .ToArray());

    private static CraftingRequirementDto MapRequirementToDto(CardCraftingRequirement r) => new(
        r.Id,
        r.CardDefinitionId,
        r.ItemTypeId,
        r.ItemType.Key,
        r.ItemType.DisplayName,
        r.QuantityRequired);
}
