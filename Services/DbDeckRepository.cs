using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Services;

public sealed class DbDeckRepository(ICardCatalogService catalogService, AppDbContext dbContext) : IDeckRepository
{
    public void Upsert(string playerId, string deckId, string displayName, IReadOnlyList<string> cardIds)
    {
        _ = catalogService.ResolveDeck(cardIds);
        var catalogCards = dbContext.Cards
            .Where(card => cardIds.Contains(card.CardId))
            .ToDictionary(card => card.CardId, StringComparer.OrdinalIgnoreCase);

        var missing = cardIds.FirstOrDefault(cardId => !catalogCards.ContainsKey(cardId));
        if (missing != null)
        {
            throw new InvalidOperationException($"Unknown card id '{missing}'.");
        }

        var model = dbContext.Decks
            .Include(d => d.DeckCards)
            .FirstOrDefault(d => d.UserId == playerId && d.DeckId == deckId);
        if (model == null)
        {
            model = new Infrastructure.Models.PlayerDeck
            {
                Id = Guid.NewGuid().ToString(),
                UserId = playerId,
                DeckId = deckId,
                DisplayName = displayName,
                CreatedAt = DateTimeOffset.UtcNow
            };
            dbContext.Decks.Add(model);
        }
        else
        {
            model.DisplayName = displayName;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            dbContext.DeckCards.RemoveRange(model.DeckCards);
            dbContext.Decks.Update(model);
        }

        for (var index = 0; index < cardIds.Count; index++)
        {
            model.DeckCards.Add(new DeckCard
            {
                Id = Guid.NewGuid().ToString(),
                CardDefinitionId = catalogCards[cardIds[index]].Id,
                Position = index
            });
        }

        dbContext.SaveChanges();
    }

    public PlayerDeck GetDeck(string playerId, string deckId)
    {
        var model = dbContext.Decks
            .Include(d => d.DeckCards)
            .ThenInclude(deckCard => deckCard.CardDefinition)
            .FirstOrDefault(d => d.UserId == playerId && d.DeckId == deckId);
        if (model == null) throw new InvalidOperationException("Deck not found.");
        return ToDeck(model);
    }

    public PlayerDeckDetails GetDeckDetails(string playerId, string deckId)
    {
        var model = dbContext.Decks
            .Include(d => d.DeckCards)
            .ThenInclude(deckCard => deckCard.CardDefinition)
            .FirstOrDefault(d => d.UserId == playerId && d.DeckId == deckId);
        if (model == null) throw new InvalidOperationException("Deck not found.");
        return ToDeckDetails(model);
    }

    public IReadOnlyList<PlayerDeck> GetDecks(string playerId)
    {
        return dbContext.Decks
            .Include(d => d.DeckCards)
            .ThenInclude(deckCard => deckCard.CardDefinition)
            .Where(d => d.UserId == playerId)
            .OrderBy(d => d.DisplayName)
            .ToList()
            .Select(ToDeck)
            .ToList()
            .AsReadOnly();
    }

    public PlayerDeckDetails AddCard(string playerId, string deckId, string cardId, int? position = null)
    {
        _ = catalogService.ResolveDeck(new[] { cardId });
        var model = dbContext.Decks
            .Include(d => d.DeckCards)
            .ThenInclude(deckCard => deckCard.CardDefinition)
            .FirstOrDefault(d => d.UserId == playerId && d.DeckId == deckId)
            ?? throw new InvalidOperationException("Deck not found.");

        var card = dbContext.Cards.FirstOrDefault(c => c.CardId == cardId)
            ?? throw new InvalidOperationException($"Unknown card id '{cardId}'.");
        var insertAt = Math.Clamp(position ?? model.DeckCards.Count, 0, model.DeckCards.Count);

        foreach (var deckCard in model.DeckCards.Where(deckCard => deckCard.Position >= insertAt))
        {
            deckCard.Position += 1;
        }

        model.DeckCards.Add(new DeckCard
        {
            Id = Guid.NewGuid().ToString(),
            CardDefinitionId = card.Id,
            Position = insertAt
        });
        model.UpdatedAt = DateTimeOffset.UtcNow;

        dbContext.SaveChanges();
        return GetDeckDetails(playerId, deckId);
    }

    public PlayerDeckDetails RemoveCard(string playerId, string deckId, string entryId)
    {
        var model = dbContext.Decks
            .Include(d => d.DeckCards)
            .ThenInclude(deckCard => deckCard.CardDefinition)
            .FirstOrDefault(d => d.UserId == playerId && d.DeckId == deckId)
            ?? throw new InvalidOperationException("Deck not found.");

        var removed = model.DeckCards.FirstOrDefault(deckCard => deckCard.Id == entryId)
            ?? throw new InvalidOperationException("Deck card entry not found.");

        var removedPosition = removed.Position;
        dbContext.DeckCards.Remove(removed);
        foreach (var deckCard in model.DeckCards.Where(deckCard => deckCard.Position > removedPosition))
        {
            deckCard.Position -= 1;
        }

        model.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.SaveChanges();
        return GetDeckDetails(playerId, deckId);
    }

    private static PlayerDeck ToDeck(Infrastructure.Models.PlayerDeck model) =>
        new(
            model.UserId,
            model.DeckId,
            model.DisplayName,
            model.DeckCards.OrderBy(card => card.Position).Select(card => card.CardDefinition.CardId).ToArray());

    private static PlayerDeckDetails ToDeckDetails(Infrastructure.Models.PlayerDeck model) =>
        new(
            model.UserId,
            model.DeckId,
            model.DisplayName,
            model.DeckCards
                .OrderBy(card => card.Position)
                .Select(card => new PlayerDeckCard(card.Id, card.CardDefinition.CardId, card.Position))
                .ToArray());
}
