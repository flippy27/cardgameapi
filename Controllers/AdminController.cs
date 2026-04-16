using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize] // TODO: Add role-based [Authorize(Roles = "Admin")]
public sealed class AdminController(AppDbContext dbContext, ILogger<AdminController> logger) : ControllerBase
{
    [HttpPost("users/{userId}/ban")]
    public async Task<IActionResult> BanUser(string userId, [FromQuery] string? reason = null)
    {
        var user = await dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        user.IsActive = false;
        await dbContext.SaveChangesAsync();

        logger.LogWarning("User banned: {UserId}, Reason: {Reason}", userId, reason ?? "No reason provided");

        return Ok(new { message = "User banned", userId, reason });
    }

    [HttpPost("users/{userId}/unban")]
    public async Task<IActionResult> UnbanUser(string userId)
    {
        var user = await dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        user.IsActive = true;
        await dbContext.SaveChangesAsync();

        logger.LogInformation("User unbanned: {UserId}", userId);

        return Ok(new { message = "User unbanned", userId });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var totalUsers = await dbContext.Users.CountAsync();
        var activeMatches = await dbContext.Matches
            .Where(m => m.CompletedAt == null)
            .CountAsync();
        var totalMatches = await dbContext.Matches.CountAsync();
        var bannedUsers = await dbContext.Users.Where(u => !u.IsActive).CountAsync();

        var topPlayers = await dbContext.Ratings
            .OrderByDescending(r => r.RatingValue)
            .Take(5)
            .Include(r => r.User)
            .Select(r => new { r.User!.Username, r.RatingValue })
            .ToListAsync();

        return Ok(new
        {
            totalUsers,
            activeMatches,
            totalMatches,
            bannedUsers,
            topPlayers
        });
    }

    [HttpGet("user/{userId}/actions")]
    public async Task<IActionResult> GetUserActions(string userId, [FromQuery] int limit = 100)
    {
        var user = await dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var matches = await dbContext.Matches
            .Where(m => m.Player1Id == userId || m.Player2Id == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .Select(m => new
            {
                matchId = m.MatchId,
                opponent = m.Player1Id == userId ? m.Player2Id : m.Player1Id,
                result = m.WinnerId == userId ? "win" : (m.WinnerId == null ? "draw" : "loss"),
                createdAt = m.CreatedAt
            })
            .ToListAsync();

        return Ok(matches);
    }
}
