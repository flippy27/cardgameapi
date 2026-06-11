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

    [HttpGet("database-schema")]
    [AllowAnonymous]
    public IActionResult GetDatabaseSchemaDocumentation()
    {
        return Ok(DatabaseSchemaDocumentation.Tables);
    }

    private static AuthoringLookupDto ToDto(SkillTypeDefinition item) => new(item.Id, item.Key, item.DisplayName, item.Description, item.Category, item.IconAssetRef, item.MetadataJson);
    private static AuthoringLookupDto ToDto(TriggerKindDefinition item) => new(item.Id, item.Key, item.DisplayName, item.Description, item.Category, item.IconAssetRef, item.MetadataJson);
    private static AuthoringLookupDto ToDto(TargetSelectorKindDefinition item) => new(item.Id, item.Key, item.DisplayName, item.Description, item.Category, item.IconAssetRef, item.MetadataJson);
    private static EffectKindDefinitionDto ToDto(EffectKindDefinition item) => new(item.Id, item.Key, item.DisplayName, item.Description, item.Category, item.ProducesStatusKind, item.IconAssetRef, item.MetadataJson);
    private static StatusEffectKindDefinitionDto ToDto(StatusEffectKindDefinition item) => new(item.Id, item.Key, item.DisplayName, item.Description, item.Category, item.MetadataJson);
}
