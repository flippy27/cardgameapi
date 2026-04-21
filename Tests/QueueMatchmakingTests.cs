using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Game;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Tests;

public class QueueMatchmakingTests
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

    private static ResolvedGameRules CreateResolvedGameRules(AppDbContext dbContext)
    {
        var ruleset = new GameRuleset
        {
            Id = "rules-default",
            RulesetKey = "default",
            DisplayName = "Default Rules",
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

        dbContext.GameRulesets.Add(ruleset);
        dbContext.SaveChanges();

        var rules = GameRules.FromEntity(ruleset);
        return new ResolvedGameRules(rules.RulesetId, rules.DisplayName, rules, rules.ToSnapshotJson());
    }

    [Fact]
    public void QueueForMatchRequest_ConstructorParametersExposeRequiredMetadata()
    {
        var constructor = typeof(QueueForMatchRequest).GetConstructors().Single();
        var parameters = constructor.GetParameters();
        var playerIdParameter = parameters.Single(parameter => parameter.Name == "PlayerId");
        var deckIdParameter = parameters.Single(parameter => parameter.Name == "DeckId");

        Assert.NotEmpty(playerIdParameter.GetCustomAttributes(typeof(RequiredAttribute), inherit: false));
        Assert.NotEmpty(deckIdParameter.GetCustomAttributes(typeof(RequiredAttribute), inherit: false));
    }

    [Fact]
    public void RequestRecords_DoNotPlaceValidationAttributesOnProperties()
    {
        var requestTypes = new[]
        {
            typeof(CreatePrivateMatchRequest),
            typeof(JoinPrivateMatchRequest),
            typeof(QueueForMatchRequest),
            typeof(ConnectMatchRequest),
            typeof(SetReadyRequest),
            typeof(PlayCardRequest),
            typeof(EndTurnRequest),
            typeof(ForfeitRequest),
            typeof(DeckUpsertRequest),
            typeof(MatchCompletionRequest),
            typeof(MatchActionDto),
            typeof(PostActionsRequest)
        };

        foreach (var requestType in requestTypes)
        {
            var propertiesWithValidationAttributes = requestType.GetProperties()
                .Where(property => property.GetCustomAttributes(typeof(ValidationAttribute), inherit: false).Any())
                .Select(property => property.Name)
                .ToArray();

            Assert.True(propertiesWithValidationAttributes.Length == 0,
                $"{requestType.Name} should not define validation attributes on properties: {string.Join(", ", propertiesWithValidationAttributes)}");
        }
    }

    [Fact]
    public void Queue_UsesDatabaseDeckAndReturnsQueuedReservation()
    {
        using var db = CreateDbContext();

        db.Cards.Add(new CardDefinition
        {
            Id = "card-db-1",
            CardId = "ember_0001",
            DisplayName = "Seed Card",
            Description = "Card used for queue tests",
            ManaCost = 1,
            Attack = 1,
            Health = 1,
            Armor = 0,
            CardType = 0,
            CardRarity = 0,
            CardFaction = 0,
            AllowedRow = 2,
            DefaultAttackSelector = 1,
            TurnsUntilCanAttack = 1,
            IsLimited = false
        });

        db.Decks.Add(new CardDuel.ServerApi.Infrastructure.Models.PlayerDeck
        {
            Id = "deck-db-1",
            UserId = "user-1",
            DeckId = "deck_playerone_1",
            DisplayName = "Queue Test Deck",
            CardIds = new List<string> { "ember_0001" }
        });

        db.SaveChanges();
        var resolvedRules = CreateResolvedGameRules(db);

        var catalog = new DbCardCatalogService(db);
        var deckRepository = new DbDeckRepository(catalog, db);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Game:DisconnectGraceSeconds"] = "20"
            })
            .Build();
        var services = new ServiceCollection()
            .AddScoped(_ => db)
            .BuildServiceProvider();
        var matchService = new InMemoryMatchService(deckRepository, catalog, configuration, services);

        var reservation = matchService.Queue("user-1", "deck_playerone_1", QueueMode.Casual, 1000, resolvedRules);

        Assert.Equal(QueueMode.Casual, reservation.Mode);
        Assert.True(reservation.WaitingForOpponent);
        Assert.Equal("queued", reservation.Status);
        Assert.Equal("rules-default", reservation.RulesetId);
        Assert.False(string.IsNullOrWhiteSpace(reservation.MatchId));
        Assert.False(string.IsNullOrWhiteSpace(reservation.ReconnectToken));
    }
}
