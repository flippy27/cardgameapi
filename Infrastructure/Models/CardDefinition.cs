namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class CardDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CardId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ManaCost { get; set; }
    public int Attack { get; set; }
    public int Health { get; set; }
    public int Armor { get; set; }
    public int CardType { get; set; } // CardType enum
    public int CardRarity { get; set; } // CardRarity enum
    public int CardFaction { get; set; } // CardFaction enum
    public int? UnitType { get; set; } // UnitType enum (null for non-units)
    public int AllowedRow { get; set; } // 0=FrontOnly, 1=BackOnly, 2=Flexible
    public int DefaultAttackSelector { get; set; } // TargetSelectorKind
    public int TurnsUntilCanAttack { get; set; } = 1;
    public bool IsLimited { get; set; } = false;
    public string BattlePresentationJson { get; set; } = "{}";
    public string VisualProfilesJson { get; set; } = "[]";

    // Navigation
    public ICollection<CardAbilityDefinition> CardAbilities { get; set; } = new List<CardAbilityDefinition>();
    public ICollection<CardVisualProfileAssignment> VisualProfileAssignments { get; set; } = new List<CardVisualProfileAssignment>();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
