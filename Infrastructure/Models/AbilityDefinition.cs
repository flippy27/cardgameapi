namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class AbilityDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AbilityId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TriggerKind { get; set; } // TriggerKind enum: OnPlay, OnTurnStart, OnTurnEnd, OnBattlePhase
    public int TargetSelectorKind { get; set; } // TargetSelectorKind enum
    public int? SkillType { get; set; } // SkillType enum: Defensive, Offensive, Equipable, Utility, Modifier (nullable)

    // Foreign key
    public string CardDefinitionId { get; set; } = string.Empty;
    public CardDefinition CardDefinition { get; set; } = null!;

    // Navigation
    public ICollection<EffectDefinition> Effects { get; set; } = new List<EffectDefinition>();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
