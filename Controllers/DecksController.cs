using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/decks")]
public sealed class DecksController(IDeckRepository deckRepository, ICardCatalogService cardCatalogService) : ControllerBase
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
        deckRepository.Upsert(request.PlayerId, request.DeckId, request.DisplayName, request.CardIds);
        return NoContent();
    }

    private void EnsurePlayer(string playerId)
    {
        var authenticated = User.FindFirst("sub")?.Value;
        if (!string.Equals(authenticated, playerId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Authenticated player mismatch.");
        }
    }
}
