using CardDuel.ServerApi.Contracts;

namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class GameRuleset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RulesetKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int StartingHeroHealth { get; set; } = 20;
    public int MaxHeroHealth { get; set; } = 20;
    public int StartingMana { get; set; } = 1;
    public int MaxMana { get; set; } = 10;
    public int ManaGrantedPerTurn { get; set; } = 1;
    public ManaGrantTiming ManaGrantTiming { get; set; } = ManaGrantTiming.StartOfTurn;
    public int InitialDrawCount { get; set; } = 4;
    public int CardsDrawnOnTurnStart { get; set; } = 1;
    public int StartingSeatIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<GameRulesetSeatOverride> SeatOverrides { get; set; } = new List<GameRulesetSeatOverride>();
    public ICollection<MatchmakingModeRulesetAssignment> MatchmakingModeAssignments { get; set; } = new List<MatchmakingModeRulesetAssignment>();
}
