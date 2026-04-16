using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/replays")]
public sealed class ReplaysController(IReplayService replayService) : ControllerBase
{
    [HttpGet("{matchId}")]
    public async Task<IActionResult> GetReplay(string matchId)
    {
        try
        {
            var logs = await replayService.GetReplayAsync(matchId);
            return Ok(new { matchId, actionCount = logs.Count, actions = logs });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{matchId}/validate")]
    public async Task<IActionResult> ValidateReplay(string matchId)
    {
        var logs = await replayService.GetReplayAsync(matchId);

        var issues = new List<string>();

        if (logs.Count == 0)
        {
            issues.Add("No actions recorded");
        }

        var sequentialActions = logs.GroupBy(l => l.ActionNumber).Where(g => g.Count() > 1).ToList();
        if (sequentialActions.Any())
        {
            issues.Add($"Duplicate action numbers: {string.Join(", ", sequentialActions.Select(g => g.Key))}");
        }

        return Ok(new
        {
            matchId,
            valid = issues.Count == 0,
            actionCount = logs.Count,
            issues
        });
    }
}
