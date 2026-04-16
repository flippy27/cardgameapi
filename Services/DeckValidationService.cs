using CardDuel.ServerApi.Game;

namespace CardDuel.ServerApi.Services;

public interface IDeckValidationService
{
    (bool isValid, string errorMessage) ValidateDeck(IReadOnlyList<string> cardIds, ICardCatalogService catalog);
}

public sealed class DeckValidationService : IDeckValidationService
{
    private const int MinDeckSize = 20;
    private const int MaxDeckSize = 30;
    private const int MaxCopiesPerCard = 3;

    public (bool isValid, string errorMessage) ValidateDeck(IReadOnlyList<string> cardIds, ICardCatalogService catalog)
    {
        if (cardIds.Count < MinDeckSize)
        {
            return (false, $"Deck must have at least {MinDeckSize} cards, got {cardIds.Count}");
        }

        if (cardIds.Count > MaxDeckSize)
        {
            return (false, $"Deck can have at most {MaxDeckSize} cards, got {cardIds.Count}");
        }

        var allCards = catalog.GetAll();
        var cardCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cardId in cardIds)
        {
            if (!allCards.ContainsKey(cardId))
            {
                return (false, $"Unknown card ID: {cardId}");
            }

            cardCounts[cardId] = cardCounts.TryGetValue(cardId, out var count) ? count + 1 : 1;

            if (cardCounts[cardId] > MaxCopiesPerCard)
            {
                return (false, $"Card '{cardId}' appears {cardCounts[cardId]} times, max is {MaxCopiesPerCard}");
            }
        }

        return (true, string.Empty);
    }
}
