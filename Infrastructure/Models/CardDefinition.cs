namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class CardDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CardId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int ManaCost { get; set; }
    public int Attack { get; set; }
    public int Health { get; set; }
    public int Armor { get; set; }
    public int AllowedRow { get; set; } // 0=FrontOnly, 1=BackOnly, 2=Flexible
    public int DefaultAttackSelector { get; set; } // TargetSelectorKind
    public string AbilitiesJson { get; set; } = "[]"; // JSON serialized
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
