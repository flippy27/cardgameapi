namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class AbilityDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AbilityId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SkillType { get; set; }
    public int TriggerKind { get; set; } // TriggerKind enum: OnPlay, OnTurnStart, OnTurnEnd, OnBattlePhase
    public int TargetSelectorKind { get; set; } // TargetSelectorKind enum
    public string AnimationCueId { get; set; } = string.Empty;
    public string? IconAssetRef { get; set; }
    public string? StatusIconAssetRef { get; set; }
    public string? VfxCueId { get; set; }
    public string? AudioCueId { get; set; }
    public string? UiColorHex { get; set; }
    public string? TooltipSummary { get; set; }
    public string ConditionsJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";

    // Navigation
    public ICollection<CardAbilityDefinition> CardAbilities { get; set; } = new List<CardAbilityDefinition>();
    public ICollection<EffectDefinition> Effects { get; set; } = new List<EffectDefinition>();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
