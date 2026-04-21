namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class GameRulesetSeatOverride
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string GameRulesetId { get; set; } = string.Empty;
    public int SeatIndex { get; set; }
    public int AdditionalHeroHealth { get; set; }
    public int AdditionalMaxHeroHealth { get; set; }
    public int AdditionalStartingMana { get; set; }
    public int AdditionalMaxMana { get; set; }
    public int AdditionalManaPerTurn { get; set; }
    public int AdditionalCardsDrawnOnTurnStart { get; set; }
    public GameRuleset? GameRuleset { get; set; }
}
