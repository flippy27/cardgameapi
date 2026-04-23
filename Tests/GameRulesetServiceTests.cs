using Microsoft.EntityFrameworkCore;
using Xunit;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Tests;

public class GameRulesetServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task GetDefaultAsync_RequiresAnActiveDefaultRuleset()
    {
        await using var dbContext = CreateDbContext();
        dbContext.GameRulesets.Add(new GameRuleset
        {
            Id = "rules-disabled",
            RulesetKey = "disabled",
            DisplayName = "Disabled Rules",
            IsActive = false,
            IsDefault = true
        });
        await dbContext.SaveChangesAsync();

        var service = new GameRulesetService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDefaultAsync());
        Assert.Contains("No active default ruleset", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_RejectsSeatOverridesThatProduceInvalidEffectiveValues()
    {
        await using var dbContext = CreateDbContext();
        var service = new GameRulesetService(dbContext);

        var request = new UpsertGameRulesetRequest(
            RulesetKey: "bad-handicap",
            DisplayName: "Bad Handicap",
            Description: null,
            IsActive: true,
            IsDefault: false,
            StartingHeroHealth: 20,
            MaxHeroHealth: 20,
            StartingMana: 1,
            MaxMana: 10,
            ManaGrantedPerTurn: 1,
            ManaGrantTiming: ManaGrantTiming.StartOfTurn,
            InitialDrawCount: 4,
            CardsDrawnOnTurnStart: 1,
            StartingSeatIndex: 0,
            SeatOverrides: new[]
            {
                new UpsertGameRulesSeatOverrideRequest(
                    SeatIndex: 1,
                    AdditionalHeroHealth: 0,
                    AdditionalMaxHeroHealth: -5,
                    AdditionalStartingMana: 0,
                    AdditionalMaxMana: 0,
                    AdditionalManaPerTurn: 0,
                    AdditionalCardsDrawnOnTurnStart: 0)
            });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request));
        Assert.Contains("MaxHeroHealth lower than StartingHeroHealth", exception.Message);
    }

    [Fact]
    public async Task ResolveForModeAsync_UsesServerSideModeAssignment()
    {
        await using var dbContext = CreateDbContext();
        var ruleset = new GameRuleset
        {
            Id = "rules-ranked",
            RulesetKey = "ranked",
            DisplayName = "Ranked Rules",
            IsActive = true,
            IsDefault = false
        };
        dbContext.GameRulesets.Add(ruleset);
        dbContext.MatchmakingModeRulesetAssignments.Add(new MatchmakingModeRulesetAssignment
        {
            Mode = QueueMode.Ranked,
            RulesetId = ruleset.Id
        });
        await dbContext.SaveChangesAsync();

        var service = new GameRulesetService(dbContext);
        var resolved = await service.ResolveForModeAsync(QueueMode.Ranked);

        Assert.Equal("rules-ranked", resolved.RulesetId);
        Assert.Equal("Ranked Rules", resolved.DisplayName);
    }

    [Fact]
    public async Task AssignModeAsync_RejectsInactiveRulesets()
    {
        await using var dbContext = CreateDbContext();
        dbContext.GameRulesets.Add(new GameRuleset
        {
            Id = "rules-inactive",
            RulesetKey = "inactive",
            DisplayName = "Inactive Rules",
            IsActive = false,
            IsDefault = false
        });
        await dbContext.SaveChangesAsync();

        var service = new GameRulesetService(dbContext);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AssignModeAsync(QueueMode.Casual, "rules-inactive"));

        Assert.Contains("inactive ruleset", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
