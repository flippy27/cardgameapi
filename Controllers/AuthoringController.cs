using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/authoring")]
[Tags("Authoring")]
public sealed class AuthoringController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("lookups")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLookups(CancellationToken cancellationToken)
    {
        return Ok(new
        {
            skillTypes = await dbContext.SkillTypeDefinitions.AsNoTracking().OrderBy(x => x.Id).Select(x => ToDto(x)).ToListAsync(cancellationToken),
            triggerKinds = await dbContext.TriggerKindDefinitions.AsNoTracking().OrderBy(x => x.Id).Select(x => ToDto(x)).ToListAsync(cancellationToken),
            targetSelectors = await dbContext.TargetSelectorKindDefinitions.AsNoTracking().OrderBy(x => x.Id).Select(x => ToDto(x)).ToListAsync(cancellationToken),
            effectKinds = await dbContext.EffectKindDefinitions.AsNoTracking().OrderBy(x => x.Id).Select(x => ToDto(x)).ToListAsync(cancellationToken),
            statusEffectKinds = await dbContext.StatusEffectKindDefinitions.AsNoTracking().OrderBy(x => x.Id).Select(x => ToDto(x)).ToListAsync(cancellationToken)
        });
    }

    [HttpGet("effect-kinds")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<EffectKindDefinitionDto>>> GetEffectKinds(CancellationToken cancellationToken)
    {
        return Ok(await dbContext.EffectKindDefinitions.AsNoTracking().OrderBy(x => x.Id).Select(x => ToDto(x)).ToListAsync(cancellationToken));
    }

    [HttpGet("status-effect-kinds")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<StatusEffectKindDefinitionDto>>> GetStatusEffectKinds(CancellationToken cancellationToken)
    {
        return Ok(await dbContext.StatusEffectKindDefinitions.AsNoTracking().OrderBy(x => x.Id).Select(x => ToDto(x)).ToListAsync(cancellationToken));
    }

    [HttpGet("abilities/{abilityId}/presentation")]
    [AllowAnonymous]
    public async Task<ActionResult<AbilityPresentationDto>> GetAbilityPresentation(string abilityId, CancellationToken cancellationToken)
    {
        var ability = await dbContext.Abilities.AsNoTracking().FirstOrDefaultAsync(x => x.AbilityId == abilityId, cancellationToken);
        return ability == null ? NotFound(new { message = $"Ability '{abilityId}' not found" }) : Ok(ToPresentationDto(ability));
    }

    [HttpPut("abilities/{abilityId}/presentation")]
    public async Task<ActionResult<AbilityPresentationDto>> UpdateAbilityPresentation(string abilityId, [FromBody] UpsertAbilityPresentationRequest request, CancellationToken cancellationToken)
    {
        var ability = await dbContext.Abilities.FirstOrDefaultAsync(x => x.AbilityId == abilityId, cancellationToken);
        if (ability == null)
        {
            return NotFound(new { message = $"Ability '{abilityId}' not found" });
        }

        if (request.IconAssetRef != null) ability.IconAssetRef = request.IconAssetRef;
        if (request.StatusIconAssetRef != null) ability.StatusIconAssetRef = request.StatusIconAssetRef;
        if (request.AnimationCueId != null) ability.AnimationCueId = request.AnimationCueId;
        if (request.VfxCueId != null) ability.VfxCueId = request.VfxCueId;
        if (request.AudioCueId != null) ability.AudioCueId = request.AudioCueId;
        if (request.UiColorHex != null) ability.UiColorHex = request.UiColorHex;
        if (request.TooltipSummary != null) ability.TooltipSummary = request.TooltipSummary;
        if (request.MetadataJson != null) ability.MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson;
        ability.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToPresentationDto(ability));
    }

    [HttpGet("card-visual-profile-templates")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CardVisualProfileTemplateDto>>> ListCardVisualProfileTemplates(CancellationToken cancellationToken)
    {
        var templates = await dbContext.CardVisualProfileTemplates.AsNoTracking()
            .OrderBy(x => x.ProfileKey)
            .ToListAsync(cancellationToken);

        return Ok(templates.Select(ToDto).ToList());
    }

    [HttpGet("card-visual-profile-templates/{profileKey}")]
    [AllowAnonymous]
    public async Task<ActionResult<CardVisualProfileTemplateDto>> GetCardVisualProfileTemplate(string profileKey, CancellationToken cancellationToken)
    {
        var template = await dbContext.CardVisualProfileTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProfileKey == profileKey, cancellationToken);

        return template == null ? NotFound(new { message = $"Visual profile template '{profileKey}' not found" }) : Ok(ToDto(template));
    }

    [HttpPost("card-visual-profile-templates")]
    public async Task<ActionResult<CardVisualProfileTemplateDto>> CreateCardVisualProfileTemplate([FromBody] UpsertCardVisualProfileTemplateRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.CardVisualProfileTemplates.AnyAsync(x => x.ProfileKey == request.ProfileKey, cancellationToken))
        {
            return Conflict(new { message = $"Visual profile template '{request.ProfileKey}' already exists" });
        }

        var template = new CardVisualProfileTemplate
        {
            ProfileKey = request.ProfileKey,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsActive = request.IsActive,
            LayersJson = SerializeLayers(request.Layers),
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson
        };

        dbContext.CardVisualProfileTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCardVisualProfileTemplate), new { profileKey = template.ProfileKey }, ToDto(template));
    }

    [HttpPut("card-visual-profile-templates/{profileKey}")]
    public async Task<ActionResult<CardVisualProfileTemplateDto>> UpdateCardVisualProfileTemplate(string profileKey, [FromBody] UpsertCardVisualProfileTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await dbContext.CardVisualProfileTemplates.FirstOrDefaultAsync(x => x.ProfileKey == profileKey, cancellationToken);
        if (template == null)
        {
            return NotFound(new { message = $"Visual profile template '{profileKey}' not found" });
        }

        template.ProfileKey = profileKey;
        template.DisplayName = request.DisplayName;
        template.Description = request.Description;
        template.IsActive = request.IsActive;
        template.LayersJson = SerializeLayers(request.Layers);
        template.MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson;
        template.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(template));
    }

    [HttpDelete("card-visual-profile-templates/{profileKey}")]
    public async Task<IActionResult> DeleteCardVisualProfileTemplate(string profileKey, CancellationToken cancellationToken)
    {
        var template = await dbContext.CardVisualProfileTemplates.FirstOrDefaultAsync(x => x.ProfileKey == profileKey, cancellationToken);
        if (template == null)
        {
            return NotFound(new { message = $"Visual profile template '{profileKey}' not found" });
        }

        dbContext.CardVisualProfileTemplates.Remove(template);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("cards/{cardId}/visual-profile-template-assignments")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CardVisualProfileAssignmentDto>>> ListCardVisualProfileAssignments(string cardId, CancellationToken cancellationToken)
    {
        var card = await dbContext.Cards.AsNoTracking().FirstOrDefaultAsync(x => x.CardId == cardId, cancellationToken);
        if (card == null)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }

        var assignments = await dbContext.CardVisualProfileAssignments.AsNoTracking()
            .Include(x => x.Template)
            .Where(x => x.CardDefinitionId == card.Id)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Template.ProfileKey)
            .ToListAsync(cancellationToken);

        return Ok(assignments.Select(x => ToDto(card.CardId, x)).ToList());
    }

    [HttpPost("cards/{cardId}/visual-profile-template-assignments")]
    public async Task<ActionResult<IReadOnlyList<CardVisualProfileAssignmentDto>>> AssignCardVisualProfileTemplate(string cardId, [FromBody] AssignCardVisualProfileTemplateRequest request, CancellationToken cancellationToken)
    {
        var card = await dbContext.Cards.FirstOrDefaultAsync(x => x.CardId == cardId, cancellationToken);
        if (card == null)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }

        var template = await dbContext.CardVisualProfileTemplates.FirstOrDefaultAsync(x => x.ProfileKey == request.ProfileKey, cancellationToken);
        if (template == null)
        {
            return NotFound(new { message = $"Visual profile template '{request.ProfileKey}' not found" });
        }

        var assignment = await dbContext.CardVisualProfileAssignments
            .FirstOrDefaultAsync(x => x.CardDefinitionId == card.Id && x.TemplateId == template.Id, cancellationToken);

        if (request.IsDefault)
        {
            await dbContext.CardVisualProfileAssignments
                .Where(x => x.CardDefinitionId == card.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDefault, false), cancellationToken);
        }

        if (assignment == null)
        {
            assignment = new CardVisualProfileAssignment
            {
                CardDefinitionId = card.Id,
                TemplateId = template.Id
            };
            dbContext.CardVisualProfileAssignments.Add(assignment);
        }

        assignment.IsDefault = request.IsDefault;
        assignment.OverrideDisplayName = request.OverrideDisplayName;
        assignment.OverrideLayersJson = request.OverrideLayers == null ? null : SerializeLayers(request.OverrideLayers);
        assignment.MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson;
        assignment.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await MaterializeAssignedProfiles(card.Id, cancellationToken);

        return await ListCardVisualProfileAssignments(cardId, cancellationToken);
    }

    [HttpDelete("cards/{cardId}/visual-profile-template-assignments/{profileKey}")]
    public async Task<IActionResult> DeleteCardVisualProfileAssignment(string cardId, string profileKey, CancellationToken cancellationToken)
    {
        var card = await dbContext.Cards.FirstOrDefaultAsync(x => x.CardId == cardId, cancellationToken);
        if (card == null)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }

        var assignment = await dbContext.CardVisualProfileAssignments
            .Include(x => x.Template)
            .FirstOrDefaultAsync(x => x.CardDefinitionId == card.Id && x.Template.ProfileKey == profileKey, cancellationToken);

        if (assignment == null)
        {
            return NotFound(new { message = $"Visual profile assignment '{profileKey}' not found on card '{cardId}'" });
        }

        dbContext.CardVisualProfileAssignments.Remove(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await MaterializeAssignedProfiles(card.Id, cancellationToken);

        return NoContent();
    }

    [HttpGet("database-schema")]
    [AllowAnonymous]
    public IActionResult GetDatabaseSchemaDocumentation()
    {
        return Ok(DatabaseSchemaDocumentation.Tables);
    }

    private async Task MaterializeAssignedProfiles(string cardDefinitionId, CancellationToken cancellationToken)
    {
        var card = await dbContext.Cards.FirstAsync(x => x.Id == cardDefinitionId, cancellationToken);
        var assignments = await dbContext.CardVisualProfileAssignments
            .Include(x => x.Template)
            .Where(x => x.CardDefinitionId == cardDefinitionId)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Template.ProfileKey)
            .ToListAsync(cancellationToken);

        var profiles = assignments.Select(x => ToCardVisualProfileDto(x)).ToArray();
        card.VisualProfilesJson = JsonSerializer.Serialize(profiles);
        card.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static CardVisualProfileDto ToCardVisualProfileDto(CardVisualProfileAssignment assignment)
    {
        return new CardVisualProfileDto(
            assignment.Template.ProfileKey,
            assignment.OverrideDisplayName ?? assignment.Template.DisplayName,
            assignment.IsDefault,
            string.IsNullOrWhiteSpace(assignment.OverrideLayersJson)
                ? DeserializeLayers(assignment.Template.LayersJson)
                : DeserializeLayers(assignment.OverrideLayersJson));
    }

    private static CardVisualProfileAssignmentDto ToDto(string cardId, CardVisualProfileAssignment assignment)
    {
        var profile = ToCardVisualProfileDto(assignment);
        return new CardVisualProfileAssignmentDto(
            assignment.Id,
            cardId,
            assignment.TemplateId,
            profile.ProfileKey,
            profile.DisplayName,
            profile.IsDefault,
            profile.Layers,
            assignment.MetadataJson);
    }

    private static CardVisualProfileTemplateDto ToDto(CardVisualProfileTemplate template)
    {
        return new CardVisualProfileTemplateDto(
            template.Id,
            template.ProfileKey,
            template.DisplayName,
            template.Description,
            template.IsActive,
            DeserializeLayers(template.LayersJson),
            template.MetadataJson,
            template.CreatedAt,
            template.UpdatedAt);
    }

    private static AbilityPresentationDto ToPresentationDto(AbilityDefinition ability) =>
        new(
            ability.AbilityId,
            ability.DisplayName,
            ability.IconAssetRef,
            ability.StatusIconAssetRef,
            ability.AnimationCueId,
            ability.VfxCueId,
            ability.AudioCueId,
            ability.UiColorHex,
            ability.TooltipSummary,
            ability.MetadataJson);

    private static AuthoringLookupDto ToDto(SkillTypeDefinition item) => new(item.Id, item.Key, item.DisplayName, item.Description, item.Category, item.IconAssetRef, item.MetadataJson);
    private static AuthoringLookupDto ToDto(TriggerKindDefinition item) => new(item.Id, item.Key, item.DisplayName, item.Description, item.Category, item.IconAssetRef, item.MetadataJson);
    private static AuthoringLookupDto ToDto(TargetSelectorKindDefinition item) => new(item.Id, item.Key, item.DisplayName, item.Description, item.Category, item.IconAssetRef, item.MetadataJson);
    private static EffectKindDefinitionDto ToDto(EffectKindDefinition item) => new(item.Id, item.Key, item.DisplayName, item.Description, item.Category, item.ProducesStatusKind, item.IconAssetRef, item.MetadataJson);
    private static StatusEffectKindDefinitionDto ToDto(StatusEffectKindDefinition item) => new(item.Id, item.Key, item.DisplayName, item.Description, item.Category, item.IconAssetRef, item.VfxCueId, item.UiColorHex, item.MetadataJson);

    private static string SerializeLayers(IReadOnlyList<UpsertCardVisualLayerRequest> layers)
    {
        var normalized = layers.Select(layer => new CardVisualLayerDto(
            layer.Surface,
            layer.Layer,
            layer.SourceKind,
            layer.AssetRef,
            layer.SortOrder,
            string.IsNullOrWhiteSpace(layer.MetadataJson) ? "{}" : layer.MetadataJson)).ToArray();

        return JsonSerializer.Serialize(normalized);
    }

    private static IReadOnlyList<CardVisualLayerDto> DeserializeLayers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
        {
            return Array.Empty<CardVisualLayerDto>();
        }

        return JsonSerializer.Deserialize<List<CardVisualLayerDto>>(json) ?? new List<CardVisualLayerDto>();
    }
}
