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
