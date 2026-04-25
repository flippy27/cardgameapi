using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

/// <summary>
/// Player-owned card collection. Each entry is a unique card instance tied to a player.
/// Players can have N copies of the same card; each copy has its own id and upgrade history.
/// </summary>
[ApiController]
[Route("api/v1/players/{userId}/cards")]
[Tags("Player Cards")]
[Authorize]
public sealed class PlayerCardsController(IPlayerCardService playerCardService) : ControllerBase
{
    /// <summary>
    /// Get all cards owned by a player (flat list).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCollection(string userId)
    {
        EnsureAccess(userId);
        return Ok(await playerCardService.GetCollectionAsync(userId));
    }

    /// <summary>
    /// Get a grouped summary of owned cards (by card type, with copy count per type).
    /// Useful for the collection screen and deck building validation.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetCollectionSummary(string userId)
    {
        EnsureAccess(userId);
        return Ok(await playerCardService.GetCollectionSummaryAsync(userId));
    }

    /// <summary>
    /// Get a specific owned card instance with full detail and upgrade history.
    /// </summary>
    [HttpGet("{playerCardId}")]
    public async Task<IActionResult> GetPlayerCard(string userId, string playerCardId)
    {
        EnsureAccess(userId);
        var card = await playerCardService.GetPlayerCardAsync(userId, playerCardId);
        if (card == null)
            return NotFound(new { message = $"Player card '{playerCardId}' not found." });
        return Ok(card);
    }

    /// <summary>
    /// Get all owned copies of a specific card type (by cardId string, e.g. "ember_vanguard").
    /// </summary>
    [HttpGet("by-card/{cardId}")]
    public async Task<IActionResult> GetOwnedCopies(string userId, string cardId)
    {
        EnsureAccess(userId);
        return Ok(await playerCardService.GetOwnedCopiesAsync(userId, cardId));
    }

    /// <summary>
    /// [Admin] Grant a card to a player. Source is tracked via acquiredFrom.
    /// </summary>
    [HttpPost("grant")]
    public async Task<IActionResult> GrantCard(string userId, [FromBody] GrantPlayerCardRequest request)
    {
        try
        {
            var card = await playerCardService.GrantCardAsync(userId, request.CardId, request.AcquiredFrom);
            return CreatedAtAction(nameof(GetPlayerCard),
                new { userId, playerCardId = card.Id }, card);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Admin] Delete/revoke an owned card instance from a player.
    /// </summary>
    [HttpDelete("{playerCardId}")]
    public async Task<IActionResult> DeletePlayerCard(string userId, string playerCardId)
    {
        var deleted = await playerCardService.DeletePlayerCardAsync(userId, playerCardId);
        if (!deleted)
            return NotFound(new { message = $"Player card '{playerCardId}' not found." });
        return NoContent();
    }

    // ── Upgrade endpoints ─────────────────────────────────────────────────────

    /// <summary>
    /// Get all upgrades applied to a specific owned card instance.
    /// </summary>
    [HttpGet("{playerCardId}/upgrades")]
    public async Task<IActionResult> GetUpgrades(string userId, string playerCardId)
    {
        EnsureAccess(userId);
        try
        {
            return Ok(await playerCardService.GetUpgradesAsync(userId, playerCardId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Apply an upgrade to a player-owned card instance.
    /// upgrade_kind is a free-form key: "attack_bonus", "health_bonus", "armor_bonus",
    /// "added_ability", "level_up", "custom_tag", etc.
    /// int_value = numeric delta (e.g. +3). string_value = reference id (e.g. ability_id).
    /// Multiple upgrades stack; each is a separate row.
    /// </summary>
    [HttpPost("{playerCardId}/upgrades")]
    public async Task<IActionResult> ApplyUpgrade(
        string userId, string playerCardId, [FromBody] ApplyUpgradeRequest request)
    {
        try
        {
            var upgrade = await playerCardService.ApplyUpgradeAsync(userId, playerCardId, request);
            return CreatedAtAction(nameof(GetUpgrades),
                new { userId, playerCardId }, upgrade);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a specific upgrade from an owned card instance.
    /// </summary>
    [HttpDelete("{playerCardId}/upgrades/{upgradeId}")]
    public async Task<IActionResult> RemoveUpgrade(string userId, string playerCardId, string upgradeId)
    {
        var deleted = await playerCardService.RemoveUpgradeAsync(userId, playerCardId, upgradeId);
        if (!deleted)
            return NotFound(new { message = $"Upgrade '{upgradeId}' not found." });
        return NoContent();
    }

    private void EnsureAccess(string userId)
    {
        var authenticatedId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.Equals(authenticatedId, userId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Cannot access another player's card collection.");
    }
}
