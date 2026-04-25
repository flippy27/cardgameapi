using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Route("api/v1/abilities")]
[Tags("Abilities")]
public sealed class AbilitiesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<AbilityAuthoringDto>>> List(CancellationToken cancellationToken)
    {
        var abilities = await dbContext.Abilities.AsNoTracking()
            .Include(x => x.Effects)
            .OrderBy(x => x.AbilityId)
            .ToListAsync(cancellationToken);

        return Ok(abilities.Select(ToDto).ToArray());
    }

    [HttpGet("{abilityId}")]
    [AllowAnonymous]
    public async Task<ActionResult<AbilityAuthoringDto>> Get(string abilityId, CancellationToken cancellationToken)
    {
        var ability = await FindAbility(abilityId, track: false, cancellationToken);
        return ability == null ? NotFound(new { message = $"Ability '{abilityId}' not found" }) : Ok(ToDto(ability));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<AbilityAuthoringDto>> Create([FromBody] CreateAbilityRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.Abilities.AnyAsync(x => x.AbilityId == request.AbilityId, cancellationToken))
        {
            return Conflict(new { message = $"Ability '{request.AbilityId}' already exists" });
        }

        var ability = new AbilityDefinition
        {
            AbilityId = request.AbilityId,
            DisplayName = request.DisplayName,
            Description = request.Description,
            SkillType = request.SkillType,
            TriggerKind = request.TriggerKind,
            TargetSelectorKind = request.TargetSelectorKind,
            AnimationCueId = request.AnimationCueId ?? string.Empty,
            IconAssetRef = request.IconAssetRef,
            StatusIconAssetRef = request.StatusIconAssetRef,
            VfxCueId = request.VfxCueId,
            AudioCueId = request.AudioCueId,
            UiColorHex = request.UiColorHex,
            TooltipSummary = request.TooltipSummary,
            ConditionsJson = NormalizeJson(request.ConditionsJson),
            MetadataJson = NormalizeJson(request.MetadataJson)
        };

        foreach (var effect in request.Effects.OrderBy(x => x.Sequence))
        {
            ability.Effects.Add(new EffectDefinition
            {
                EffectKind = effect.EffectKind,
                Amount = effect.Amount,
                SecondaryAmount = effect.SecondaryAmount,
                DurationTurns = effect.DurationTurns,
                TargetSelectorKindOverride = effect.TargetSelectorKindOverride,
                Sequence = effect.Sequence,
                MetadataJson = NormalizeJson(effect.MetadataJson)
            });
        }

        dbContext.Abilities.Add(ability);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { abilityId = ability.AbilityId }, ToDto(ability));
    }

    [HttpPut("{abilityId}")]
    [Authorize]
    public async Task<ActionResult<AbilityAuthoringDto>> Update(string abilityId, [FromBody] UpdateAbilityRequest request, CancellationToken cancellationToken)
    {
        var ability = await FindAbility(abilityId, track: true, cancellationToken);
        if (ability == null)
        {
            return NotFound(new { message = $"Ability '{abilityId}' not found" });
        }

        if (request.DisplayName != null) ability.DisplayName = request.DisplayName;
        if (request.Description != null) ability.Description = request.Description;
        if (request.SkillType.HasValue) ability.SkillType = request.SkillType.Value;
        if (request.TriggerKind.HasValue) ability.TriggerKind = request.TriggerKind.Value;
        if (request.TargetSelectorKind.HasValue) ability.TargetSelectorKind = request.TargetSelectorKind.Value;
        if (request.AnimationCueId != null) ability.AnimationCueId = request.AnimationCueId;
        if (request.IconAssetRef != null) ability.IconAssetRef = request.IconAssetRef;
        if (request.StatusIconAssetRef != null) ability.StatusIconAssetRef = request.StatusIconAssetRef;
        if (request.VfxCueId != null) ability.VfxCueId = request.VfxCueId;
        if (request.AudioCueId != null) ability.AudioCueId = request.AudioCueId;
        if (request.UiColorHex != null) ability.UiColorHex = request.UiColorHex;
        if (request.TooltipSummary != null) ability.TooltipSummary = request.TooltipSummary;
        if (request.ConditionsJson != null) ability.ConditionsJson = NormalizeJson(request.ConditionsJson);
        if (request.MetadataJson != null) ability.MetadataJson = NormalizeJson(request.MetadataJson);
        ability.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(ability));
    }

    [HttpDelete("{abilityId}")]
    [Authorize]
    public async Task<IActionResult> Delete(string abilityId, CancellationToken cancellationToken)
    {
        var ability = await dbContext.Abilities
            .Include(x => x.CardAbilities)
            .FirstOrDefaultAsync(x => x.AbilityId == abilityId, cancellationToken);
        if (ability == null)
        {
            return NotFound(new { message = $"Ability '{abilityId}' not found" });
        }

        if (ability.CardAbilities.Count > 0)
        {
            return Conflict(new { message = $"Ability '{abilityId}' is attached to {ability.CardAbilities.Count} card(s). Detach it from cards before deleting." });
        }

        dbContext.Abilities.Remove(ability);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{abilityId}/effects")]
    [Authorize]
    public async Task<ActionResult<AbilityAuthoringDto>> AddEffect(string abilityId, [FromBody] CreateEffectRequest request, CancellationToken cancellationToken)
    {
        var ability = await FindAbility(abilityId, track: true, cancellationToken);
        if (ability == null)
        {
            return NotFound(new { message = $"Ability '{abilityId}' not found" });
        }

        ability.Effects.Add(new EffectDefinition
        {
            EffectKind = request.EffectKind,
            Amount = request.Amount,
            SecondaryAmount = request.SecondaryAmount,
            DurationTurns = request.DurationTurns,
            TargetSelectorKindOverride = request.TargetSelectorKindOverride,
            Sequence = request.Sequence,
            MetadataJson = NormalizeJson(request.MetadataJson)
        });
        ability.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(ability));
    }

    [HttpPut("{abilityId}/effects/{effectId}")]
    [Authorize]
    public async Task<ActionResult<AbilityAuthoringDto>> UpdateEffect(string abilityId, string effectId, [FromBody] UpdateEffectRequest request, CancellationToken cancellationToken)
    {
        var ability = await FindAbility(abilityId, track: true, cancellationToken);
        if (ability == null)
        {
            return NotFound(new { message = $"Ability '{abilityId}' not found" });
        }

        var effect = ability.Effects.FirstOrDefault(x => x.Id == effectId);
        if (effect == null)
        {
            return NotFound(new { message = $"Effect '{effectId}' not found on ability '{abilityId}'" });
        }

        if (request.EffectKind.HasValue) effect.EffectKind = request.EffectKind.Value;
        if (request.Amount.HasValue) effect.Amount = request.Amount.Value;
        if (request.SecondaryAmount.HasValue) effect.SecondaryAmount = request.SecondaryAmount.Value;
        if (request.DurationTurns.HasValue) effect.DurationTurns = request.DurationTurns.Value;
        if (request.TargetSelectorKindOverride.HasValue) effect.TargetSelectorKindOverride = request.TargetSelectorKindOverride.Value;
        if (request.Sequence.HasValue) effect.Sequence = request.Sequence.Value;
        if (request.MetadataJson != null) effect.MetadataJson = NormalizeJson(request.MetadataJson);
        ability.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(ability));
    }

    [HttpDelete("{abilityId}/effects/{effectId}")]
    [Authorize]
    public async Task<ActionResult<AbilityAuthoringDto>> DeleteEffect(string abilityId, string effectId, CancellationToken cancellationToken)
    {
        var ability = await FindAbility(abilityId, track: true, cancellationToken);
        if (ability == null)
        {
            return NotFound(new { message = $"Ability '{abilityId}' not found" });
        }

        var effect = ability.Effects.FirstOrDefault(x => x.Id == effectId);
        if (effect == null)
        {
            return NotFound(new { message = $"Effect '{effectId}' not found on ability '{abilityId}'" });
        }

        dbContext.Effects.Remove(effect);
        ability.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(ability));
    }

    private Task<AbilityDefinition?> FindAbility(string abilityId, bool track, CancellationToken cancellationToken)
    {
        var query = dbContext.Abilities
            .Include(x => x.Effects)
            .Where(x => x.AbilityId == abilityId);

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    private static AbilityAuthoringDto ToDto(AbilityDefinition ability) =>
        new(
            ability.Id,
            ability.AbilityId,
            ability.DisplayName,
            ability.Description,
            ability.SkillType,
            ability.TriggerKind,
            ability.TargetSelectorKind,
            ability.AnimationCueId,
            ability.IconAssetRef,
            ability.StatusIconAssetRef,
            ability.VfxCueId,
            ability.AudioCueId,
            ability.UiColorHex,
            ability.TooltipSummary,
            ability.ConditionsJson,
            ability.MetadataJson,
            ability.Effects.OrderBy(x => x.Sequence).Select(ToDto).ToArray());

    private static EffectDto ToDto(EffectDefinition effect) =>
        new(effect.Id, effect.EffectKind, effect.Amount, effect.SecondaryAmount, effect.DurationTurns, effect.TargetSelectorKindOverride, effect.Sequence, effect.MetadataJson);

    private static string NormalizeJson(string? json) =>
        string.IsNullOrWhiteSpace(json) ? "{}" : json;
}
