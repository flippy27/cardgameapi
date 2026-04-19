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
    [property: Required] string CardId,
    [property: Required] string DisplayName,
    string Description,
    [property: Range(0, 20)] int ManaCost,
    [property: Range(0, 20)] int Attack,
    [property: Range(1, 20)] int Health,
    [property: Range(0, 10)] int Armor,
    [property: Range(0, 3)] int CardType,
    [property: Range(0, 3)] int CardRarity,
    [property: Range(0, 4)] int CardFaction,
    int? UnitType,
    [property: Range(0, 2)] int AllowedRow,
    [property: Range(0, 4)] int DefaultAttackSelector,
    [property: Range(0, 5)] int TurnsUntilCanAttack = 1,
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
    [property: Required] string AbilityId,
    [property: Required] string DisplayName,
    string Description,
    [property: Range(0, 3)] int TriggerKind,
    [property: Range(0, 4)] int TargetSelectorKind,
    [property: Required] List<CreateEffectRequest> Effects);

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
    [property: Range(0, 26)] int EffectKind,
    [property: Range(1, 100)] int Amount,
    [property: Range(0, 10)] int Sequence);

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
