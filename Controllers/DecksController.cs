using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/decks")]
public sealed class DecksController(
    IDeckRepository deckRepository,
    ICardCatalogService cardCatalogService,
    IDeckValidationService deckValidation,
    ILogger<DecksController> logger) : ControllerBase
{
    [HttpGet("catalog")]
    public IActionResult GetCatalog() => Ok(cardCatalogService.GetAll().Values.OrderBy(x => x.CardId));

    [HttpGet("{playerId}")]
    public IActionResult GetDecks(string playerId)
    {
        EnsurePlayer(playerId);
        return Ok(deckRepository.GetDecks(playerId));
    }

    [HttpGet("{playerId}/{deckId}")]
    public IActionResult GetDeck(string playerId, string deckId)
    {
        EnsurePlayer(playerId);
        return Ok(ToDto(deckRepository.GetDeckDetails(playerId, deckId)));
    }

    [HttpPut]
    public IActionResult Upsert(DeckUpsertRequest request)
    {
        EnsurePlayer(request.PlayerId);

        if (string.IsNullOrWhiteSpace(request.DeckId) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequest(new { message = "DeckId and DisplayName are required" });
        }

        var (isValid, errorMessage) = deckValidation.ValidateDeck(request.CardIds, cardCatalogService);
        if (!isValid)
        {
            logger.LogWarning("Invalid deck submission by {PlayerId}: {Error}", request.PlayerId, errorMessage);
            return BadRequest(new { message = errorMessage });
        }

        deckRepository.Upsert(request.PlayerId, request.DeckId, request.DisplayName, request.CardIds);
        logger.LogInformation("Deck upserted: {PlayerId} / {DeckId}", request.PlayerId, request.DeckId);
        return NoContent();
    }

    [HttpPost("{playerId}/{deckId}/cards")]
    public IActionResult AddCard(string playerId, string deckId, [FromBody] AddDeckCardRequest request)
    {
        EnsurePlayer(playerId);
        var current = deckRepository.GetDeckDetails(playerId, deckId);
        var proposedCardIds = current.CardIds.ToList();
        proposedCardIds.Insert(Math.Clamp(request.Position ?? proposedCardIds.Count, 0, proposedCardIds.Count), request.CardId);
        var (isValid, errorMessage) = deckValidation.ValidateDeck(proposedCardIds, cardCatalogService);
        if (!isValid)
        {
            return BadRequest(new { message = errorMessage });
        }

        var details = deckRepository.AddCard(playerId, deckId, request.CardId, request.Position);
        return Ok(ToDto(details));
    }

    [HttpDelete("{playerId}/{deckId}/cards/{entryId}")]
    public IActionResult RemoveCard(string playerId, string deckId, string entryId)
    {
        EnsurePlayer(playerId);
        return Ok(ToDto(deckRepository.RemoveCard(playerId, deckId, entryId)));
    }

    private void EnsurePlayer(string playerId)
    {
        var authenticated = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.Equals(authenticated, playerId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Authenticated player mismatch.");
        }
    }

    private static DeckDetailsDto ToDto(PlayerDeckDetails deck) =>
        new(
            deck.PlayerId,
            deck.DeckId,
            deck.DisplayName,
            deck.Cards.Select(card => new DeckCardEntryDto(card.EntryId, card.CardId, card.Position)).ToArray(),
            deck.CardIds);
}
