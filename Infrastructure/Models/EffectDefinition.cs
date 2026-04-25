namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class EffectDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int EffectKind { get; set; } // EffectKind enum
    public int Amount { get; set; }
    public int? SecondaryAmount { get; set; }
    public int? DurationTurns { get; set; }
    public int? TargetSelectorKindOverride { get; set; }
    public int Sequence { get; set; } // Order within ability
    public string MetadataJson { get; set; } = "{}";

    // Foreign key
    public string AbilityDefinitionId { get; set; } = string.Empty;
    public AbilityDefinition AbilityDefinition { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
