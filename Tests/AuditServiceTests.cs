using Microsoft.EntityFrameworkCore;
using Xunit;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Tests;

public class AuditServiceTests
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
    public async Task LogAsync_CreatesAuditLog()
    {
        using var db = CreateDbContext();
        var service = new AuditService(db);

        await service.LogAsync("user123", "PlayCard", "Match", "match456", "Card: card1", "192.168.1.1", 200);

        var log = db.AuditLogs.FirstOrDefault(l => l.UserId == "user123");
        Assert.NotNull(log);
        Assert.Equal("PlayCard", log.Action);
        Assert.Equal("Match", log.Resource);
        Assert.Equal("match456", log.ResourceId);
    }

    [Fact]
    public async Task LogAsync_StoresIpAndStatus()
    {
        using var db = CreateDbContext();
        var service = new AuditService(db);

        await service.LogAsync("user1", "CreateDeck", "Deck", "deck1", statusCode: 201, ipAddress: "10.0.0.1");

        var log = db.AuditLogs.First();
        Assert.Equal("10.0.0.1", log.IpAddress);
        Assert.Equal(201, log.StatusCode);
    }
}
