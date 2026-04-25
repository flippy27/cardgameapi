using System.ComponentModel.DataAnnotations;

namespace CardDuel.ServerApi.Contracts;

// ────────────────────────────────────────────────────────────────────────────
// Item type definitions (read-only catalog)
// ────────────────────────────────────────────────────────────────────────────

public sealed record ItemTypeDto(
    int Id,
    string Key,
    string DisplayName,
    string Description,
    string Category,
    int MaxStack,
    bool IsActive,
    string? IconAssetRef,
    string? MetadataJson);

// ────────────────────────────────────────────────────────────────────────────
// Player inventory
// ────────────────────────────────────────────────────────────────────────────

public sealed record PlayerItemDto(
    string Id,
    string UserId,
    int ItemTypeId,
    string ItemTypeKey,
    string ItemTypeDisplayName,
    string ItemTypeCategory,
    long Quantity,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PlayerInventoryDto(
    string UserId,
    IReadOnlyList<PlayerItemDto> Items);

public sealed record GrantItemsRequest(
    [Required] string ItemTypeKey,
    [Range(1, 999_999_999)] long Quantity,
    string? Reason = null);

public sealed record ConsumeItemsRequest(
    [Required] string ItemTypeKey,
    [Range(1, 999_999_999)] long Quantity,
    string? Reason = null);

public sealed record ItemOperationResponse(
    bool Success,
    string Message,
    PlayerItemDto? UpdatedItem = null);

// ────────────────────────────────────────────────────────────────────────────
// Player cards (owned card instances)
// ────────────────────────────────────────────────────────────────────────────

public sealed record PlayerCardDto(
    string Id,
    string UserId,
    string CardDefinitionId,
    string CardId,
    string DisplayName,
    int CardRarity,
    int CardFaction,
    int CardType,
    string AcquiredFrom,
    DateTimeOffset AcquiredAt);

public sealed record PlayerCardDetailDto(
    string Id,
    string UserId,
    string CardDefinitionId,
    string CardId,
    string DisplayName,
    string Description,
    int ManaCost,
    int BaseAttack,
    int BaseHealth,
    int BaseArmor,
    int CardRarity,
    int CardFaction,
    int CardType,
    int? UnitType,
    string AcquiredFrom,
    DateTimeOffset AcquiredAt,
    // Computed effective stats (base + all upgrade bonuses)
    int EffectiveAttack,
    int EffectiveHealth,
    int EffectiveArmor,
    int Level,
    IReadOnlyList<PlayerCardUpgradeDto> Upgrades);

public sealed record PlayerCardCollectionDto(
    string UserId,
    int TotalCards,
    IReadOnlyList<PlayerCardDto> Cards);

public sealed record GrantPlayerCardRequest(
    [Required] string CardId,  // the string cardId like "ember_vanguard"
    string AcquiredFrom = "admin_grant");

public sealed record PlayerCardOperationResponse(
    bool Success,
    string Message,
    PlayerCardDto? PlayerCard = null);

// ────────────────────────────────────────────────────────────────────────────
// Player card upgrades
// ────────────────────────────────────────────────────────────────────────────

public sealed record PlayerCardUpgradeDto(
    string Id,
    string PlayerCardId,
    string UpgradeKind,
    int? IntValue,
    string? StringValue,
    DateTimeOffset AppliedAt,
    string AppliedBy,
    string? Note);

public sealed record ApplyUpgradeRequest(
    [Required][MaxLength(64)] string UpgradeKind,
    int? IntValue = null,
    [MaxLength(255)] string? StringValue = null,
    string AppliedBy = "admin",
    [MaxLength(512)] string? Note = null);

public sealed record UpgradeOperationResponse(
    bool Success,
    string Message,
    PlayerCardUpgradeDto? Upgrade = null,
    PlayerCardDetailDto? UpdatedCard = null);

// ────────────────────────────────────────────────────────────────────────────
// Crafting
// ────────────────────────────────────────────────────────────────────────────

public sealed record CraftingRequirementDto(
    string Id,
    string CardDefinitionId,
    int ItemTypeId,
    string ItemTypeKey,
    string ItemTypeDisplayName,
    int QuantityRequired);

public sealed record CardCraftingInfoDto(
    string CardId,
    string DisplayName,
    int CardRarity,
    bool IsCraftable,
    IReadOnlyList<CraftingRequirementDto> Requirements);

public sealed record CraftCardResponse(
    bool Success,
    string Message,
    PlayerCardDto? PlayerCard = null,
    IReadOnlyList<PlayerItemDto>? UpdatedInventory = null);

public sealed record UpsertCraftingRequirementRequest(
    [Required] string ItemTypeKey,
    [Range(1, 9999)] int QuantityRequired);

public sealed record SetCraftingRequirementsRequest(
    [Required] IReadOnlyList<UpsertCraftingRequirementRequest> Requirements);

// ────────────────────────────────────────────────────────────────────────────
// Ownership check helpers (for deck building)
// ────────────────────────────────────────────────────────────────────────────

public sealed record CardOwnershipDto(
    string CardId,
    string DisplayName,
    int OwnedCopies,
    IReadOnlyList<PlayerCardDto> OwnedInstances);

public sealed record PlayerCardCollectionSummaryDto(
    string UserId,
    int UniqueCardTypes,
    int TotalCopies,
    IReadOnlyList<CardOwnershipDto> Cards);
