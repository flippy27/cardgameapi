using Xunit;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Tests;

public sealed class CardManagementServiceTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private CardManagementService _service = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-db-{Guid.NewGuid()}")
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _service = new CardManagementService(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task CreateCard_ValidRequest_ReturnsCard()
    {
        // Arrange
        var request = new CreateCardRequest(
            "test_card_1",
            "Test Card",
            "A test card",
            3,
            2,
            3,
            0,
            0,
            1,
            0,
            0,
            0,
            1,
            1,
            false
        );

        // Act
        var result = await _service.CreateCardAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test_card_1", result.CardId);
        Assert.Equal("Test Card", result.DisplayName);
        Assert.Equal(3, result.ManaCost);
    }

    [Fact]
    public async Task CreateCard_DuplicateCardId_ThrowsException()
    {
        // Arrange
        var request = new CreateCardRequest(
            "dup_card",
            "Card",
            "",
            1,
            1,
            1,
            0,
            0,
            0,
            0,
            null,
            0,
            0,
            1,
            false
        );

        await _service.CreateCardAsync(request);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateCardAsync(request));
    }

    [Fact]
    public async Task UpdateCard_ValidRequest_UpdatesCard()
    {
        // Arrange
        var createRequest = new CreateCardRequest(
            "update_test",
            "Original",
            "Original desc",
            1,
            1,
            1,
            0,
            0,
            0,
            0,
            null,
            0,
            0,
            1,
            false
        );
        await _service.CreateCardAsync(createRequest);

        var updateRequest = new UpdateCardRequest(
            "Updated",
            "Updated desc",
            5,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );

        // Act
        var result = await _service.UpdateCardAsync("update_test", updateRequest);

        // Assert
        Assert.Equal("Updated", result.DisplayName);
        Assert.Equal("Updated desc", result.Description);
        Assert.Equal(5, result.ManaCost);
    }

    [Fact]
    public async Task DeleteCard_ExistingCard_ReturnsTrue()
    {
        // Arrange
        var request = new CreateCardRequest(
            "delete_test",
            "To Delete",
            "",
            1,
            1,
            1,
            0,
            0,
            0,
            0,
            null,
            0,
            0,
            1,
            false
        );
        await _service.CreateCardAsync(request);

        // Act
        var result = await _service.DeleteCardAsync("delete_test");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteCard_NonExistentCard_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteCardAsync("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddAbility_ValidRequest_ReturnsAbility()
    {
        // Arrange
        var cardRequest = new CreateCardRequest(
            "ability_test",
            "Card",
            "",
            1,
            1,
            1,
            0,
            0,
            0,
            0,
            null,
            0,
            0,
            1,
            false
        );
        await _service.CreateCardAsync(cardRequest);

        var abilityRequest = new CreateAbilityRequest(
            "test_ability",
            "Test Ability",
            "An ability",
            0,
            0,
            new List<CreateEffectRequest>
            {
                new(0, 5, 0)
            }
        );

        // Act
        var result = await _service.AddAbilityAsync("ability_test", abilityRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test_ability", result.AbilityId);
        Assert.Equal("Test Ability", result.DisplayName);
        Assert.Single(result.Effects);
    }

    [Fact]
    public async Task GetCardWithAbilities_ExistingCard_ReturnsCardWithAbilities()
    {
        // Arrange
        var cardRequest = new CreateCardRequest(
            "full_test",
            "Full Test",
            "",
            1,
            1,
            1,
            0,
            0,
            0,
            0,
            null,
            0,
            0,
            1,
            false
        );
        await _service.CreateCardAsync(cardRequest);

        var abilityRequest = new CreateAbilityRequest(
            "full_ability",
            "Full Ability",
            "",
            0,
            0,
            new List<CreateEffectRequest>
            {
                new(0, 3, 0)
            }
        );
        await _service.AddAbilityAsync("full_test", abilityRequest);

        // Act
        var result = await _service.GetCardWithAbilitiesAsync("full_test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("full_test", result.CardId);
        Assert.Single(result.Abilities);
        Assert.Equal("full_ability", result.Abilities[0].AbilityId);
        Assert.Single(result.Abilities[0].Effects);
    }

    [Fact]
    public async Task DeleteAbility_ExistingAbility_ReturnsTrue()
    {
        // Arrange
        var cardRequest = new CreateCardRequest(
            "del_ability",
            "Card",
            "",
            1,
            1,
            1,
            0,
            0,
            0,
            0,
            null,
            0,
            0,
            1,
            false
        );
        await _service.CreateCardAsync(cardRequest);

        var abilityRequest = new CreateAbilityRequest(
            "del_ab",
            "To Delete",
            "",
            0,
            0,
            new List<CreateEffectRequest>
            {
                new(0, 1, 0)
            }
        );
        await _service.AddAbilityAsync("del_ability", abilityRequest);

        // Act
        var result = await _service.DeleteAbilityAsync("del_ability", "del_ab");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AddEffect_ValidRequest_ReturnsEffect()
    {
        // Arrange - Create card and ability
        var cardRequest = new CreateCardRequest(
            "effect_test",
            "Card",
            "",
            1,
            1,
            1,
            0,
            0,
            0,
            0,
            null,
            0,
            0,
            1,
            false
        );
        await _service.CreateCardAsync(cardRequest);

        var abilityRequest = new CreateAbilityRequest(
            "effect_ability",
            "Ability",
            "",
            0,
            0,
            new List<CreateEffectRequest>
            {
                new(0, 1, 0)
            }
        );
        var ability = await _service.AddAbilityAsync("effect_test", abilityRequest);

        // Act - Add effect
        var effectRequest = new CreateEffectRequest(
            1,
            5,
            1
        );
        var result = await _service.AddEffectAsync("effect_test", "effect_ability", effectRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.EffectKind);
        Assert.Equal(5, result.Amount);
    }

    [Fact]
    public async Task CreateCard_PersistsBattlePresentationAndVisualProfiles()
    {
        var request = new CreateCardRequest(
            "visual_test",
            "Visual Test",
            "Card with visuals",
            2,
            3,
            4,
            1,
            0,
            2,
            1,
            1,
            2,
            0,
            1,
            false,
            new UpsertBattlePresentationRequest(
                AttackMotionLevel: 4,
                AttackShakeLevel: 5,
                AttackDeliveryType: "projectile",
                ImpactFxId: "impact-heavy",
                AttackAudioCueId: "audio-heavy"),
            new[]
            {
                new UpsertCardVisualProfileRequest(
                    "default-hand",
                    "Default Hand",
                    true,
                    new[]
                    {
                        new UpsertCardVisualLayerRequest("hand", "frame", "sprite", "frames/rare-hand", 0),
                        new UpsertCardVisualLayerRequest("hand", "art", "image", "art/visual-test", 1)
                    }),
                new UpsertCardVisualProfileRequest(
                    "played-default",
                    "Played Default",
                    false,
                    new[]
                    {
                        new UpsertCardVisualLayerRequest("played", "frame", "sprite", "frames/rare-played", 0),
                        new UpsertCardVisualLayerRequest("played", "art", "image", "art/visual-test-full", 1, "{\"variant\":\"full-art\"}")
                    })
            });

        var result = await _service.CreateCardAsync(request);

        Assert.NotNull(result.BattlePresentation);
        Assert.Equal(4, result.BattlePresentation!.AttackMotionLevel);
        Assert.Equal(5, result.BattlePresentation.AttackShakeLevel);
        Assert.Equal("projectile", result.BattlePresentation.AttackDeliveryType);
        Assert.Equal(2, result.VisualProfiles.Count);
        Assert.Contains(result.VisualProfiles, profile => profile.ProfileKey == "default-hand" && profile.IsDefault);
        Assert.Contains(result.VisualProfiles.SelectMany(profile => profile.Layers), layer => layer.Surface == "played" && layer.Layer == "art");
    }
}
