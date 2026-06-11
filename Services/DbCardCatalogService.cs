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
            var abilities = card.CardAbilities
                .OrderBy(ca => ca.Sequence)
                .Select(ca => ca.AbilityDefinition)
                .Select(a => new ServerAbilityDefinition(
                    a.AbilityId,
                    a.DisplayName ?? a.AbilityId,
                    (TriggerKind)a.TriggerKind,
                    (TargetSelectorKind)a.TargetSelectorKind,
                    a.Effects.OrderBy(e => e.Sequence).Select(e => new ServerEffectDefinition(
                        (EffectKind)e.EffectKind,
                        e.Amount,
                        e.SecondaryAmount,
                        e.DurationTurns,
                        e.TargetSelectorKindOverride.HasValue ? (TargetSelectorKind)e.TargetSelectorKindOverride.Value : null,
                        e.MetadataJson)).ToList(),
                    (SkillType)a.SkillType,
                    string.IsNullOrWhiteSpace(a.AnimationCueId) ? null : a.AnimationCueId,
                    a.ConditionsJson,
                    a.MetadataJson
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
                abilities
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
}
