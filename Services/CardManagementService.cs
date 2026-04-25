using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CardDuel.ServerApi.Services;

public interface ICardManagementService
{
    Task<CardDefinitionDto> CreateCardAsync(CreateCardRequest request);
    Task<CardDefinitionDto> UpdateCardAsync(string cardId, UpdateCardRequest request);
    Task<bool> DeleteCardAsync(string cardId);
    Task<BattlePresentationDto?> UpdateBattlePresentationAsync(string cardId, UpsertBattlePresentationRequest request);
    Task<IReadOnlyList<CardVisualProfileDto>> ReplaceVisualProfilesAsync(string cardId, IReadOnlyList<UpsertCardVisualProfileRequest> request);
    Task<IReadOnlyList<CardVisualProfileDto>> UpsertVisualProfileAsync(string cardId, string profileKey, UpsertCardVisualProfileRequest request);
    Task<bool> DeleteVisualProfileAsync(string cardId, string profileKey);
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
            IsLimited = request.IsLimited,
            BattlePresentationJson = SerializeBattlePresentation(request.BattlePresentation),
            VisualProfilesJson = SerializeVisualProfiles(request.VisualProfiles)
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
        if (request.BattlePresentation != null) card.BattlePresentationJson = SerializeBattlePresentation(request.BattlePresentation);
        if (request.VisualProfiles != null) card.VisualProfilesJson = SerializeVisualProfiles(request.VisualProfiles);

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

    public async Task<BattlePresentationDto?> UpdateBattlePresentationAsync(string cardId, UpsertBattlePresentationRequest request)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.CardId == cardId)
            ?? throw new KeyNotFoundException($"Card '{cardId}' not found");

        card.BattlePresentationJson = SerializeBattlePresentation(request);
        card.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return DeserializeBattlePresentation(card.BattlePresentationJson);
    }

    public async Task<IReadOnlyList<CardVisualProfileDto>> ReplaceVisualProfilesAsync(string cardId, IReadOnlyList<UpsertCardVisualProfileRequest> request)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.CardId == cardId)
            ?? throw new KeyNotFoundException($"Card '{cardId}' not found");

        card.VisualProfilesJson = SerializeVisualProfiles(request);
        card.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return DeserializeVisualProfiles(card.VisualProfilesJson);
    }

    public async Task<IReadOnlyList<CardVisualProfileDto>> UpsertVisualProfileAsync(string cardId, string profileKey, UpsertCardVisualProfileRequest request)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.CardId == cardId)
            ?? throw new KeyNotFoundException($"Card '{cardId}' not found");

        var normalizedProfileKey = string.IsNullOrWhiteSpace(profileKey)
            ? request.ProfileKey
            : profileKey;

        var profile = new UpsertCardVisualProfileRequest(
            normalizedProfileKey,
            request.DisplayName,
            request.IsDefault,
            request.Layers);

        var profiles = DeserializeVisualProfiles(card.VisualProfilesJson).ToList();
        var replacement = ToVisualProfileDto(profile);
        var existingIndex = profiles.FindIndex(existing => string.Equals(existing.ProfileKey, normalizedProfileKey, StringComparison.OrdinalIgnoreCase));

        if (profile.IsDefault)
        {
            profiles = profiles
                .Select(existing => existing with { IsDefault = false })
                .ToList();
        }

        if (existingIndex >= 0)
        {
            profiles[existingIndex] = replacement;
        }
        else
        {
            profiles.Add(replacement);
        }

        card.VisualProfilesJson = JsonSerializer.Serialize(profiles);
        card.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return DeserializeVisualProfiles(card.VisualProfilesJson);
    }

    public async Task<bool> DeleteVisualProfileAsync(string cardId, string profileKey)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.CardId == cardId)
            ?? throw new KeyNotFoundException($"Card '{cardId}' not found");

        var profiles = DeserializeVisualProfiles(card.VisualProfilesJson).ToList();
        var removed = profiles.RemoveAll(profile => string.Equals(profile.ProfileKey, profileKey, StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
        {
            return false;
        }

        if (profiles.Count > 0 && profiles.All(profile => !profile.IsDefault))
        {
            profiles[0] = profiles[0] with { IsDefault = true };
        }

        card.VisualProfilesJson = JsonSerializer.Serialize(profiles);
        card.UpdatedAt = DateTimeOffset.UtcNow;
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
            SkillType = request.SkillType,
            TriggerKind = request.TriggerKind,
            TargetSelectorKind = request.TargetSelectorKind,
            AnimationCueId = request.AnimationCueId ?? string.Empty,
            IconAssetRef = request.IconAssetRef,
            StatusIconAssetRef = request.StatusIconAssetRef,
            VfxCueId = request.VfxCueId,
            AudioCueId = request.AudioCueId,
            UiColorHex = request.UiColorHex,
            TooltipSummary = request.TooltipSummary,
            ConditionsJson = string.IsNullOrWhiteSpace(request.ConditionsJson) ? "{}" : request.ConditionsJson,
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson
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
                    SecondaryAmount = effect.SecondaryAmount,
                    DurationTurns = effect.DurationTurns,
                    TargetSelectorKindOverride = effect.TargetSelectorKindOverride,
                    MetadataJson = string.IsNullOrWhiteSpace(effect.MetadataJson) ? "{}" : effect.MetadataJson,
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
        if (request.SkillType.HasValue) ability.SkillType = request.SkillType.Value;
        if (request.TriggerKind.HasValue) ability.TriggerKind = request.TriggerKind.Value;
        if (request.TargetSelectorKind.HasValue) ability.TargetSelectorKind = request.TargetSelectorKind.Value;
        if (request.AnimationCueId != null) ability.AnimationCueId = request.AnimationCueId;
        if (request.IconAssetRef != null) ability.IconAssetRef = request.IconAssetRef;
        if (request.StatusIconAssetRef != null) ability.StatusIconAssetRef = request.StatusIconAssetRef;
        if (request.VfxCueId != null) ability.VfxCueId = request.VfxCueId;
        if (request.AudioCueId != null) ability.AudioCueId = request.AudioCueId;
        if (request.UiColorHex != null) ability.UiColorHex = request.UiColorHex;
        if (request.TooltipSummary != null) ability.TooltipSummary = request.TooltipSummary;
        if (request.ConditionsJson != null) ability.ConditionsJson = string.IsNullOrWhiteSpace(request.ConditionsJson) ? "{}" : request.ConditionsJson;
        if (request.MetadataJson != null) ability.MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson;

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
            SecondaryAmount = request.SecondaryAmount,
            DurationTurns = request.DurationTurns,
            TargetSelectorKindOverride = request.TargetSelectorKindOverride,
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson,
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
        if (request.SecondaryAmount.HasValue) effect.SecondaryAmount = request.SecondaryAmount.Value;
        if (request.DurationTurns.HasValue) effect.DurationTurns = request.DurationTurns.Value;
        if (request.TargetSelectorKindOverride.HasValue) effect.TargetSelectorKindOverride = request.TargetSelectorKindOverride.Value;
        if (request.Sequence.HasValue) effect.Sequence = request.Sequence.Value;
        if (request.MetadataJson != null) effect.MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson;

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
            DeserializeBattlePresentation(card.BattlePresentationJson),
            DeserializeVisualProfiles(card.VisualProfilesJson),
            card.CardAbilities.OrderBy(ca => ca.Sequence).Select(ca => MapToDto(ca.AbilityDefinition)).ToList());

    private static AbilityDto MapToDto(AbilityDefinition ability) =>
        new(ability.Id, ability.AbilityId, ability.DisplayName, ability.Description, ability.SkillType, ability.TriggerKind,
            ability.TargetSelectorKind, ability.AnimationCueId, ability.ConditionsJson, ability.MetadataJson,
            ability.Effects.OrderBy(e => e.Sequence).Select(MapToDto).ToList());

    private static EffectDto MapToDto(EffectDefinition effect) =>
        new(effect.Id, effect.EffectKind, effect.Amount, effect.SecondaryAmount, effect.DurationTurns,
            effect.TargetSelectorKindOverride, effect.Sequence, effect.MetadataJson);

    private static string SerializeBattlePresentation(UpsertBattlePresentationRequest? request)
    {
        if (request == null)
        {
            return "{}";
        }

        return JsonSerializer.Serialize(new BattlePresentationDto(
            request.AttackMotionLevel,
            request.AttackShakeLevel,
            request.AttackDeliveryType,
            request.ImpactFxId,
            request.AttackAudioCueId,
            request.MetadataJson));
    }

    private static string SerializeVisualProfiles(IReadOnlyList<UpsertCardVisualProfileRequest>? request)
    {
        if (request == null)
        {
            return "[]";
        }

        var profiles = request.Select(ToVisualProfileDto).ToArray();

        return JsonSerializer.Serialize(profiles);
    }

    private static CardVisualProfileDto ToVisualProfileDto(UpsertCardVisualProfileRequest profile) =>
        new(
            profile.ProfileKey,
            profile.DisplayName,
            profile.IsDefault,
            profile.Layers.Select(layer => new CardVisualLayerDto(
                layer.Surface,
                layer.Layer,
                layer.SourceKind,
                layer.AssetRef,
                layer.SortOrder,
                layer.MetadataJson)).ToArray());

    private static BattlePresentationDto? DeserializeBattlePresentation(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
        {
            return null;
        }

        return JsonSerializer.Deserialize<BattlePresentationDto>(json);
    }

    private static IReadOnlyList<CardVisualProfileDto> DeserializeVisualProfiles(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
        {
            return Array.Empty<CardVisualProfileDto>();
        }

        var profiles = JsonSerializer.Deserialize<List<CardVisualProfileDto>>(json);
        return profiles ?? new List<CardVisualProfileDto>();
    }
}
