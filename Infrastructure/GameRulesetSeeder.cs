using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Infrastructure;

public static class GameRulesetSeeder
{
    public static void SeedDefaultRuleset(AppDbContext dbContext)
    {
        var defaultRuleset = dbContext.GameRulesets.FirstOrDefault(ruleset => ruleset.IsDefault)
            ?? dbContext.GameRulesets.FirstOrDefault();

        if (defaultRuleset == null)
        {
            defaultRuleset = new GameRuleset
            {
                RulesetKey = "default",
                DisplayName = "Default Rules",
                Description = "Default server ruleset used for casual, ranked, and private matches unless another ruleset is selected.",
                IsActive = true,
                IsDefault = true,
                StartingHeroHealth = 20,
                MaxHeroHealth = 20,
                StartingMana = 1,
                MaxMana = 10,
                ManaGrantedPerTurn = 1,
                ManaGrantTiming = ManaGrantTiming.StartOfTurn,
                InitialDrawCount = 4,
                CardsDrawnOnTurnStart = 1,
                StartingSeatIndex = 0
            };

            dbContext.GameRulesets.Add(defaultRuleset);
            dbContext.SaveChanges();
        }

        foreach (var mode in Enum.GetValues<QueueMode>())
        {
            if (dbContext.MatchmakingModeRulesetAssignments.Any(assignment => assignment.Mode == mode))
            {
                continue;
            }

            dbContext.MatchmakingModeRulesetAssignments.Add(new MatchmakingModeRulesetAssignment
            {
                Mode = mode,
                RulesetId = defaultRuleset.Id
            });
        }

        dbContext.SaveChanges();
    }
}
