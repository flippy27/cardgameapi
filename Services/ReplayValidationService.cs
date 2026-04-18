using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Infrastructure;

namespace CardDuel.ServerApi.Services;

public interface IReplayValidationService
{
    Task<(bool IsValid, string Message)> ValidateReplayAsync(string matchId);
}

public sealed class ReplayValidationService(AppDbContext dbContext) : IReplayValidationService
{
    public async Task<(bool IsValid, string Message)> ValidateReplayAsync(string matchId)
    {
        var actions = await dbContext.MatchActions
            .Where(a => a.MatchId == matchId)
            .OrderBy(a => a.ActionNumber)
            .ToListAsync();

        if (!actions.Any())
            return (false, "No actions recorded for match");

        // Verify sequential action numbers
        for (var i = 0; i < actions.Count; i++)
        {
            if (actions[i].ActionNumber != i + 1)
                return (false, $"Action number gap at position {i}: expected {i + 1}, got {actions[i].ActionNumber}");
        }

        // Verify action data integrity (basic - check not null)
        if (actions.Any(a => string.IsNullOrWhiteSpace(a.ActionData)))
            return (false, "Action data integrity check failed: empty action data");

        // Verify timestamp ordering
        for (var i = 1; i < actions.Count; i++)
        {
            if (actions[i].CreatedAt < actions[i - 1].CreatedAt)
                return (false, $"Timestamp order violation at action {i + 1}");
        }

        return (true, "Replay validation passed");
    }

    private static string ComputeActionHash(string actionData)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(actionData));
        return string.Concat(bytes.Select(b => b.ToString("x2")));
    }
}
