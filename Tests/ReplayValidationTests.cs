using Microsoft.EntityFrameworkCore;
using Xunit;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Tests;

public class ReplayValidationTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task ValidateReplay_EmptyMatch_ReturnsFalse()
    {
        using var db = CreateDbContext();
        var service = new ReplayValidationService(db);

        var (isValid, message) = await service.ValidateReplayAsync("match123");

        Assert.False(isValid);
        Assert.Contains("No actions", message);
    }

    [Fact]
    public async Task ValidateReplay_SequentialActions_ReturnsTrue()
    {
        using var db = CreateDbContext();

        db.MatchActions.Add(new MatchAction { MatchId = "m1", ActionNumber = 1, PlayerId = "p1", ActionType = "PlayCard", ActionData = "{}" });
        db.MatchActions.Add(new MatchAction { MatchId = "m1", ActionNumber = 2, PlayerId = "p2", ActionType = "EndTurn", ActionData = "{}" });
        await db.SaveChangesAsync();

        var service = new ReplayValidationService(db);
        var (isValid, message) = await service.ValidateReplayAsync("m1");

        Assert.True(isValid);
        Assert.Contains("passed", message);
    }

    [Fact]
    public async Task ValidateReplay_ActionNumberGap_ReturnsFalse()
    {
        using var db = CreateDbContext();

        db.MatchActions.Add(new MatchAction { MatchId = "m1", ActionNumber = 1, PlayerId = "p1", ActionType = "PlayCard", ActionData = "{}" });
        db.MatchActions.Add(new MatchAction { MatchId = "m1", ActionNumber = 3, PlayerId = "p2", ActionType = "EndTurn", ActionData = "{}" });
        await db.SaveChangesAsync();

        var service = new ReplayValidationService(db);
        var (isValid, message) = await service.ValidateReplayAsync("m1");

        Assert.False(isValid);
        Assert.Contains("gap", message);
    }
}
