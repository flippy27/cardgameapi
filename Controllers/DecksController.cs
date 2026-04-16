using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/decks")]
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

    private void EnsurePlayer(string playerId)
    {
        var authenticated = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.Equals(authenticated, playerId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Authenticated player mismatch.");
        }
    }
}
