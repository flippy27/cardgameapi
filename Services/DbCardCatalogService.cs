using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Game;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;
using CardDuel.ServerApi.Contracts;
using System.Text.Json;

namespace CardDuel.ServerApi.Services;

public sealed class DbCardCatalogService(AppDbContext dbContext) : ICardCatalogService
{
    private Dictionary<string, ServerCardDefinition>? _catalog;

    public IReadOnlyDictionary<string, ServerCardDefinition> GetAll()
    {
        if (_catalog != null) return _catalog;

        _catalog = new Dictionary<string, ServerCardDefinition>(StringComparer.OrdinalIgnoreCase);
        var cards = dbContext.Cards.AsNoTracking()
            .Include(c => c.CardAbilities)
            .ThenInclude(ca => ca.AbilityDefinition)
            .ThenInclude(a => a.Effects)
            .ToList();

        foreach (var card in cards)
        {
            var battlePresentation = DeserializeBattlePresentation(card.BattlePresentationJson);
            var visualProfiles = DeserializeVisualProfiles(card.VisualProfilesJson);
            var abilities = card.CardAbilities
                .OrderBy(ca => ca.Sequence)
                .Select(ca => ca.AbilityDefinition)
                .Select(a => new ServerAbilityDefinition(
                    a.AbilityId,
                    a.DisplayName ?? a.AbilityId,
                    (TriggerKind)a.TriggerKind,
                    (TargetSelectorKind)a.TargetSelectorKind,
                    a.Effects.OrderBy(e => e.Sequence).Select(e => new ServerEffectDefinition((EffectKind)e.EffectKind, e.Amount)).ToList()
                )).ToList().AsReadOnly();

            var def = new ServerCardDefinition(
                card.CardId,
                card.DisplayName,
                card.Description,
                card.ManaCost,
                card.Attack,
                card.Health,
                card.Armor,
                card.CardType,
                card.CardRarity,
                card.CardFaction,
                card.UnitType,
                (AllowedRow)card.AllowedRow,
                (TargetSelectorKind)card.DefaultAttackSelector,
                card.TurnsUntilCanAttack,
                abilities,
                battlePresentation?.AttackMotionLevel ?? 0,
                battlePresentation?.AttackShakeLevel ?? 0,
                battlePresentation?.AttackDeliveryType,
                visualProfiles
            );
            _catalog[card.CardId] = def;
        }

        return _catalog;
    }

    public IReadOnlyList<ServerCardDefinition> ResolveDeck(IEnumerable<string> cardIds)
    {
        var catalog = GetAll();
        return cardIds.Select(cardId => catalog.TryGetValue(cardId, out var card)
                ? card
                : throw new InvalidOperationException($"Unknown card id '{cardId}'."))
            .ToArray();
    }

    public void InvalidateCache() => _catalog = null;

    private static BattlePresentationDto? DeserializeBattlePresentation(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
        {
            return null;
        }

        return JsonSerializer.Deserialize<BattlePresentationDto>(json);
    }

    private static IReadOnlyList<ServerCardVisualProfile> DeserializeVisualProfiles(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
        {
            return Array.Empty<ServerCardVisualProfile>();
        }

        var profiles = JsonSerializer.Deserialize<List<CardVisualProfileDto>>(json) ?? new List<CardVisualProfileDto>();
        return profiles.Select(profile => new ServerCardVisualProfile(
            profile.ProfileKey,
            profile.DisplayName,
            profile.IsDefault,
            profile.Layers.Select(layer => new ServerCardVisualLayer(
                layer.Surface,
                layer.Layer,
                layer.SourceKind,
                layer.AssetRef,
                layer.SortOrder,
                layer.MetadataJson)).ToArray())).ToArray();
    }
}
