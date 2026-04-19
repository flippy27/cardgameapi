using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Game;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Services;

public sealed class DbCardCatalogService(AppDbContext dbContext) : ICardCatalogService
{
    private Dictionary<string, ServerCardDefinition>? _catalog;

    public IReadOnlyDictionary<string, ServerCardDefinition> GetAll()
    {
        if (_catalog != null) return _catalog;

        _catalog = new Dictionary<string, ServerCardDefinition>(StringComparer.OrdinalIgnoreCase);
        var cards = dbContext.Cards.AsNoTracking().ToList();

        foreach (var card in cards)
        {
            var abilities = ParseAbilities(card.AbilitiesJson);
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

    private static IReadOnlyList<ServerAbilityDefinition> ParseAbilities(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return Array.Empty<ServerAbilityDefinition>();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<AbilityJson>>(json, options) ?? new();
            return items.Select(a => new ServerAbilityDefinition(
                a.AbilityId,
                a.DisplayName ?? a.AbilityId,
                (TriggerKind)a.Trigger,
                (TargetSelectorKind)a.Selector,
                a.Effects.Select(e => new ServerEffectDefinition((EffectKind)e.Kind, e.Amount)).ToList()
            )).ToList().AsReadOnly();
        }
        catch
        {
            return Array.Empty<ServerAbilityDefinition>();
        }
    }

    private sealed record AbilityJson(string AbilityId, string? DisplayName, int Trigger, int Selector, List<EffectJson> Effects);
    private sealed record EffectJson(int Kind, int Amount);
}
