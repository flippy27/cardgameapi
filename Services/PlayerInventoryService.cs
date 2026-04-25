using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace CardDuel.ServerApi.Services;

public interface IPlayerInventoryService
{
    Task<IReadOnlyList<ItemTypeDto>> GetItemTypesAsync();
    Task<ItemTypeDto?> GetItemTypeAsync(string key);
    Task<PlayerInventoryDto> GetInventoryAsync(string userId);
    Task<PlayerItemDto?> GetItemBalanceAsync(string userId, string itemTypeKey);
    Task<PlayerItemDto> GrantItemsAsync(string userId, string itemTypeKey, long quantity);
    Task<PlayerItemDto> ConsumeItemsAsync(string userId, string itemTypeKey, long quantity);
    Task<bool> HasSufficientItemsAsync(string userId, string itemTypeKey, long quantity);
}

public sealed class PlayerInventoryService(AppDbContext db) : IPlayerInventoryService
{
    public async Task<IReadOnlyList<ItemTypeDto>> GetItemTypesAsync()
    {
        var types = await db.ItemTypeDefinitions
            .OrderBy(x => x.Id)
            .ToListAsync();

        return types.Select(MapItemTypeToDto).ToArray();
    }

    public async Task<ItemTypeDto?> GetItemTypeAsync(string key)
    {
        var type = await db.ItemTypeDefinitions
            .FirstOrDefaultAsync(x => x.Key == key);

        return type == null ? null : MapItemTypeToDto(type);
    }

    public async Task<PlayerInventoryDto> GetInventoryAsync(string userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        var items = await db.PlayerItems
            .Include(x => x.ItemType)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.ItemTypeId)
            .ToListAsync();

        return new PlayerInventoryDto(userId, items.Select(MapItemToDto).ToArray());
    }

    public async Task<PlayerItemDto?> GetItemBalanceAsync(string userId, string itemTypeKey)
    {
        var itemType = await db.ItemTypeDefinitions.FirstOrDefaultAsync(x => x.Key == itemTypeKey);
        if (itemType == null) return null;

        var item = await db.PlayerItems
            .Include(x => x.ItemType)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemTypeId == itemType.Id);

        if (item == null)
        {
            // Return zero balance row (not persisted)
            return new PlayerItemDto(
                string.Empty,
                userId,
                itemType.Id,
                itemType.Key,
                itemType.DisplayName,
                itemType.Category,
                0,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
        }

        return MapItemToDto(item);
    }

    public async Task<PlayerItemDto> GrantItemsAsync(string userId, string itemTypeKey, long quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        var itemType = await db.ItemTypeDefinitions.FirstOrDefaultAsync(x => x.Key == itemTypeKey)
            ?? throw new KeyNotFoundException($"Item type '{itemTypeKey}' not found.");

        var item = await db.PlayerItems
            .Include(x => x.ItemType)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemTypeId == itemType.Id);

        if (item == null)
        {
            item = new PlayerItem
            {
                UserId = userId,
                ItemTypeId = itemType.Id,
                Quantity = quantity,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.PlayerItems.Add(item);
        }
        else
        {
            item.Quantity += quantity;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            db.PlayerItems.Update(item);
        }

        await db.SaveChangesAsync();
        item.ItemType = itemType;
        return MapItemToDto(item);
    }

    public async Task<PlayerItemDto> ConsumeItemsAsync(string userId, string itemTypeKey, long quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        var itemType = await db.ItemTypeDefinitions.FirstOrDefaultAsync(x => x.Key == itemTypeKey)
            ?? throw new KeyNotFoundException($"Item type '{itemTypeKey}' not found.");

        var item = await db.PlayerItems
            .Include(x => x.ItemType)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemTypeId == itemType.Id);

        var current = item?.Quantity ?? 0;
        if (current < quantity)
            throw new InvalidOperationException($"Insufficient '{itemTypeKey}': have {current}, need {quantity}.");

        item!.Quantity -= quantity;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        db.PlayerItems.Update(item);
        await db.SaveChangesAsync();
        return MapItemToDto(item);
    }

    public async Task<bool> HasSufficientItemsAsync(string userId, string itemTypeKey, long quantity)
    {
        var itemType = await db.ItemTypeDefinitions.FirstOrDefaultAsync(x => x.Key == itemTypeKey);
        if (itemType == null) return false;

        var item = await db.PlayerItems
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemTypeId == itemType.Id);

        return (item?.Quantity ?? 0) >= quantity;
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    internal static ItemTypeDto MapItemTypeToDto(ItemTypeDefinition x) => new(
        x.Id,
        x.Key,
        x.DisplayName,
        x.Description,
        x.Category,
        x.MaxStack,
        x.IsActive,
        x.IconAssetRef,
        x.MetadataJson);

    internal static PlayerItemDto MapItemToDto(PlayerItem x) => new(
        x.Id,
        x.UserId,
        x.ItemTypeId,
        x.ItemType.Key,
        x.ItemType.DisplayName,
        x.ItemType.Category,
        x.Quantity,
        x.CreatedAt,
        x.UpdatedAt);
}
