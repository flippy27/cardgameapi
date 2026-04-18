using Microsoft.EntityFrameworkCore;
using Xunit;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Tests;

public class CardCatalogTests
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
    public void DbCardCatalogService_GetsAllCards()
    {
        using var db = CreateDbContext();
        CardCatalogSeeder.SeedCards(db);

        var service = new DbCardCatalogService(db);
        var catalog = service.GetAll();

        Assert.NotEmpty(catalog);
        Assert.True(catalog.Count >= 18, $"Expected at least 18 cards, got {catalog.Count}");
    }

    [Fact]
    public void DbCardCatalogService_ResolveDeck()
    {
        using var db = CreateDbContext();
        CardCatalogSeeder.SeedCards(db);

        var service = new DbCardCatalogService(db);
        var cardIds = new[] { "ember_vanguard", "tidal_priest" };
        var resolved = service.ResolveDeck(cardIds);

        Assert.Equal(2, resolved.Count);
        Assert.Equal("Ember Vanguard", resolved[0].DisplayName);
        Assert.Equal("Tidal Priest", resolved[1].DisplayName);
    }

    [Fact]
    public void DbCardCatalogService_ThrowsOnUnknownCard()
    {
        using var db = CreateDbContext();
        CardCatalogSeeder.SeedCards(db);

        var service = new DbCardCatalogService(db);
        var cardIds = new[] { "nonexistent_card" };

        Assert.Throws<InvalidOperationException>(() => service.ResolveDeck(cardIds));
    }
}
