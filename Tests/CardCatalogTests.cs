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
        var allCards = db.Cards.ToList();
        var cardIds = new[] { allCards[0].CardId, allCards[1].CardId };
        var resolved = service.ResolveDeck(cardIds);

        Assert.Equal(2, resolved.Count);
        Assert.NotNull(resolved[0]);
        Assert.NotNull(resolved[1]);
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

    [Fact]
    public void DbCardCatalogService_MapsBattlePresentationAndVisualProfiles()
    {
        using var db = CreateDbContext();
        db.Cards.Add(new Infrastructure.Models.CardDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            CardId = "visual_card",
            DisplayName = "Visual Card",
            Description = "Server authoritative visuals",
            ManaCost = 3,
            Attack = 4,
            Health = 5,
            Armor = 1,
            CardType = 0,
            CardRarity = 2,
            CardFaction = 1,
            UnitType = 1,
            AllowedRow = 2,
            DefaultAttackSelector = 1,
            TurnsUntilCanAttack = 1,
            BattlePresentationJson = """
                {"AttackMotionLevel":4,"AttackShakeLevel":2,"AttackDeliveryType":"beam","ImpactFxId":"beam-hit","AttackAudioCueId":"beam-audio","MetadataJson":"{\"trail\":\"long\"}"}
                """,
            VisualProfilesJson = """
                [{"ProfileKey":"hand-default","DisplayName":"Hand Default","IsDefault":true,"Layers":[{"Surface":"hand","Layer":"frame","SourceKind":"sprite","AssetRef":"frames/epic-hand","SortOrder":0,"MetadataJson":null},{"Surface":"hand","Layer":"art","SourceKind":"image","AssetRef":"art/visual-card","SortOrder":1,"MetadataJson":null}]}]
                """
        });
        db.SaveChanges();

        var service = new DbCardCatalogService(db);
        var card = service.GetAll()["visual_card"];

        Assert.Equal(4, card.AttackMotionLevel);
        Assert.Equal(2, card.AttackShakeLevel);
        Assert.Equal("beam", card.AttackDeliveryType);
        Assert.Single(card.VisualProfiles!);
        Assert.Equal("hand-default", card.VisualProfiles![0].ProfileKey);
        Assert.Equal("hand", card.VisualProfiles[0].Layers[0].Surface);
    }
}
