using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CardDuel.ServerApi.Infrastructure.Swagger;

public sealed class CardDuelSwaggerDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new() { Name = "Auth", Description = "Register/login. Use the helper panel to store local profiles, JWTs, and common variables." },
            new() { Name = "Authoring", Description = "Fast authoring access for database docs, enum lookups, ability icons/status UI, reusable visual profile templates, and card template assignments." },
            new() { Name = "Abilities", Description = "Reusable ability/buff/debuff/status authoring. Create abilities, edit presentation, and manage ordered effects without going through a card first." },
            new() { Name = "Cards", Description = "Card catalog, authoring, abilities, effects, visual profiles, and battle presentation data." },
            new() { Name = "Decks", Description = "Player decks. Most calls require `playerId` to match the JWT subject." },
            new() { Name = "GameRulesets", Description = "Server-owned game rules and matchmaking mode assignments." },
            new() { Name = "Matchmaking", Description = "Queue/private room creation. Mode decides the ruleset; clients do not choose rulesets during queue." },
            new() { Name = "Matches", Description = "Snapshots and authoritative match actions. `battleEvents.sequence` is the animation order." },
            new() { Name = "Player Inventory", Description = "Player-owned item balances and the item type catalog used by crafting/upgrades." },
            new() { Name = "Player Cards", Description = "Player-owned card instances, collection summaries, and upgrade management." },
            new() { Name = "Crafting", Description = "Craftable card definitions plus requirement authoring and craft execution." },
            new() { Name = "Users", Description = "Profile, stats, and leaderboard." },
            new() { Name = "Replays", Description = "Replay retrieval and validation." },
            new() { Name = "Admin", Description = "Administrative operations and metrics." },
            new() { Name = "Tournaments", Description = "Prototype tournament endpoints." }
        };
    }
}
