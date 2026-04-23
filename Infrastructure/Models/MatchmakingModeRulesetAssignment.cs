using CardDuel.ServerApi.Contracts;

namespace CardDuel.ServerApi.Infrastructure.Models;

public sealed class MatchmakingModeRulesetAssignment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public QueueMode Mode { get; set; }
    public string RulesetId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public GameRuleset? Ruleset { get; set; }
}
