using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

/// <summary>
/// Crafting system. Players spend items (earned from matches) to craft specific cards.
/// Each card has N crafting requirements (one row per item type needed).
/// The base material is "card_dust"; other materials are added as the game grows.
/// </summary>
[ApiController]
[Route("api/v1/crafting")]
[Tags("Crafting")]
public sealed class CraftingController(ICraftingService craftingService) : ControllerBase
{
    /// <summary>
    /// Get all cards that have crafting requirements defined (craftable cards).
    /// </summary>
    [HttpGet("cards")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCraftableCards()
    {
        return Ok(await craftingService.GetAllCraftableCardsAsync());
    }

    /// <summary>
    /// Get crafting requirements for a specific card.
    /// Returns the card and the list of items + quantities required to craft it.
    /// </summary>
    [HttpGet("cards/{cardId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCraftingInfo(string cardId)
    {
        var info = await craftingService.GetCraftingInfoAsync(cardId);
        if (info == null)
            return NotFound(new { message = $"Card '{cardId}' not found." });
        return Ok(info);
    }

    /// <summary>
    /// Craft a card. Deducts all required items from the player's inventory and
    /// adds a new owned card instance to their collection.
    /// Fails atomically if any item requirement is not met.
    /// </summary>
    [HttpPost("cards/{cardId}")]
    [Authorize]
    public async Task<IActionResult> CraftCard(string cardId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "User identity not found." });

        var result = await craftingService.CraftCardAsync(userId, cardId);

        if (!result.Success)
            return Conflict(new { message = result.Message });

        return Ok(result);
    }

    // ── Admin: crafting requirement management ────────────────────────────────

    /// <summary>
    /// [Admin] Set crafting requirements for a card. Replaces all existing requirements.
    /// Pass an empty list to make the card uncraftable.
    /// Each requirement maps an item_type_key to a required quantity.
    /// </summary>
    [HttpPut("cards/{cardId}/requirements")]
    [Authorize]
    public async Task<IActionResult> SetCraftingRequirements(
        string cardId, [FromBody] SetCraftingRequirementsRequest request)
    {
        try
        {
            var requirements = await craftingService.SetCraftingRequirementsAsync(cardId, request);
            return Ok(requirements);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Admin] Delete a single crafting requirement by its id.
    /// Use PUT /requirements to replace all requirements at once.
    /// </summary>
    [HttpDelete("cards/{cardId}/requirements/{requirementId}")]
    [Authorize]
    public async Task<IActionResult> DeleteCraftingRequirement(string cardId, string requirementId)
    {
        var deleted = await craftingService.DeleteCraftingRequirementAsync(cardId, requirementId);
        if (!deleted)
            return NotFound(new { message = $"Crafting requirement '{requirementId}' not found on card '{cardId}'." });
        return NoContent();
    }
}
