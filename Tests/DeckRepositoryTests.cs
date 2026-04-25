using Microsoft.EntityFrameworkCore;
using Xunit;
using CardDuel.ServerApi.Infrastructure.Models;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Tests;

public class DeckRepositoryTests
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

    private static void SeedCard(AppDbContext db, string cardId)
    {
        db.Cards.Add(new CardDefinition
        {
            Id = $"db-{cardId}",
            CardId = cardId,
            DisplayName = cardId,
            Description = "",
            ManaCost = 1,
            Attack = 1,
            Health = 1,
            Armor = 0,
            CardType = 0,
            CardRarity = 0,
            CardFaction = 0,
            UnitType = 0,
            AllowedRow = 2,
            DefaultAttackSelector = 1,
            TurnsUntilCanAttack = 1
        });
        db.SaveChanges();
    }

    [Fact]
    public void Upsert_CreatesNewDeck()
    {
        using var db = CreateDbContext();
        SeedCard(db, "ember_vanguard");
        SeedCard(db, "tidal_priest");
        var catalogService = new InMemoryCardCatalogService();
        var repo = new DbDeckRepository(catalogService, db);

        var cardIds = new[] { "ember_vanguard", "tidal_priest" };
        repo.Upsert("player1", "deck1", "Test Deck", cardIds);

        var deck = repo.GetDeck("player1", "deck1");
        Assert.NotNull(deck);
        Assert.Equal("Test Deck", deck.DisplayName);
        Assert.Equal(2, deck.CardIds.Count);
    }

    [Fact]
    public void GetDecks_ReturnsManyDecks()
    {
        using var db = CreateDbContext();
        SeedCard(db, "ember_vanguard");
        SeedCard(db, "tidal_priest");
        var catalogService = new InMemoryCardCatalogService();
        var repo = new DbDeckRepository(catalogService, db);

        repo.Upsert("player1", "deck1", "Deck A", new[] { "ember_vanguard" });
        repo.Upsert("player1", "deck2", "Deck B", new[] { "tidal_priest" });

        var decks = repo.GetDecks("player1");
        Assert.Equal(2, decks.Count);
    }

    [Fact]
    public void GetDeck_ThrowsWhenNotFound()
    {
        using var db = CreateDbContext();
        var catalogService = new InMemoryCardCatalogService();
        var repo = new DbDeckRepository(catalogService, db);

        Assert.Throws<InvalidOperationException>(() => repo.GetDeck("player1", "nonexistent"));
    }
}
