using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Route("api/cards")]
public sealed class CardsController(ICardCatalogService cardCatalogService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAll()
    {
        var cards = cardCatalogService.GetAll().Values.OrderBy(c => c.CardId).ToList();
        return Ok(cards);
    }

    [HttpGet("{cardId}")]
    [AllowAnonymous]
    public IActionResult GetCard(string cardId)
    {
        var cards = cardCatalogService.GetAll();
        if (!cards.TryGetValue(cardId, out var card))
        {
            return NotFound(new { message = "Card not found" });
        }

        return Ok(card);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public IActionResult Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return BadRequest(new { message = "Search query must be at least 2 characters" });
        }

        var cards = cardCatalogService.GetAll().Values
            .Where(c => c.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                       c.CardId.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.DisplayName)
            .ToList();

        return Ok(cards);
    }

    [HttpGet("stats")]
    [AllowAnonymous]
    public IActionResult GetStats()
    {
        var cards = cardCatalogService.GetAll();
        return Ok(new
        {
            totalCards = cards.Count,
            manaCostAvg = cards.Values.Average(c => c.ManaCost),
            attackAvg = cards.Values.Average(c => c.Attack),
            healthAvg = cards.Values.Average(c => c.Health),
            cardsWithAbilities = cards.Values.Count(c => c.Abilities.Count > 0)
        });
    }
}
