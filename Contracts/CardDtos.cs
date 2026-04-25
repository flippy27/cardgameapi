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

public sealed record AuthoringLookupDto(
    int Id,
    string Key,
    string DisplayName,
    string Description,
    string Category,
    string? IconAssetRef,
    string? MetadataJson);

public sealed record EffectKindDefinitionDto(
    int Id,
    string Key,
    string DisplayName,
    string Description,
    string Category,
    int? ProducesStatusKind,
    string? IconAssetRef,
    string? MetadataJson);

public sealed record StatusEffectKindDefinitionDto(
    int Id,
    string Key,
    string DisplayName,
    string Description,
    string Category,
    string? IconAssetRef,
    string? VfxCueId,
    string? UiColorHex,
    string? MetadataJson);

public sealed record AbilityPresentationDto(
    string AbilityId,
    string DisplayName,
    string? IconAssetRef,
    string? StatusIconAssetRef,
    string? AnimationCueId,
    string? VfxCueId,
    string? AudioCueId,
    string? UiColorHex,
    string? TooltipSummary,
    string? MetadataJson);

public sealed record AbilityAuthoringDto(
    string Id,
    string AbilityId,
    string DisplayName,
    string Description,
    int SkillType,
    int TriggerKind,
    int TargetSelectorKind,
    string? AnimationCueId,
    string? IconAssetRef,
    string? StatusIconAssetRef,
    string? VfxCueId,
    string? AudioCueId,
    string? UiColorHex,
    string? TooltipSummary,
    string? ConditionsJson,
    string? MetadataJson,
    IReadOnlyList<EffectDto> Effects);

public sealed record UpsertAbilityPresentationRequest(
    string? IconAssetRef = null,
    string? StatusIconAssetRef = null,
    string? AnimationCueId = null,
    string? VfxCueId = null,
    string? AudioCueId = null,
    string? UiColorHex = null,
    string? TooltipSummary = null,
    string? MetadataJson = null);

public sealed record CardVisualProfileTemplateDto(
    string Id,
    string ProfileKey,
    string DisplayName,
    string Description,
    bool IsActive,
    IReadOnlyList<CardVisualLayerDto> Layers,
    string? MetadataJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertCardVisualProfileTemplateRequest(
    [Required] string ProfileKey,
    [Required] string DisplayName,
    string Description,
    bool IsActive,
    [Required] IReadOnlyList<UpsertCardVisualLayerRequest> Layers,
    string? MetadataJson = null);

public sealed record AssignCardVisualProfileTemplateRequest(
    [Required] string ProfileKey,
    bool IsDefault = false,
    string? OverrideDisplayName = null,
    IReadOnlyList<UpsertCardVisualLayerRequest>? OverrideLayers = null,
    string? MetadataJson = null);

public sealed record CardVisualProfileAssignmentDto(
    string Id,
    string CardId,
    string TemplateId,
    string ProfileKey,
    string DisplayName,
    bool IsDefault,
    IReadOnlyList<CardVisualLayerDto> Layers,
    string? MetadataJson);

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
    int SkillType,
    int TriggerKind,
    int TargetSelectorKind,
    string? AnimationCueId,
    string? ConditionsJson,
    string? MetadataJson,
    List<EffectDto> Effects);

public sealed record CreateAbilityRequest(
    [Required] string AbilityId,
    [Required] string DisplayName,
    string Description,
    [Range(0, 3)] int TriggerKind,
    [Range(0, 8)] int TargetSelectorKind,
    [Required] List<CreateEffectRequest> Effects,
    [Range(0, 4)] int SkillType = 3,
    string? AnimationCueId = null,
    string? IconAssetRef = null,
    string? StatusIconAssetRef = null,
    string? VfxCueId = null,
    string? AudioCueId = null,
    string? UiColorHex = null,
    string? TooltipSummary = null,
    string? ConditionsJson = null,
    string? MetadataJson = null);

public sealed record UpdateAbilityRequest(
    string? DisplayName,
    string? Description,
    int? SkillType,
    int? TriggerKind,
    int? TargetSelectorKind,
    string? AnimationCueId,
    string? IconAssetRef,
    string? StatusIconAssetRef,
    string? VfxCueId,
    string? AudioCueId,
    string? UiColorHex,
    string? TooltipSummary,
    string? ConditionsJson,
    string? MetadataJson);

// ===== Effect DTOs =====

public sealed record EffectDto(
    string Id,
    int EffectKind,
    int Amount,
    int? SecondaryAmount,
    int? DurationTurns,
    int? TargetSelectorKindOverride,
    int Sequence,
    string? MetadataJson);

public sealed record CreateEffectRequest(
    [Range(0, 30)] int EffectKind,
    [Range(1, 100)] int Amount,
    [Range(0, 10)] int Sequence,
    int? SecondaryAmount = null,
    int? DurationTurns = null,
    int? TargetSelectorKindOverride = null,
    string? MetadataJson = null);

public sealed record UpdateEffectRequest(
    int? EffectKind,
    int? Amount,
    int? SecondaryAmount,
    int? DurationTurns,
    int? TargetSelectorKindOverride,
    int? Sequence,
    string? MetadataJson);

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
