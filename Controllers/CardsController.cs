using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CardDuel.ServerApi.Contracts;
using CardDuel.ServerApi.Services;

namespace CardDuel.ServerApi.Controllers;

/// <summary>
/// Card catalog management endpoints. Create, read, update, delete cards with abilities and effects.
/// </summary>
[ApiController]
[Route("api/v1/cards")]
[Tags("Cards")]
public sealed class CardsController(
    ICardCatalogService cardCatalogService,
    ICardManagementService cardManagementService,
    IDeckRepository deckRepository
    ) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAll()
    {
        var cards = cardCatalogService.GetAll().Values.OrderBy(c => c.CardId).ToList();
        return Ok(cards);
    }

    [HttpGet("{cardId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCard(string cardId)
    {
        var card = await cardManagementService.GetCardWithAbilitiesAsync(cardId);
        if (card == null)
        {
            return NotFound(new { message = "Card not found" });
        }

        return Ok(card);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public IActionResult Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return BadRequest(new { message = "Search query must be at least 2 characters" });
        }

        var cards = cardCatalogService.GetAll().Values
            .Where(c => c.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                       c.CardId.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.DisplayName)
            .ToList();

        return Ok(cards);
    }

    [HttpGet("stats")]
    [AllowAnonymous]
    public IActionResult GetStats()
    {
        var cards = cardCatalogService.GetAll();
        return Ok(new
        {
            totalCards = cards.Count,
            manaCostAvg = cards.Values.Average(c => c.ManaCost),
            attackAvg = cards.Values.Average(c => c.Attack),
            healthAvg = cards.Values.Average(c => c.Health),
            cardsWithAbilities = cards.Values.Count(c => c.Abilities.Count > 0)
        });
    }

    // ===== ADMIN ENDPOINTS (CRUD) =====

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateCard([FromBody] CreateCardRequest request)
    {
        try
        {
            var card = await cardManagementService.CreateCardAsync(request);
            return CreatedAtAction(nameof(GetCard), new { cardId = card.CardId }, card);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{cardId}")]
    [Authorize]
    public async Task<IActionResult> UpdateCard(string cardId, [FromBody] UpdateCardRequest request)
    {
        try
        {
            var card = await cardManagementService.UpdateCardAsync(cardId, request);
            return Ok(card);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }
    }

    [HttpDelete("{cardId}")]
    [Authorize]
    public async Task<IActionResult> DeleteCard(string cardId)
    {
        var deleted = await cardManagementService.DeleteCardAsync(cardId);
        if (!deleted)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }

        return NoContent();
    }

    // ===== PRESENTATION / VISUAL AUTHORING ENDPOINTS =====

    [HttpGet("{cardId}/presentation")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBattlePresentation(string cardId)
    {
        var card = await cardManagementService.GetCardWithAbilitiesAsync(cardId);
        if (card == null)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }

        return Ok(card.BattlePresentation);
    }

    [HttpPut("{cardId}/presentation")]
    [Authorize]
    public async Task<IActionResult> UpdateBattlePresentation(string cardId, [FromBody] UpsertBattlePresentationRequest request)
    {
        try
        {
            return Ok(await cardManagementService.UpdateBattlePresentationAsync(cardId, request));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }
    }

    [HttpGet("{cardId}/visual-profiles")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVisualProfiles(string cardId)
    {
        var card = await cardManagementService.GetCardWithAbilitiesAsync(cardId);
        if (card == null)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }

        return Ok(card.VisualProfiles);
    }

    [HttpPut("{cardId}/visual-profiles")]
    [Authorize]
    public async Task<IActionResult> ReplaceVisualProfiles(string cardId, [FromBody] IReadOnlyList<UpsertCardVisualProfileRequest> request)
    {
        try
        {
            return Ok(await cardManagementService.ReplaceVisualProfilesAsync(cardId, request));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }
    }

    [HttpPut("{cardId}/visual-profiles/{profileKey}")]
    [Authorize]
    public async Task<IActionResult> UpsertVisualProfile(string cardId, string profileKey, [FromBody] UpsertCardVisualProfileRequest request)
    {
        try
        {
            return Ok(await cardManagementService.UpsertVisualProfileAsync(cardId, profileKey, request));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }
    }

    [HttpDelete("{cardId}/visual-profiles/{profileKey}")]
    [Authorize]
    public async Task<IActionResult> DeleteVisualProfile(string cardId, string profileKey)
    {
        try
        {
            var deleted = await cardManagementService.DeleteVisualProfileAsync(cardId, profileKey);
            if (!deleted)
            {
                return NotFound(new { message = $"Visual profile '{profileKey}' not found on card '{cardId}'" });
            }

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }
    }

    // ===== ABILITY ENDPOINTS =====

    [HttpPost("{cardId}/abilities")]
    [Authorize]
    public async Task<IActionResult> AddAbility(string cardId, [FromBody] CreateAbilityRequest request)
    {
        try
        {
            var ability = await cardManagementService.AddAbilityAsync(cardId, request);
            return CreatedAtAction(nameof(GetAbility), new { cardId, abilityId = ability.AbilityId }, ability);
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

    [HttpGet("{cardId}/abilities/{abilityId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAbility(string cardId, string abilityId)
    {
        var card = await cardManagementService.GetCardWithAbilitiesAsync(cardId);
        if (card == null)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }

        var ability = card.Abilities.FirstOrDefault(a => a.AbilityId == abilityId);
        if (ability == null)
        {
            return NotFound(new { message = $"Ability '{abilityId}' not found" });
        }

        return Ok(ability);
    }

    [HttpPut("{cardId}/abilities/{abilityId}")]
    [Authorize]
    public async Task<IActionResult> UpdateAbility(string cardId, string abilityId, [FromBody] UpdateAbilityRequest request)
    {
        try
        {
            var ability = await cardManagementService.UpdateAbilityAsync(cardId, abilityId, request);
            return Ok(ability);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{cardId}/abilities/{abilityId}")]
    [Authorize]
    public async Task<IActionResult> DeleteAbility(string cardId, string abilityId)
    {
        var deleted = await cardManagementService.DeleteAbilityAsync(cardId, abilityId);
        if (!deleted)
        {
            return NotFound(new { message = $"Ability '{abilityId}' not found" });
        }

        return NoContent();
    }

    // ===== EFFECT ENDPOINTS =====

    [HttpPost("{cardId}/abilities/{abilityId}/effects")]
    [Authorize]
    public async Task<IActionResult> AddEffect(string cardId, string abilityId, [FromBody] CreateEffectRequest request)
    {
        try
        {
            var effect = await cardManagementService.AddEffectAsync(cardId, abilityId, request);
            return CreatedAtAction(nameof(GetEffect), new { cardId, abilityId, effectId = effect.Id }, effect);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{cardId}/abilities/{abilityId}/effects/{effectId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEffect(string cardId, string abilityId, string effectId)
    {
        var card = await cardManagementService.GetCardWithAbilitiesAsync(cardId);
        if (card == null)
        {
            return NotFound(new { message = $"Card '{cardId}' not found" });
        }

        var ability = card.Abilities.FirstOrDefault(a => a.AbilityId == abilityId);
        if (ability == null)
        {
            return NotFound(new { message = $"Ability '{abilityId}' not found" });
        }

        var effect = ability.Effects.FirstOrDefault(e => e.Id == effectId);
        if (effect == null)
        {
            return NotFound(new { message = $"Effect '{effectId}' not found" });
        }

        return Ok(effect);
    }

    [HttpPut("{cardId}/abilities/{abilityId}/effects/{effectId}")]
    [Authorize]
    public async Task<IActionResult> UpdateEffect(string cardId, string abilityId, string effectId, [FromBody] UpdateEffectRequest request)
    {
        try
        {
            var effect = await cardManagementService.UpdateEffectAsync(cardId, abilityId, effectId, request);
            return Ok(effect);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{cardId}/abilities/{abilityId}/effects/{effectId}")]
    [Authorize]
    public async Task<IActionResult> DeleteEffect(string cardId, string abilityId, string effectId)
    {
        var deleted = await cardManagementService.DeleteEffectAsync(cardId, abilityId, effectId);
        if (!deleted)
        {
            return NotFound(new { message = $"Effect '{effectId}' not found" });
        }

        return NoContent();
    }

    // ===== PUBLIC FILTER ENDPOINTS =====

    [HttpGet("by-deck")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCardsByDeck([FromQuery] string playerid, [FromQuery] string deckid)
    {

        //  return BadRequest(new { message = "Deck must valid" });
        var deckCardIds = deckRepository.GetDeck(playerid, deckid);
        var card_ids = deckCardIds.CardIds;

        var catalog = cardCatalogService.GetAll();
        var allCards = card_ids
            .Where(cardId => catalog.ContainsKey(cardId))
            .Select(cardId => catalog[cardId])
            .ToList();

        return Ok(allCards);
    }


    [HttpGet("by-faction")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCardsByFaction([FromQuery] int faction)
    {
        if (faction < 0 || faction > 4)
        {
            return BadRequest(new { message = "Faction must be 0-4" });
        }

        var allCards = cardCatalogService.GetAll().Values
            .Where(c => (c.CardType >> 4) == faction) // Assuming faction in card data
            .OrderBy(c => c.DisplayName)
            .ToList();

        return Ok(allCards);
    }

    [HttpGet("by-type")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCardsByType([FromQuery] int cardType)
    {
        if (cardType < 0 || cardType > 3)
        {
            return BadRequest(new { message = "CardType must be 0-3" });
        }

        var allCards = cardCatalogService.GetAll().Values
            .Where(c => c.CardType == cardType)
            .OrderBy(c => c.DisplayName)
            .ToList();

        return Ok(allCards);
    }

    [HttpGet("by-rarity")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCardsByRarity([FromQuery] int rarity)
    {
        if (rarity < 0 || rarity > 3)
        {
            return BadRequest(new { message = "Rarity must be 0-3" });
        }

        var allCards = cardCatalogService.GetAll().Values
            .Where(c => c.CardRarity == rarity)
            .OrderBy(c => c.DisplayName)
            .ToList();

        return Ok(allCards);
    }

    [HttpGet("stats/by-faction")]
    [AllowAnonymous]
    public IActionResult GetStatsByFaction()
    {
        var cards = cardCatalogService.GetAll().Values.ToList();
        var stats = new List<object>();

        for (int faction = 0; faction <= 4; faction++)
        {
            var factionCards = cards.Where(c => c.CardFaction == faction).ToList();
            if (factionCards.Count == 0) continue;

            stats.Add(new
            {
                faction = faction,
                factionName = ((Game.CardFaction)faction).ToString(),
                cardCount = factionCards.Count,
                avgManaCost = factionCards.Average(c => c.ManaCost),
                avgAttack = factionCards.Average(c => c.Attack),
                avgHealth = factionCards.Average(c => c.Health),
                avgArmor = factionCards.Average(c => c.Armor)
            });
        }

        return Ok(stats);
    }

    [HttpGet("stats/by-type")]
    [AllowAnonymous]
    public IActionResult GetStatsByType()
    {
        var cards = cardCatalogService.GetAll().Values.ToList();
        var stats = new List<object>();

        for (int type = 0; type <= 3; type++)
        {
            var typeCards = cards.Where(c => c.CardType == type).ToList();
            if (typeCards.Count == 0) continue;

            stats.Add(new
            {
                type = type,
                typeName = ((Game.CardType)type).ToString(),
                cardCount = typeCards.Count,
                avgManaCost = typeCards.Average(c => c.ManaCost),
                avgAttack = typeCards.Average(c => c.Attack),
                avgHealth = typeCards.Average(c => c.Health)
            });
        }

        return Ok(stats);
    }

    [HttpGet("stats/by-rarity")]
    [AllowAnonymous]
    public IActionResult GetStatsByRarity()
    {
        var cards = cardCatalogService.GetAll().Values.ToList();
        var stats = new List<object>();

        for (int rarity = 0; rarity <= 3; rarity++)
        {
            var rarityCards = cards.Where(c => c.CardRarity == rarity).ToList();
            if (rarityCards.Count == 0) continue;

            stats.Add(new
            {
                rarity = rarity,
                rarityName = ((Game.CardRarity)rarity).ToString(),
                cardCount = rarityCards.Count,
                avgManaCost = rarityCards.Average(c => c.ManaCost),
                avgAttack = rarityCards.Average(c => c.Attack),
                avgHealth = rarityCards.Average(c => c.Health)
            });
        }

        return Ok(stats);
    }

    [HttpGet("effects")]
    [AllowAnonymous]
    public IActionResult GetEffectDefinitions()
    {
        var effects = Enum.GetValues<Game.EffectKind>()
            .Select(effect => new
            {
                kind = (int)effect,
                name = effect.ToString()
            })
            .ToList();

        return Ok(effects);
    }

    [HttpGet("triggers")]
    [AllowAnonymous]
    public IActionResult GetTriggerDefinitions()
    {
        var triggers = Enum.GetValues<Game.TriggerKind>()
            .Select(trigger => new
            {
                kind = (int)trigger,
                name = trigger.ToString()
            })
            .ToList();

        return Ok(triggers);
    }

    [HttpGet("target-selectors")]
    [AllowAnonymous]
    public IActionResult GetTargetSelectorDefinitions()
    {
        var selectors = Enum.GetValues<Game.TargetSelectorKind>()
            .Select(selector => new
            {
                kind = (int)selector,
                name = selector.ToString()
            })
            .ToList();

        return Ok(selectors);
    }

    [HttpGet("skill-types")]
    [AllowAnonymous]
    public IActionResult GetSkillTypeDefinitions()
    {
        var types = Enum.GetValues<Game.SkillType>()
            .Select(type => new
            {
                type = (int)type,
                name = type.ToString()
            })
            .ToList();

        return Ok(types);
    }

    [HttpGet("skills")]
    [AllowAnonymous]
    public IActionResult GetAllSkills()
    {
        var allCards = cardCatalogService.GetAll().Values.ToList();
        var skills = new List<object>();

        foreach (var card in allCards)
        {
            if (card.Abilities.Count == 0) continue;

            foreach (var ability in card.Abilities)
            {
                skills.Add(new
                {
                    skillId = ability.AbilityId,
                    displayName = ability.DisplayName,
                    cardId = card.CardId,
                    triggerKind = (int)ability.Trigger,
                    targetSelectorKind = (int)ability.Selector,
                    effectCount = ability.Effects.Count
                });
            }
        }

        return Ok(skills);
    }

}
