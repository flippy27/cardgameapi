using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/game-rulesets")]
public sealed class GameRulesetsController(IGameRulesetService gameRulesetService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GameRulesetSummaryDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await gameRulesetService.ListAsync(cancellationToken));
    }

    [HttpGet("default")]
    public async Task<ActionResult<GameRulesDto>> GetDefault(CancellationToken cancellationToken)
    {
        return Ok(await gameRulesetService.GetDefaultAsync(cancellationToken));
    }

    [HttpGet("{rulesetId}")]
    public async Task<ActionResult<GameRulesDto>> Get(string rulesetId, CancellationToken cancellationToken)
    {
        var ruleset = await gameRulesetService.GetAsync(rulesetId, cancellationToken);
        return ruleset == null ? NotFound() : Ok(ruleset);
    }

    [HttpPost]
    public async Task<ActionResult<GameRulesDto>> Create([FromBody] UpsertGameRulesetRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await gameRulesetService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { rulesetId = created.RulesetId }, created);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{rulesetId}")]
    public async Task<ActionResult<GameRulesDto>> Update(string rulesetId, [FromBody] UpsertGameRulesetRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await gameRulesetService.UpdateAsync(rulesetId, request, cancellationToken);
            return updated == null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("{rulesetId}/activate")]
    public async Task<ActionResult<GameRulesDto>> Activate(string rulesetId, CancellationToken cancellationToken)
    {
        var activated = await gameRulesetService.ActivateAsync(rulesetId, cancellationToken);
        return activated == null ? NotFound() : Ok(activated);
    }
}
