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
    List<AbilityDto> Abilities);

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
    string? TooltipSummary,
    string? ConditionsJson,
    string? MetadataJson,
    IReadOnlyList<EffectDto> Effects);

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
    bool IsLimited = false);

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
    bool? IsLimited);

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
