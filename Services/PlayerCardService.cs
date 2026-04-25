using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace CardDuel.ServerApi.Services;

public interface IPlayerCardService
{
    Task<PlayerCardCollectionDto> GetCollectionAsync(string userId);
    Task<PlayerCardCollectionSummaryDto> GetCollectionSummaryAsync(string userId);
    Task<PlayerCardDetailDto?> GetPlayerCardAsync(string userId, string playerCardId);
    Task<PlayerCardDto> GrantCardAsync(string userId, string cardId, string acquiredFrom);
    Task<bool> DeletePlayerCardAsync(string userId, string playerCardId);
    Task<bool> UserOwnsCardAsync(string userId, string cardDefinitionId);
    Task<IReadOnlyList<PlayerCardDto>> GetOwnedCopiesAsync(string userId, string cardId);

    Task<PlayerCardUpgradeDto> ApplyUpgradeAsync(string userId, string playerCardId, ApplyUpgradeRequest request);
    Task<bool> RemoveUpgradeAsync(string userId, string playerCardId, string upgradeId);
    Task<IReadOnlyList<PlayerCardUpgradeDto>> GetUpgradesAsync(string userId, string playerCardId);
}

public sealed class PlayerCardService(AppDbContext db) : IPlayerCardService
{
    public async Task<PlayerCardCollectionDto> GetCollectionAsync(string userId)
    {
        var cards = await db.PlayerCards
            .Include(pc => pc.CardDefinition)
            .Where(pc => pc.UserId == userId)
            .OrderBy(pc => pc.CardDefinition.CardId)
            .ThenBy(pc => pc.AcquiredAt)
            .ToListAsync();

        return new PlayerCardCollectionDto(
            userId,
            cards.Count,
            cards.Select(MapToDto).ToArray());
    }

    public async Task<PlayerCardCollectionSummaryDto> GetCollectionSummaryAsync(string userId)
    {
        var cards = await db.PlayerCards
            .Include(pc => pc.CardDefinition)
            .Where(pc => pc.UserId == userId)
            .OrderBy(pc => pc.CardDefinition.CardId)
            .ThenBy(pc => pc.AcquiredAt)
            .ToListAsync();

        var grouped = cards
            .GroupBy(pc => pc.CardDefinition.CardId)
            .Select(g => new CardOwnershipDto(
                g.Key,
                g.First().CardDefinition.DisplayName,
                g.Count(),
                g.Select(MapToDto).ToArray()))
            .OrderBy(x => x.CardId)
            .ToArray();

        return new PlayerCardCollectionSummaryDto(
            userId,
            grouped.Length,
            cards.Count,
            grouped);
    }

    public async Task<PlayerCardDetailDto?> GetPlayerCardAsync(string userId, string playerCardId)
    {
        var pc = await db.PlayerCards
            .Include(x => x.CardDefinition)
            .Include(x => x.Upgrades)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == playerCardId);

        return pc == null ? null : MapToDetailDto(pc);
    }

    public async Task<PlayerCardDto> GrantCardAsync(string userId, string cardId, string acquiredFrom)
    {
        var cardDef = await db.Cards.FirstOrDefaultAsync(c => c.CardId == cardId)
            ?? throw new KeyNotFoundException($"Card '{cardId}' not found.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        var playerCard = new PlayerCard
        {
            UserId = userId,
            CardDefinitionId = cardDef.Id,
            AcquiredFrom = acquiredFrom,
            AcquiredAt = DateTimeOffset.UtcNow
        };

        db.PlayerCards.Add(playerCard);
        await db.SaveChangesAsync();

        playerCard.CardDefinition = cardDef;
        return MapToDto(playerCard);
    }

    public async Task<bool> DeletePlayerCardAsync(string userId, string playerCardId)
    {
        var pc = await db.PlayerCards
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == playerCardId);

        if (pc == null) return false;

        db.PlayerCards.Remove(pc);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UserOwnsCardAsync(string userId, string cardDefinitionId)
    {
        return await db.PlayerCards
            .AnyAsync(pc => pc.UserId == userId && pc.CardDefinitionId == cardDefinitionId);
    }

    public async Task<IReadOnlyList<PlayerCardDto>> GetOwnedCopiesAsync(string userId, string cardId)
    {
        var cardDef = await db.Cards.FirstOrDefaultAsync(c => c.CardId == cardId);
        if (cardDef == null) return Array.Empty<PlayerCardDto>();

        var cards = await db.PlayerCards
            .Include(pc => pc.CardDefinition)
            .Where(pc => pc.UserId == userId && pc.CardDefinitionId == cardDef.Id)
            .OrderBy(pc => pc.AcquiredAt)
            .ToListAsync();

        return cards.Select(MapToDto).ToArray();
    }

    public async Task<PlayerCardUpgradeDto> ApplyUpgradeAsync(string userId, string playerCardId, ApplyUpgradeRequest request)
    {
        var pc = await db.PlayerCards
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == playerCardId)
            ?? throw new KeyNotFoundException($"Player card '{playerCardId}' not found.");

        var upgrade = new PlayerCardUpgrade
        {
            PlayerCardId = playerCardId,
            UpgradeKind = request.UpgradeKind,
            IntValue = request.IntValue,
            StringValue = request.StringValue,
            AppliedAt = DateTimeOffset.UtcNow,
            AppliedBy = request.AppliedBy,
            Note = request.Note
        };

        db.PlayerCardUpgrades.Add(upgrade);
        await db.SaveChangesAsync();
        return MapUpgradeToDto(upgrade);
    }

    public async Task<bool> RemoveUpgradeAsync(string userId, string playerCardId, string upgradeId)
    {
        var pc = await db.PlayerCards
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == playerCardId);
        if (pc == null) return false;

        var upgrade = await db.PlayerCardUpgrades
            .FirstOrDefaultAsync(u => u.Id == upgradeId && u.PlayerCardId == playerCardId);
        if (upgrade == null) return false;

        db.PlayerCardUpgrades.Remove(upgrade);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<PlayerCardUpgradeDto>> GetUpgradesAsync(string userId, string playerCardId)
    {
        var pc = await db.PlayerCards
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == playerCardId)
            ?? throw new KeyNotFoundException($"Player card '{playerCardId}' not found.");

        var upgrades = await db.PlayerCardUpgrades
            .Where(u => u.PlayerCardId == playerCardId)
            .OrderBy(u => u.AppliedAt)
            .ToListAsync();

        return upgrades.Select(MapUpgradeToDto).ToArray();
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    internal static PlayerCardDto MapToDto(PlayerCard pc) => new(
        pc.Id,
        pc.UserId,
        pc.CardDefinitionId,
        pc.CardDefinition.CardId,
        pc.CardDefinition.DisplayName,
        pc.CardDefinition.CardRarity,
        pc.CardDefinition.CardFaction,
        pc.CardDefinition.CardType,
        pc.AcquiredFrom,
        pc.AcquiredAt);

    internal static PlayerCardDetailDto MapToDetailDto(PlayerCard pc)
    {
        var upgrades = pc.Upgrades.OrderBy(u => u.AppliedAt).ToList();

        var attackBonus = upgrades.Where(u => u.UpgradeKind == "attack_bonus").Sum(u => u.IntValue ?? 0);
        var healthBonus = upgrades.Where(u => u.UpgradeKind == "health_bonus").Sum(u => u.IntValue ?? 0);
        var armorBonus  = upgrades.Where(u => u.UpgradeKind == "armor_bonus").Sum(u => u.IntValue ?? 0);
        var level       = 1 + upgrades.Count(u => u.UpgradeKind == "level_up");

        return new PlayerCardDetailDto(
            pc.Id,
            pc.UserId,
            pc.CardDefinitionId,
            pc.CardDefinition.CardId,
            pc.CardDefinition.DisplayName,
            pc.CardDefinition.Description,
            pc.CardDefinition.ManaCost,
            pc.CardDefinition.Attack,
            pc.CardDefinition.Health,
            pc.CardDefinition.Armor,
            pc.CardDefinition.CardRarity,
            pc.CardDefinition.CardFaction,
            pc.CardDefinition.CardType,
            pc.CardDefinition.UnitType,
            pc.AcquiredFrom,
            pc.AcquiredAt,
            pc.CardDefinition.Attack + attackBonus,
            pc.CardDefinition.Health + healthBonus,
            pc.CardDefinition.Armor + armorBonus,
            level,
            upgrades.Select(MapUpgradeToDto).ToArray());
    }

    internal static PlayerCardUpgradeDto MapUpgradeToDto(PlayerCardUpgrade u) => new(
        u.Id,
        u.PlayerCardId,
        u.UpgradeKind,
        u.IntValue,
        u.StringValue,
        u.AppliedAt,
        u.AppliedBy,
        u.Note);
}
