using System.ComponentModel.DataAnnotations;
using CardDuel.ServerApi.Game;

namespace CardDuel.ServerApi.Contracts;

// ===== Card DTOs =====

public sealed record CardDefinitionDto(
    string Id,
    string CardId,
    string DisplayName,
    string Description,
    int ManaCost,
    int Attack,
    int Health,
    int Armor,
    int CardType,
    int CardRarity,
    int CardFaction,
    int? UnitType,
    int AllowedRow,
    int DefaultAttackSelector,
    int TurnsUntilCanAttack,
    bool IsLimited,
    BattlePresentationDto? BattlePresentation,
    IReadOnlyList<CardVisualProfileDto> VisualProfiles,
    List<AbilityDto> Abilities);

public sealed record BattlePresentationDto(
    int AttackMotionLevel,
    int AttackShakeLevel,
    string? AttackDeliveryType,
    string? ImpactFxId,
    string? AttackAudioCueId,
    string? MetadataJson);

public sealed record CardVisualLayerDto(
    string Surface,
    string Layer,
    string SourceKind,
    string AssetRef,
    int SortOrder,
    string? MetadataJson);

public sealed record CardVisualProfileDto(
    string ProfileKey,
    string DisplayName,
    bool IsDefault,
    IReadOnlyList<CardVisualLayerDto> Layers);

public sealed record UpsertBattlePresentationRequest(
    [Range(0, 5)] int AttackMotionLevel = 0,
    [Range(0, 5)] int AttackShakeLevel = 0,
    string? AttackDeliveryType = null,
    string? ImpactFxId = null,
    string? AttackAudioCueId = null,
    string? MetadataJson = null);

public sealed record UpsertCardVisualLayerRequest(
    [Required] string Surface,
    [Required] string Layer,
    [Required] string SourceKind,
    [Required] string AssetRef,
    int SortOrder = 0,
    string? MetadataJson = null);

public sealed record UpsertCardVisualProfileRequest(
    [Required] string ProfileKey,
    [Required] string DisplayName,
    bool IsDefault,
    [Required] IReadOnlyList<UpsertCardVisualLayerRequest> Layers);

public sealed record CreateCardRequest(
    [Required] string CardId,
    [Required] string DisplayName,
    string Description,
    [Range(0, 20)] int ManaCost,
    [Range(0, 20)] int Attack,
    [Range(1, 20)] int Health,
    [Range(0, 10)] int Armor,
    [Range(0, 3)] int CardType,
    [Range(0, 3)] int CardRarity,
    [Range(0, 4)] int CardFaction,
    int? UnitType,
    [Range(0, 2)] int AllowedRow,
    [Range(0, 4)] int DefaultAttackSelector,
    [Range(0, 5)] int TurnsUntilCanAttack = 1,
    bool IsLimited = false,
    UpsertBattlePresentationRequest? BattlePresentation = null,
    IReadOnlyList<UpsertCardVisualProfileRequest>? VisualProfiles = null);

public sealed record UpdateCardRequest(
    string? DisplayName,
    string? Description,
    int? ManaCost,
    int? Attack,
    int? Health,
    int? Armor,
    int? CardType,
    int? CardRarity,
    int? CardFaction,
    int? UnitType,
    int? AllowedRow,
    int? DefaultAttackSelector,
    int? TurnsUntilCanAttack,
    bool? IsLimited,
    UpsertBattlePresentationRequest? BattlePresentation = null,
    IReadOnlyList<UpsertCardVisualProfileRequest>? VisualProfiles = null);

// ===== Ability DTOs =====

public sealed record AbilityDto(
    string Id,
    string AbilityId,
    string DisplayName,
    string Description,
    int TriggerKind,
    int TargetSelectorKind,
    List<EffectDto> Effects);

public sealed record CreateAbilityRequest(
    [Required] string AbilityId,
    [Required] string DisplayName,
    string Description,
    [Range(0, 3)] int TriggerKind,
    [Range(0, 4)] int TargetSelectorKind,
    [Required] List<CreateEffectRequest> Effects);

public sealed record UpdateAbilityRequest(
    string? DisplayName,
    string? Description,
    int? TriggerKind,
    int? TargetSelectorKind);

// ===== Effect DTOs =====

public sealed record EffectDto(
    string Id,
    int EffectKind,
    int Amount,
    int Sequence);

public sealed record CreateEffectRequest(
    [Range(0, 26)] int EffectKind,
    [Range(1, 100)] int Amount,
    [Range(0, 10)] int Sequence);

public sealed record UpdateEffectRequest(
    int? EffectKind,
    int? Amount,
    int? Sequence);

// ===== Responses =====

public sealed record CardOperationResponse(
    bool Success,
    string Message,
    CardDefinitionDto? Card = null);

public sealed record AbilityOperationResponse(
    bool Success,
    string Message,
    AbilityDto? Ability = null);

public sealed record EffectOperationResponse(
    bool Success,
    string Message,
    EffectDto? Effect = null);
