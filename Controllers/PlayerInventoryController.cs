using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

/// <summary>
/// Player item inventory. Items are crafting materials, currencies, and upgrade resources.
/// The base item "card_dust" is earned from matches.
/// </summary>
[ApiController]
[Tags("Player Inventory")]
public sealed class PlayerInventoryController(IPlayerInventoryService inventoryService) : ControllerBase
{
    // ── Item type catalog (public) ────────────────────────────────────────────

    /// <summary>
    /// List all item types available in the game. Read-only catalog.
    /// </summary>
    [HttpGet("api/v1/items")]
    [AllowAnonymous]
    public async Task<IActionResult> GetItemTypes()
    {
        return Ok(await inventoryService.GetItemTypesAsync());
    }

    /// <summary>
    /// Get a specific item type by key (e.g. "card_dust", "arcane_shard").
    /// </summary>
    [HttpGet("api/v1/items/{key}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetItemType(string key)
    {
        var item = await inventoryService.GetItemTypeAsync(key);
        if (item == null)
            return NotFound(new { message = $"Item type '{key}' not found." });
        return Ok(item);
    }

    // ── Player inventory ──────────────────────────────────────────────────────

    /// <summary>
    /// Get a player's full item inventory. Shows all items, including zero balances
    /// for items the player has never received (omitted by default — only rows exist when
    /// quantity > 0 or was ever granted).
    /// </summary>
    [HttpGet("api/v1/players/{userId}/inventory")]
    [Authorize]
    public async Task<IActionResult> GetInventory(string userId)
    {
        EnsureAccess(userId);
        try
        {
            return Ok(await inventoryService.GetInventoryAsync(userId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get balance of a specific item type for a player.
    /// Returns 0 quantity if the player has never received this item type.
    /// </summary>
    [HttpGet("api/v1/players/{userId}/inventory/{itemTypeKey}")]
    [Authorize]
    public async Task<IActionResult> GetItemBalance(string userId, string itemTypeKey)
    {
        EnsureAccess(userId);
        var balance = await inventoryService.GetItemBalanceAsync(userId, itemTypeKey);
        if (balance == null)
            return NotFound(new { message = $"Item type '{itemTypeKey}' not found." });
        return Ok(balance);
    }

    /// <summary>
    /// [Admin/System] Grant items to a player. Used when rewarding match completions,
    /// events, admin grants, etc. The quantity is additive.
    /// </summary>
    [HttpPost("api/v1/players/{userId}/inventory/grant")]
    [Authorize]
    public async Task<IActionResult> GrantItems(string userId, [FromBody] GrantItemsRequest request)
    {
        try
        {
            var item = await inventoryService.GrantItemsAsync(userId, request.ItemTypeKey, request.Quantity);
            return Ok(new ItemOperationResponse(true, $"Granted {request.Quantity}x {request.ItemTypeKey}.", item));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Admin/System] Consume items from a player's inventory. Fails if balance is
    /// insufficient. Used for admin corrections and event systems.
    /// For crafting, use the crafting endpoint instead.
    /// </summary>
    [HttpPost("api/v1/players/{userId}/inventory/consume")]
    [Authorize]
    public async Task<IActionResult> ConsumeItems(string userId, [FromBody] ConsumeItemsRequest request)
    {
        EnsureAccess(userId);
        try
        {
            var item = await inventoryService.ConsumeItemsAsync(userId, request.ItemTypeKey, request.Quantity);
            return Ok(new ItemOperationResponse(true, $"Consumed {request.Quantity}x {request.ItemTypeKey}.", item));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private void EnsureAccess(string userId)
    {
        var authenticatedId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.Equals(authenticatedId, userId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Cannot access another player's inventory.");
    }
}
