namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class CardAbilityDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CardDefinitionId { get; set; } = string.Empty;
    public string AbilityDefinitionId { get; set; } = string.Empty;
    public int Sequence { get; set; }

    public CardDefinition CardDefinition { get; set; } = null!;
    public AbilityDefinition AbilityDefinition { get; set; } = null!;
}
