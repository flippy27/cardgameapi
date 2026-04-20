using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace CardDuel.ServerApi.Services;

public interface ICardManagementService
{
    Task<CardDefinitionDto> CreateCardAsync(CreateCardRequest request);
    Task<CardDefinitionDto> UpdateCardAsync(string cardId, UpdateCardRequest request);
    Task<bool> DeleteCardAsync(string cardId);
    Task<AbilityDto> AddAbilityAsync(string cardId, CreateAbilityRequest request);
    Task<AbilityDto> UpdateAbilityAsync(string cardId, string abilityId, UpdateAbilityRequest request);
    Task<bool> DeleteAbilityAsync(string cardId, string abilityId);
    Task<EffectDto> AddEffectAsync(string cardId, string abilityId, CreateEffectRequest request);
    Task<EffectDto> UpdateEffectAsync(string cardId, string abilityId, string effectId, UpdateEffectRequest request);
    Task<bool> DeleteEffectAsync(string cardId, string abilityId, string effectId);
    Task<CardDefinitionDto?> GetCardWithAbilitiesAsync(string cardId);
}

public sealed class CardManagementService(AppDbContext db) : ICardManagementService
{
    public async Task<CardDefinitionDto> CreateCardAsync(CreateCardRequest request)
    {
        var existing = await db.Cards.FirstOrDefaultAsync(c => c.CardId == request.CardId);
        if (existing != null)
            throw new InvalidOperationException($"Card with ID '{request.CardId}' already exists");

        var card = new CardDefinition
        {
            CardId = request.CardId,
            DisplayName = request.DisplayName,
            Description = request.Description,
            ManaCost = request.ManaCost,
            Attack = request.Attack,
            Health = request.Health,
            Armor = request.Armor,
            CardType = request.CardType,
            CardRarity = request.CardRarity,
            CardFaction = request.CardFaction,
            UnitType = request.UnitType,
            AllowedRow = request.AllowedRow,
            DefaultAttackSelector = request.DefaultAttackSelector,
            TurnsUntilCanAttack = request.TurnsUntilCanAttack,
            IsLimited = request.IsLimited
        };

        db.Cards.Add(card);
        await db.SaveChangesAsync();

        return MapToDto(card);
    }

    public async Task<CardDefinitionDto> UpdateCardAsync(string cardId, UpdateCardRequest request)
    {
        var card = await db.Cards.Include(c => c.CardAbilities).ThenInclude(ca => ca.AbilityDefinition).ThenInclude(a => a.Effects)
            .FirstOrDefaultAsync(c => c.CardId == cardId)
            ?? throw new KeyNotFoundException($"Card '{cardId}' not found");

        if (request.DisplayName != null) card.DisplayName = request.DisplayName;
        if (request.Description != null) card.Description = request.Description;
        if (request.ManaCost.HasValue) card.ManaCost = request.ManaCost.Value;
        if (request.Attack.HasValue) card.Attack = request.Attack.Value;
        if (request.Health.HasValue) card.Health = request.Health.Value;
        if (request.Armor.HasValue) card.Armor = request.Armor.Value;
        if (request.CardType.HasValue) card.CardType = request.CardType.Value;
        if (request.CardRarity.HasValue) card.CardRarity = request.CardRarity.Value;
        if (request.CardFaction.HasValue) card.CardFaction = request.CardFaction.Value;
        if (request.UnitType.HasValue) card.UnitType = request.UnitType.Value;
        if (request.AllowedRow.HasValue) card.AllowedRow = request.AllowedRow.Value;
        if (request.DefaultAttackSelector.HasValue) card.DefaultAttackSelector = request.DefaultAttackSelector.Value;
        if (request.TurnsUntilCanAttack.HasValue) card.TurnsUntilCanAttack = request.TurnsUntilCanAttack.Value;
        if (request.IsLimited.HasValue) card.IsLimited = request.IsLimited.Value;

        card.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return MapToDto(card);
    }

    public async Task<bool> DeleteCardAsync(string cardId)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.CardId == cardId);
        if (card == null) return false;

        db.Cards.Remove(card);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<AbilityDto> AddAbilityAsync(string cardId, CreateAbilityRequest request)
    {
        var card = await db.Cards.Include(c => c.CardAbilities)
            .FirstOrDefaultAsync(c => c.CardId == cardId)
            ?? throw new KeyNotFoundException($"Card '{cardId}' not found");

        var existingAbility = await db.Abilities.FirstOrDefaultAsync(a => a.AbilityId == request.AbilityId);
        var ability = existingAbility ?? new AbilityDefinition
        {
            AbilityId = request.AbilityId,
            DisplayName = request.DisplayName,
            Description = request.Description,
            TriggerKind = request.TriggerKind,
            TargetSelectorKind = request.TargetSelectorKind
        };

        if (existingAbility == null)
        {
            // Add effects in sequence
            for (int i = 0; i < request.Effects.Count; i++)
            {
                var effect = request.Effects[i];
                ability.Effects.Add(new EffectDefinition
                {
                    EffectKind = effect.EffectKind,
                    Amount = effect.Amount,
                    Sequence = i
                });
            }
            db.Abilities.Add(ability);
        }

        var cardAbility = new CardAbilityDefinition
        {
            CardDefinitionId = card.Id,
            AbilityDefinitionId = ability.Id,
            Sequence = card.CardAbilities.Count
        };
        db.CardAbilities.Add(cardAbility);
        await db.SaveChangesAsync();

        return MapToDto(ability);
    }

    public async Task<AbilityDto> UpdateAbilityAsync(string cardId, string abilityId, UpdateAbilityRequest request)
    {
        var cardAbility = await db.CardAbilities
            .Include(ca => ca.AbilityDefinition).ThenInclude(a => a.Effects)
            .Include(ca => ca.CardDefinition)
            .FirstOrDefaultAsync(ca => ca.CardDefinition.CardId == cardId && ca.AbilityDefinition.AbilityId == abilityId)
            ?? throw new KeyNotFoundException($"Ability '{abilityId}' not found on card '{cardId}'");

        var ability = cardAbility.AbilityDefinition;
        if (request.DisplayName != null) ability.DisplayName = request.DisplayName;
        if (request.Description != null) ability.Description = request.Description;
        if (request.TriggerKind.HasValue) ability.TriggerKind = request.TriggerKind.Value;
        if (request.TargetSelectorKind.HasValue) ability.TargetSelectorKind = request.TargetSelectorKind.Value;

        ability.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return MapToDto(ability);
    }

    public async Task<bool> DeleteAbilityAsync(string cardId, string abilityId)
    {
        var cardAbility = await db.CardAbilities
            .Include(ca => ca.CardDefinition)
            .FirstOrDefaultAsync(ca => ca.CardDefinition.CardId == cardId && ca.AbilityDefinition.AbilityId == abilityId);

        if (cardAbility == null) return false;

        db.CardAbilities.Remove(cardAbility);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<EffectDto> AddEffectAsync(string cardId, string abilityId, CreateEffectRequest request)
    {
        var cardAbility = await db.CardAbilities
            .Include(ca => ca.AbilityDefinition).ThenInclude(a => a.Effects)
            .Include(ca => ca.CardDefinition)
            .FirstOrDefaultAsync(ca => ca.CardDefinition.CardId == cardId && ca.AbilityDefinition.AbilityId == abilityId)
            ?? throw new KeyNotFoundException($"Ability '{abilityId}' not found on card '{cardId}'");

        var effect = new EffectDefinition
        {
            EffectKind = request.EffectKind,
            Amount = request.Amount,
            Sequence = request.Sequence,
            AbilityDefinitionId = cardAbility.AbilityDefinitionId
        };

        cardAbility.AbilityDefinition.Effects.Add(effect);
        await db.SaveChangesAsync();

        return MapToDto(effect);
    }

    public async Task<EffectDto> UpdateEffectAsync(string cardId, string abilityId, string effectId, UpdateEffectRequest request)
    {
        var effect = await db.Effects
            .Include(e => e.AbilityDefinition).ThenInclude(a => a.CardAbilities)
            .FirstOrDefaultAsync(e => e.Id == effectId &&
                e.AbilityDefinition.CardAbilities.Any(ca => ca.CardDefinition.CardId == cardId) &&
                e.AbilityDefinition.AbilityId == abilityId)
            ?? throw new KeyNotFoundException($"Effect '{effectId}' not found");

        if (request.EffectKind.HasValue) effect.EffectKind = request.EffectKind.Value;
        if (request.Amount.HasValue) effect.Amount = request.Amount.Value;
        if (request.Sequence.HasValue) effect.Sequence = request.Sequence.Value;

        await db.SaveChangesAsync();

        return MapToDto(effect);
    }

    public async Task<bool> DeleteEffectAsync(string cardId, string abilityId, string effectId)
    {
        var effect = await db.Effects
            .Include(e => e.AbilityDefinition).ThenInclude(a => a.CardAbilities)
            .FirstOrDefaultAsync(e => e.Id == effectId &&
                e.AbilityDefinition.CardAbilities.Any(ca => ca.CardDefinition.CardId == cardId) &&
                e.AbilityDefinition.AbilityId == abilityId);

        if (effect == null) return false;

        db.Effects.Remove(effect);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<CardDefinitionDto?> GetCardWithAbilitiesAsync(string cardId)
    {
        var card = await db.Cards
            .Include(c => c.CardAbilities.OrderBy(ca => ca.Sequence))
            .ThenInclude(ca => ca.AbilityDefinition)
            .ThenInclude(a => a.Effects.OrderBy(e => e.Sequence))
            .FirstOrDefaultAsync(c => c.CardId == cardId);

        return card == null ? null : MapToDto(card);
    }

    private static CardDefinitionDto MapToDto(CardDefinition card) =>
        new(card.Id, card.CardId, card.DisplayName, card.Description, card.ManaCost, card.Attack, card.Health,
            card.Armor, card.CardType, card.CardRarity, card.CardFaction, card.UnitType, card.AllowedRow,
            card.DefaultAttackSelector, card.TurnsUntilCanAttack, card.IsLimited,
            card.CardAbilities.OrderBy(ca => ca.Sequence).Select(ca => MapToDto(ca.AbilityDefinition)).ToList());

    private static AbilityDto MapToDto(AbilityDefinition ability) =>
        new(ability.Id, ability.AbilityId, ability.DisplayName, ability.Description, ability.TriggerKind,
            ability.TargetSelectorKind, ability.Effects.OrderBy(e => e.Sequence).Select(MapToDto).ToList());

    private static EffectDto MapToDto(EffectDefinition effect) =>
        new(effect.Id, effect.EffectKind, effect.Amount, effect.Sequence);
}
