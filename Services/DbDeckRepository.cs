using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Services;

public sealed class DbDeckRepository(ICardCatalogService catalogService, AppDbContext dbContext) : IDeckRepository
{
    public void Upsert(string playerId, string deckId, string displayName, IReadOnlyList<string> cardIds)
    {
        _ = catalogService.ResolveDeck(cardIds);
        var model = dbContext.Decks.FirstOrDefault(d => d.UserId == playerId && d.DeckId == deckId);
        if (model == null)
        {
            model = new Infrastructure.Models.PlayerDeck
            {
                Id = Guid.NewGuid().ToString(),
                UserId = playerId,
                DeckId = deckId,
                DisplayName = displayName,
                CardIds = cardIds.ToList(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            dbContext.Decks.Add(model);
        }
        else
        {
            model.DisplayName = displayName;
            model.CardIds = cardIds.ToList();
            model.UpdatedAt = DateTimeOffset.UtcNow;
            dbContext.Decks.Update(model);
        }
        dbContext.SaveChanges();
    }

    public PlayerDeck GetDeck(string playerId, string deckId)
    {
        var model = dbContext.Decks.FirstOrDefault(d => d.UserId == playerId && d.DeckId == deckId);
        if (model == null) throw new InvalidOperationException("Deck not found.");
        return new PlayerDeck(model.UserId, model.DeckId, model.DisplayName, model.CardIds);
    }

    public IReadOnlyList<PlayerDeck> GetDecks(string playerId)
    {
        return dbContext.Decks
            .Where(d => d.UserId == playerId)
            .OrderBy(d => d.DisplayName)
            .ToList()
            .Select(m => new PlayerDeck(m.UserId, m.DeckId, m.DisplayName, m.CardIds))
            .ToList()
            .AsReadOnly();
    }
}
