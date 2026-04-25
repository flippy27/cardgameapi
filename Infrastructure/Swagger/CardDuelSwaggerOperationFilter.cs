using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CardDuel.ServerApi.Infrastructure.Swagger;

public sealed class CardDuelSwaggerOperationFilter : IOperationFilter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = "/" + (context.ApiDescription.RelativePath ?? string.Empty).Split('?')[0];
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? "GET";
        var key = $"{method} {path}";

        operation.OperationId ??= BuildOperationId(context);

        if (EndpointDocs.TryGetValue(key, out var doc))
        {
            operation.Summary = doc.Summary;
            operation.Description = doc.Description;
        }

        ApplyAuthentication(operation, context);
        ApplyRequestExamples(operation, context, key);
        ApplyParameterDocumentation(operation, key);
        ApplyStandardResponses(operation);
    }

    private static string BuildOperationId(OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor action)
        {
            return $"{action.ControllerName}_{action.ActionName}";
        }

        return context.MethodInfo.Name;
    }

    private static void ApplyAuthentication(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = HasAttribute<AuthorizeAttribute>(context);

        if (!hasAuthorize || HasAttribute<AllowAnonymousAttribute>(context))
        {
            return;
        }

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            }
        };
    }

    private static bool HasAttribute<TAttribute>(OperationFilterContext context) where TAttribute : Attribute
    {
        return context.MethodInfo.GetCustomAttributes(true).OfType<TAttribute>().Any()
            || context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<TAttribute>().Any() == true;
    }

    private static void ApplyRequestExamples(OpenApiOperation operation, OperationFilterContext context, string key)
    {
        if (operation.RequestBody?.Content == null)
        {
            return;
        }

        var bodyType = context.ApiDescription.ParameterDescriptions
            .FirstOrDefault(parameter => parameter.Source == BindingSource.Body)
            ?.Type;

        var examples = RequestExamples.For(bodyType, key);
        if (examples.Count == 0)
        {
            return;
        }

        foreach (var media in operation.RequestBody.Content.Values)
        {
            media.Examples = examples.ToDictionary(
                pair => pair.Key,
                pair => new OpenApiExample
                {
                    Summary = pair.Value.Summary,
                    Description = pair.Value.Description,
                    Value = ToOpenApiAny(pair.Value.Value)
                });

            media.Example = ToOpenApiAny(examples.First().Value.Value);
        }
    }

    private static void ApplyParameterDocumentation(OpenApiOperation operation, string key)
    {
        foreach (var parameter in operation.Parameters)
        {
            if (ParameterDocs.TryGetValue(parameter.Name, out var description))
            {
                parameter.Description = Append(parameter.Description, description);
            }

            if (ParameterExamples.TryGetValue(parameter.Name, out var example))
            {
                parameter.Example ??= new OpenApiString(example);
            }
        }

        if (key.Contains("/snapshot/{playerId}", StringComparison.OrdinalIgnoreCase))
        {
            operation.Description = Append(operation.Description,
                "Use the same `playerId` that is inside the JWT. The snapshot is personalized: `localSeatIndex`, `isLocalTurn`, hand contents and opponent visibility depend on this player.");
        }
    }

    private static void ApplyStandardResponses(OpenApiOperation operation)
    {
        operation.Responses.TryAdd("400", new OpenApiResponse
        {
            Description = "Bad request. Usually validation, illegal game action, invalid deck, or player/token mismatch."
        });
        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Unauthorized. Login first, then authorize Swagger with the returned JWT."
        });
        operation.Responses.TryAdd("404", new OpenApiResponse
        {
            Description = "Resource not found."
        });
    }

    private static IOpenApiAny ToOpenApiAny(object value)
    {
        return OpenApiAnyFactory.CreateFromJson(JsonSerializer.Serialize(value, JsonOptions));
    }

    private static string Append(string? current, string addition)
    {
        return string.IsNullOrWhiteSpace(current)
            ? addition
            : $"{current}\n\n{addition}";
    }

    private sealed record EndpointDoc(string Summary, string Description);

    private static readonly Dictionary<string, EndpointDoc> EndpointDocs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["POST /api/v1/auth/login"] = new(
            "Login",
            "Validates an existing account and returns the JWT plus the real `userId`. Swagger captures this response automatically, stores the token in the active local profile, and authorizes subsequent requests."),
        ["POST /api/v1/auth/register"] = new(
            "Register player",
            "Creates a new player account, creates the initial rating row, returns a JWT, and auto-authorizes Swagger with the newly created user."),
        ["GET /api/v1/authoring/database-schema"] = new(
            "Database schema guide",
            "Human-readable documentation for important authoring tables and columns. Use this when a DB column is unclear, especially enum ids, visual templates, ability icons, status indicators, and materialized client-facing JSON."),
        ["GET /api/v1/authoring/lookups"] = new(
            "All authoring lookup tables",
            "Returns skill types, trigger kinds, target selectors, effect kinds, and status effect kinds in one response. This is the fastest way to understand what numeric ids in DB mean."),
        ["GET /api/v1/authoring/effect-kinds"] = new(
            "Effect kind definitions",
            "Documents every server effect kind id, readable key, category, produced status, default icon, and description. `effects.effect_kind` has a FK here."),
        ["GET /api/v1/authoring/status-effect-kinds"] = new(
            "Status/buff/debuff definitions",
            "Documents runtime status indicators such as Poisoned, Stunned, Shielded, and Enrage Cooldown, including icon/VFX/color metadata for UI authoring."),
        ["GET /api/v1/abilities"] = new(
            "List abilities",
            "Dedicated ability authoring tab. Lists reusable ability definitions with presentation fields and ordered effects, independent from any one card."),
        ["GET /api/v1/abilities/{abilityId}"] = new(
            "Get ability",
            "Reads one reusable ability definition, including skill type, trigger, target selector, icons, tooltip metadata, and effect rows."),
        ["POST /api/v1/abilities"] = new(
            "Create reusable ability",
            "Creates a reusable server-authoritative ability. Attach it to cards through the card ability endpoints after it exists."),
        ["PUT /api/v1/abilities/{abilityId}"] = new(
            "Update reusable ability",
            "Updates ability rules/presentation metadata without changing card assignments. Existing attached cards use the updated ability definition."),
        ["DELETE /api/v1/abilities/{abilityId}"] = new(
            "Delete reusable ability",
            "Deletes an ability only if it is not attached to cards. This prevents silently breaking card definitions."),
        ["POST /api/v1/abilities/{abilityId}/effects"] = new(
            "Add ability effect",
            "Adds one ordered effect row to a reusable ability. Use lookup endpoints for readable effect kind and target selector ids."),
        ["PUT /api/v1/abilities/{abilityId}/effects/{effectId}"] = new(
            "Update ability effect",
            "Updates one effect row on a reusable ability, including kind, amount, duration, target override, sequence, and metadata."),
        ["DELETE /api/v1/abilities/{abilityId}/effects/{effectId}"] = new(
            "Delete ability effect",
            "Removes one effect row from a reusable ability."),
        ["GET /api/v1/authoring/abilities/{abilityId}/presentation"] = new(
            "Get ability UI presentation",
            "Returns authoring metadata for ability badges/icons/tooltips/status icons. This does not change runtime snapshot shape."),
        ["PUT /api/v1/authoring/abilities/{abilityId}/presentation"] = new(
            "Update ability UI presentation",
            "Updates icons, tooltip, UI color, animation/VFX/audio cues, and metadata for an ability. Use this for ability badges and status/debuff icon association."),
        ["GET /api/v1/authoring/card-visual-profile-templates"] = new(
            "List reusable card visual profile templates",
            "Templates are independent from cards. Examples: `hand-default`, `played-default`, `hand-full-art`, `reward-legendary`, `decklist-minimal`. Assigning one materializes client-facing `visualProfiles` on a card."),
        ["GET /api/v1/authoring/card-visual-profile-templates/{profileKey}"] = new(
            "Get reusable card visual profile template",
            "Reads one reusable visual template by profile key."),
        ["POST /api/v1/authoring/card-visual-profile-templates"] = new(
            "Create reusable card visual profile template",
            "Creates a profile template independent from any card. This avoids duplicating common layer stacks like played-normal, played-premium, full-art, reward, inspect, or decklist."),
        ["PUT /api/v1/authoring/card-visual-profile-templates/{profileKey}"] = new(
            "Update reusable card visual profile template",
            "Replaces one reusable template. Already assigned cards are not silently rematerialized until you reassign/update the card assignment, keeping changes deliberate."),
        ["DELETE /api/v1/authoring/card-visual-profile-templates/{profileKey}"] = new(
            "Delete reusable card visual profile template",
            "Deletes a template and its assignments. Use carefully because assigned card materialized JSON may need cleanup/reassignment."),
        ["GET /api/v1/authoring/cards/{cardId}/visual-profile-template-assignments"] = new(
            "List card visual template assignments",
            "Shows which reusable visual templates are assigned to a card and what layers will be materialized to the card catalog."),
        ["POST /api/v1/authoring/cards/{cardId}/visual-profile-template-assignments"] = new(
            "Assign visual template to card",
            "Assigns a reusable visual profile template to a card, optionally with per-card layer overrides, then materializes the card's client-facing `visualProfiles` without changing runtime contract."),
        ["DELETE /api/v1/authoring/cards/{cardId}/visual-profile-template-assignments/{profileKey}"] = new(
            "Delete card visual template assignment",
            "Removes one template assignment from a card and rematerializes the card's `visualProfiles`."),
        ["GET /api/v1/cards"] = new(
            "List card catalog",
            "Returns every server card definition currently loaded from the database/catalog, including gameplay stats, abilities, battle presentation, and visual profiles."),
        ["GET /api/v1/cards/{cardId}"] = new(
            "Get one card",
            "Returns a single card definition with all attached abilities/effects and presentation metadata."),
        ["GET /api/v1/cards/search"] = new(
            "Search cards",
            "Finds cards by display name or card id. Useful before building a deck or authoring examples."),
        ["GET /api/v1/cards/stats"] = new(
            "Card catalog statistics",
            "Returns aggregate card counts and averages for quick sanity checks after seeding or authoring cards."),
        ["POST /api/v1/matchmaking/queue"] = new(
            "Enter casual or ranked matchmaking",
            "The server resolves the ruleset from `mode`; clients should not send a ruleset id here. Use `Casual` or `Ranked`, never `Private`. The response contains `matchId`, `reconnectToken`, `seatIndex`, and the authoritative `rules`."),
        ["POST /api/v1/matchmaking/private"] = new(
            "Create a private match room",
            "Creates a room using the ruleset assigned to `Private` mode. Share `roomCode` with the second player."),
        ["POST /api/v1/matchmaking/private/join"] = new(
            "Join an existing private room",
            "Requires the joining player's JWT, their real `playerId`, a valid deck, and the room code returned by create-private."),
        ["GET /api/v1/matches/{matchId}/snapshot/{playerId}"] = new(
            "Read a player-local match snapshot",
            "Returns the server-authoritative match state from this player's perspective. `battleEvents` are ordered by `sequence`; Unity should animate in that order and then reconcile to the final snapshot."),
        ["GET /api/v1/matches/{matchId}/rules/{playerId}"] = new(
            "Read match rules snapshot",
            "Returns the immutable rules snapshot recorded for this match, so clients and replay tools know exactly which HP/mana/draw/handicap rules were used."),
        ["POST /api/v1/matches/{matchId}/ready"] = new(
            "Set ready state",
            "Marks a player as ready or not ready for a match flow. The response is the updated authoritative match snapshot."),
        ["POST /api/v1/matches/{matchId}/play"] = new(
            "Play a card from hand",
            "Uses `runtimeHandKey` from the latest snapshot. The server owns placement, mana validation, row shifting, and final board state."),
        ["POST /api/v1/matches/{matchId}/end-turn"] = new(
            "End the active player's turn",
            "Triggers the server battle phase when appropriate. Illegal turn ownership returns a parseable `GameActionErrorDto`."),
        ["PUT /api/v1/decks"] = new(
            "Create or replace a player's deck",
            "The `playerId` must match the JWT subject. Deck validation is server-side and checks card ids/counts."),
        ["GET /api/v1/decks/catalog"] = new(
            "Deck-building card catalog",
            "Returns all cards available for deck construction."),
        ["GET /api/v1/decks/{playerId}"] = new(
            "List player decks",
            "Returns all saved decks for the selected player."),
        ["GET /api/v1/decks/{playerId}/{deckId}"] = new(
            "Get deck with normalized entries",
            "Returns one deck with `cards`, where each entry is one card copy row from `deck_cards`. Use `entryId` to remove a specific copy."),
        ["POST /api/v1/decks/{playerId}/{deckId}/cards"] = new(
            "Add one card to deck",
            "Adds a single card id to a deck at an optional position. This is the Swagger-friendly path so you do not have to edit a giant card id array."),
        ["DELETE /api/v1/decks/{playerId}/{deckId}/cards/{entryId}"] = new(
            "Remove one card copy from deck",
            "Removes one specific deck_cards row by `entryId`, then compacts deck order."),
        ["POST /api/v1/cards"] = new(
            "Create a card definition",
            "Admin-style card authoring endpoint. Supports server-authoritative gameplay stats, battle presentation metadata, visual profiles, abilities, and effects."),
        ["PUT /api/v1/cards/{cardId}"] = new(
            "Update a card definition",
            "Partially updates gameplay stats, presentation metadata, and visual profiles for an existing card."),
        ["DELETE /api/v1/cards/{cardId}"] = new(
            "Delete a card definition",
            "Removes a card definition from the catalog. Use carefully because decks and tests may reference stable card ids."),
        ["GET /api/v1/cards/{cardId}/presentation"] = new(
            "Get card battle presentation",
            "Returns the server-authored attack presentation hints for one card: motion intensity, shake intensity, delivery family, impact VFX, audio cue, and optional metadata JSON."),
        ["PUT /api/v1/cards/{cardId}/presentation"] = new(
            "Update card battle presentation",
            "Updates only the attack presentation block of a card. Use this when authoring attack animations/VFX/audio without touching gameplay stats or visual profiles."),
        ["GET /api/v1/cards/{cardId}/visual-profiles"] = new(
            "Get card visual profiles",
            "Returns all server-authored visual profiles for one card. The client uses these layers to compose hand cards, played cards, inspect views, reward cards, decklist thumbnails, and future surfaces."),
        ["PUT /api/v1/cards/{cardId}/visual-profiles"] = new(
            "Replace all card visual profiles",
            "Replaces the entire visual profile list for a card. Use this for bulk art authoring, imports, or when moving a card to a fully new visual setup."),
        ["PUT /api/v1/cards/{cardId}/visual-profiles/{profileKey}"] = new(
            "Upsert one card visual profile",
            "Creates or replaces one visual profile by `profileKey` while preserving the other profiles. This is the easiest endpoint for adding art from Swagger."),
        ["DELETE /api/v1/cards/{cardId}/visual-profiles/{profileKey}"] = new(
            "Delete one card visual profile",
            "Removes one visual profile from a card. If the deleted profile was the default, the server promotes the first remaining profile to default."),
        ["POST /api/v1/cards/{cardId}/abilities"] = new(
            "Attach an ability to a card",
            "Abilities are evaluated by trigger order in the match engine. Use `sequence` inside effects to control deterministic execution and animation order."),
        ["GET /api/v1/cards/{cardId}/abilities/{abilityId}"] = new(
            "Get card ability",
            "Returns one ability attached to a specific card, including its effect list."),
        ["PUT /api/v1/cards/{cardId}/abilities/{abilityId}"] = new(
            "Update card ability",
            "Updates ability metadata such as display text, trigger, selector, animation cue, conditions JSON, and metadata JSON."),
        ["DELETE /api/v1/cards/{cardId}/abilities/{abilityId}"] = new(
            "Delete card ability",
            "Removes one ability and its effects from a card."),
        ["POST /api/v1/cards/{cardId}/abilities/{abilityId}/effects"] = new(
            "Attach an effect to an ability",
            "Effects are composable units. Use `targetSelectorKindOverride`, `durationTurns`, and `metadataJson` for flexible server-authoritative behavior."),
        ["GET /api/v1/cards/{cardId}/abilities/{abilityId}/effects/{effectId}"] = new(
            "Get ability effect",
            "Returns one effect row attached to a card ability."),
        ["PUT /api/v1/cards/{cardId}/abilities/{abilityId}/effects/{effectId}"] = new(
            "Update ability effect",
            "Updates the operation, amount, duration, target override, sequence, or metadata of an ability effect."),
        ["DELETE /api/v1/cards/{cardId}/abilities/{abilityId}/effects/{effectId}"] = new(
            "Delete ability effect",
            "Removes one effect from an ability."),
        ["POST /api/v1/game-rulesets"] = new(
            "Create a game ruleset",
            "Rulesets define match-level behavior such as HP, mana, draw counts, turn start seat, and per-seat handicaps."),
        ["GET /api/v1/game-rulesets"] = new(
            "List rulesets",
            "Returns lightweight summaries of all configured game rulesets."),
        ["GET /api/v1/game-rulesets/default"] = new(
            "Get default ruleset",
            "Returns the active default ruleset used when no more specific mode mapping is available."),
        ["GET /api/v1/game-rulesets/{rulesetId}"] = new(
            "Get ruleset",
            "Returns the full ruleset including mana, HP, draw, starting seat, and seat override values."),
        ["PUT /api/v1/game-rulesets/{rulesetId}"] = new(
            "Update ruleset",
            "Replaces the editable fields of a ruleset and its seat overrides."),
        ["POST /api/v1/game-rulesets/{rulesetId}/activate"] = new(
            "Activate ruleset",
            "Marks a ruleset active so it can be assigned to matchmaking modes."),
        ["GET /api/v1/game-rulesets/matchmaking-modes"] = new(
            "List mode ruleset assignments",
            "Shows which ruleset the server will use for Casual, Ranked, and Private modes."),
        ["PUT /api/v1/game-rulesets/matchmaking-modes/{mode}"] = new(
            "Assign a ruleset to a matchmaking mode",
            "This is how the server decides which ruleset queue/private modes use. Queue requests should only send `mode`; this mapping controls the rules."),
        ["GET /api/v1/users/{playerId}/profile"] = new(
            "Get player profile",
            "Returns identity and rating summary for the authenticated player."),
        ["GET /api/v1/users/{playerId}/stats"] = new(
            "Get player stats",
            "Returns rating, wins/losses, total games, win rate, and region for the authenticated player."),
        ["GET /api/v1/users/leaderboard"] = new(
            "Get leaderboard",
            "Returns paginated player rating entries ordered by rating for a region."),
        ["GET /api/v1/replays/{matchId}"] = new(
            "Get replay actions",
            "Returns persisted action logs for a completed or active match."),
        ["GET /api/v1/replays/{matchId}/validate"] = new(
            "Validate replay actions",
            "Runs simple consistency checks over replay action logs and reports issues."),
        ["GET /api/v1/admin/dashboard"] = new(
            "Admin dashboard",
            "Returns operational summary counts such as users, active matches, total matches, banned users, and top players."),
        ["GET /api/v1/admin/metrics"] = new(
            "Admin metrics summary",
            "Returns in-memory middleware metrics as JSON."),
        ["POST /api/v1/admin/metrics/reset"] = new(
            "Reset admin metrics",
            "Clears the in-memory middleware metrics counters."),
        ["GET /api/v1/tournaments"] = new(
            "List tournaments",
            "Returns prototype in-memory tournament records."),
        ["POST /api/v1/tournaments"] = new(
            "Create tournament",
            "Creates an in-memory tournament with display name, start time, and max player count."),
        ["POST /api/v1/tournaments/{tournamentId}/register"] = new(
            "Register in tournament",
            "Registers the authenticated player in an existing in-memory tournament.")
    };

    private static readonly Dictionary<string, string> ParameterDocs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["playerId"] = "Must match the authenticated JWT subject. Login response gives you the correct value.",
        ["matchId"] = "Match id returned by matchmaking or private match creation.",
        ["cardId"] = "Stable card definition id, for example `card_001` or a custom authored id.",
        ["profileKey"] = "Stable visual profile key, for example `hand-default`, `played-premium`, `full-art`, `reward-legendary`, or `inspect-gallery`.",
        ["abilityId"] = "Stable ability id attached to the card, for example `poison` or `regenerate_left`.",
        ["effectId"] = "Database id of the effect row.",
        ["entryId"] = "Deck card entry id returned by `GET /api/v1/decks/{playerId}/{deckId}`. This identifies one card copy, not the card definition.",
        ["rulesetId"] = "Ruleset id returned by ruleset create/list endpoints.",
        ["mode"] = "Queue mode enum: 0 Casual, 1 Ranked, 2 Private.",
        ["deckid"] = "Deck id owned by `playerid`, for example `deck_playerone_1`.",
        ["playerid"] = "Deck owner id. This endpoint uses lowercase query parameter names.",
        ["q"] = "Search text. Minimum 2 characters."
    };

    private static readonly Dictionary<string, string> ParameterExamples = new(StringComparer.OrdinalIgnoreCase)
    {
        ["playerId"] = "{{playerId}}",
        ["matchId"] = "{{matchId}}",
        ["roomCode"] = "{{roomCode}}",
        ["deckid"] = "{{deckId}}",
        ["playerid"] = "{{playerId}}",
        ["deckId"] = "{{deckId}}",
        ["cardId"] = "{{cardId}}",
        ["abilityId"] = "{{abilityId}}",
        ["entryId"] = "{{deckEntryId}}",
        ["profileKey"] = "{{profileKey}}",
        ["rulesetId"] = "{{rulesetId}}",
        ["q"] = "monk"
    };

    private sealed record ExampleDefinition(string Summary, string Description, object Value);

    private static class RequestExamples
    {
        public static IReadOnlyDictionary<string, ExampleDefinition> For(Type? bodyType, string key)
        {
            if (bodyType == null)
            {
                return new Dictionary<string, ExampleDefinition>();
            }

            var name = bodyType.Name;
            if (key.EndsWith("/visual-profiles", StringComparison.OrdinalIgnoreCase))
            {
                return Examples(
                    ("handPlayedBaseline", "Hand + played baseline", "Replaces all profiles with a normal hand profile and a played-board profile.", new[]
                    {
                        new
                        {
                            profileKey = "hand-default",
                            displayName = "Hand Default",
                            isDefault = true,
                            layers = new[]
                            {
                                new { surface = "hand", layer = "bg", sourceKind = "sprite", assetRef = "card/bg/common_blue", sortOrder = 0, metadataJson = "{\"theme\":\"neutral\"}" },
                                new { surface = "hand", layer = "frame", sourceKind = "sprite", assetRef = "card/frame/common_metal", sortOrder = 10, metadataJson = "{\"rarity\":\"common\"}" },
                                new { surface = "hand", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}", sortOrder = 20, metadataJson = "{\"crop\":\"portrait\"}" },
                                new { surface = "hand", layer = "nameplate", sourceKind = "sprite", assetRef = "card/ui/nameplate_default", sortOrder = 30, metadataJson = "{}" }
                            }
                        },
                        new
                        {
                            profileKey = "played-default",
                            displayName = "Played Default",
                            isDefault = false,
                            layers = new[]
                            {
                                new { surface = "played", layer = "frame", sourceKind = "sprite", assetRef = "board/frame/common_metal", sortOrder = 0, metadataJson = "{}" },
                                new { surface = "played", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}", sortOrder = 10, metadataJson = "{\"crop\":\"board-close\"}" }
                            }
                        }
                    }),
                    ("completeUiSurfaces", "Complete UI surface set", "Example with hand, played, inspect, reward and decklist surfaces in one replace operation.", new[]
                    {
                        new
                        {
                            profileKey = "premium-hand",
                            displayName = "Premium Hand",
                            isDefault = true,
                            layers = new[]
                            {
                                new { surface = "hand", layer = "bg", sourceKind = "sprite", assetRef = "card/bg/premium_dark", sortOrder = 0, metadataJson = "{}" },
                                new { surface = "hand", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}_premium", sortOrder = 10, metadataJson = "{\"fullArt\":true,\"premium\":true}" },
                                new { surface = "hand", layer = "frame", sourceKind = "sprite", assetRef = "card/frame/premium_reactive", sortOrder = 20, metadataJson = "{\"reactive\":\"mousemove\"}" },
                                new { surface = "hand", layer = "foil", sourceKind = "sprite", assetRef = "card/fx/foil_reactive_gold", sortOrder = 30, metadataJson = "{\"scrollSpeed\":0.4,\"mask\":\"diagonal\"}" }
                            }
                        },
                        new
                        {
                            profileKey = "played-premium",
                            displayName = "Played Premium",
                            isDefault = false,
                            layers = new[]
                            {
                                new { surface = "played", layer = "frame", sourceKind = "sprite", assetRef = "board/frame/premium_reactive", sortOrder = 0, metadataJson = "{}" },
                                new { surface = "played", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}_premium", sortOrder = 10, metadataJson = "{\"crop\":\"tight\"}" },
                                new { surface = "played", layer = "slot-aura", sourceKind = "sprite", assetRef = "board/fx/premium_slot_aura", sortOrder = 20, metadataJson = "{\"pulseOnAttack\":true}" }
                            }
                        },
                        new
                        {
                            profileKey = "inspect-gallery",
                            displayName = "Inspect Gallery",
                            isDefault = false,
                            layers = new[]
                            {
                                new { surface = "inspect", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}_gallery", sortOrder = 0, metadataJson = "{\"zoom\":\"free\",\"pan\":\"slow\"}" },
                                new { surface = "inspect", layer = "title-ornament", sourceKind = "sprite", assetRef = "card/ui/inspect_ornament_legendary", sortOrder = 10, metadataJson = "{}" }
                            }
                        },
                        new
                        {
                            profileKey = "reward-card",
                            displayName = "Reward Card",
                            isDefault = false,
                            layers = new[]
                            {
                                new { surface = "reward", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}_reward", sortOrder = 0, metadataJson = "{\"fullBleed\":true}" },
                                new { surface = "reward", layer = "frame", sourceKind = "sprite", assetRef = "card/frame/reward_gold", sortOrder = 10, metadataJson = "{\"shine\":\"slow\"}" },
                                new { surface = "reward", layer = "burst", sourceKind = "sprite", assetRef = "card/fx/reward_burst_orange", sortOrder = 20, metadataJson = "{}" }
                            }
                        },
                        new
                        {
                            profileKey = "decklist-minimal",
                            displayName = "Decklist Minimal",
                            isDefault = false,
                            layers = new[]
                            {
                                new { surface = "decklist", layer = "thumbnail", sourceKind = "image", assetRef = "card/thumb/{{cardId}}", sortOrder = 0, metadataJson = "{\"shape\":\"square\"}" },
                                new { surface = "decklist", layer = "rarity-pip", sourceKind = "sprite", assetRef = "card/icon/rarity_epic", sortOrder = 10, metadataJson = "{}" }
                            }
                        }
                    }));
            }

            return name switch
            {
                "LoginRequest" => Examples(
                    ("playerOne", "Seeded Player One", "Example local PlayerOne login.", new { email = "playerone@flippy.com", password = "123456" }),
                    ("playerTwo", "Seeded Player Two", "Example local PlayerTwo login.", new { email = "playertwo@flippy.com", password = "123456" }),
                    ("custom", "Custom profile", "Replace with any registered user.", new { email = "you@example.com", password = "your-password" })),
                "RegisterRequest" => Examples(
                    ("newPlayer", "Create a fresh test player", "Registers and immediately returns a usable JWT.", new { email = "swagger.player@example.com", username = "SwaggerPlayer", password = "ChangeMe123!" })),
                "UpsertAbilityPresentationRequest" => Examples(
                    ("poisonBadge", "Poison ability badge", "Ability icon plus status indicator metadata for poison.", new { iconAssetRef = "abilities/poison", statusIconAssetRef = "status/poisoned", animationCueId = "skill_poison_strike", vfxCueId = "vfx_poison_splash", audioCueId = "sfx_poison_apply", uiColorHex = "#62B357", tooltipSummary = "Poisons damaged enemies. Poison ticks during battle-start processing.", metadataJson = "{\"indicatorPriority\":20,\"badgeGroup\":\"debuff\"}" }),
                    ("shieldBadge", "Shield ability badge", "Ability icon plus shield buff indicator metadata.", new { iconAssetRef = "abilities/shield", statusIconAssetRef = "status/shielded", animationCueId = "skill_shield_pulse", vfxCueId = "vfx_shield_apply", audioCueId = "sfx_shield", uiColorHex = "#61A8FF", tooltipSummary = "Adds shield charges that block incoming damage.", metadataJson = "{\"indicatorPriority\":10,\"badgeGroup\":\"buff\"}" }),
                    ("stunBadge", "Stun ability badge", "Ability icon plus stun debuff indicator metadata.", new { iconAssetRef = "abilities/stun", statusIconAssetRef = "status/stunned", animationCueId = "skill_stun_hit", vfxCueId = "vfx_stun_apply", audioCueId = "sfx_stun", uiColorHex = "#F2D14B", tooltipSummary = "Stunned cards skip their next attack.", metadataJson = "{\"indicatorPriority\":30,\"badgeGroup\":\"debuff\"}" })),
                "UpsertCardVisualProfileTemplateRequest" => Examples(
                    ("playedDefault", "Reusable played-normal template", "Generic played-board frame/art stack, independent from any specific card.", new { profileKey = "played-default", displayName = "Played Default", description = "Normal board card composition: frame plus board-cropped art.", isActive = true, layers = new[] { new { surface = "played", layer = "frame", sourceKind = "sprite", assetRef = "board/frame/common_metal", sortOrder = 0, metadataJson = "{}" }, new { surface = "played", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}", sortOrder = 10, metadataJson = "{\"crop\":\"board-close\"}" } }, metadataJson = "{\"surface\":\"played\",\"rarity\":\"common\"}" }),
                    ("handDefault", "Reusable hand-normal template", "Generic hand card stack with bg, frame, art, nameplate, rules area.", new { profileKey = "hand-default", displayName = "Hand Default", description = "Baseline hand card composition for most non-premium cards.", isActive = true, layers = new[] { new { surface = "hand", layer = "bg", sourceKind = "sprite", assetRef = "card/bg/common_blue", sortOrder = 0, metadataJson = "{}" }, new { surface = "hand", layer = "frame", sourceKind = "sprite", assetRef = "card/frame/common_metal", sortOrder = 10, metadataJson = "{\"rarity\":\"common\"}" }, new { surface = "hand", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}", sortOrder = 20, metadataJson = "{\"crop\":\"portrait\"}" }, new { surface = "hand", layer = "nameplate", sourceKind = "sprite", assetRef = "card/ui/nameplate_default", sortOrder = 30, metadataJson = "{}" }, new { surface = "hand", layer = "rules", sourceKind = "sprite", assetRef = "card/ui/rules_text_default", sortOrder = 40, metadataJson = "{\"font\":\"serif-small\"}" } }, metadataJson = "{\"surface\":\"hand\"}" }),
                    ("fullArt", "Reusable full-art hand template", "Full-art hand card stack with art, legendary frame and foil.", new { profileKey = "hand-full-art", displayName = "Hand Full Art", description = "Full-bleed art composition for premium/legendary cards.", isActive = true, layers = new[] { new { surface = "hand", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}_full", sortOrder = 0, metadataJson = "{\"fullArt\":true,\"safeTextTop\":0.14,\"safeTextBottom\":0.22}" }, new { surface = "hand", layer = "frame", sourceKind = "sprite", assetRef = "card/frame/legendary_fullart", sortOrder = 20, metadataJson = "{\"rarity\":\"legendary\"}" }, new { surface = "hand", layer = "foil", sourceKind = "sprite", assetRef = "card/fx/foil_rainbow_soft", sortOrder = 30, metadataJson = "{\"scrollSpeed\":0.15}" } }, metadataJson = "{\"surface\":\"hand\",\"premium\":true}" }),
                    ("statusIconFamily", "Reusable status indicator visual family", "Template-like example for status/buff/debuff icon surfaces if you later choose to render status cards/badges through the same layer system.", new { profileKey = "status-indicator-default", displayName = "Status Indicator Default", description = "Small status/buff/debuff indicator composition.", isActive = true, layers = new[] { new { surface = "status", layer = "icon-bg", sourceKind = "sprite", assetRef = "status/bg/default", sortOrder = 0, metadataJson = "{}" }, new { surface = "status", layer = "icon", sourceKind = "sprite", assetRef = "status/{{statusKey}}", sortOrder = 10, metadataJson = "{\"size\":\"small\"}" }, new { surface = "status", layer = "duration-badge", sourceKind = "sprite", assetRef = "status/ui/duration_badge", sortOrder = 20, metadataJson = "{\"visibleWhen\":\"remainingTurns>0\"}" } }, metadataJson = "{\"surface\":\"status\",\"note\":\"authoring helper; runtime statuses still come from snapshot\"}" })),
                "AssignCardVisualProfileTemplateRequest" => Examples(
                    ("assignPlayedDefault", "Assign played-normal", "Assigns reusable played-default to current card and materializes it into card.visualProfiles.", new { profileKey = "played-default", isDefault = false, overrideDisplayName = (string?)null, overrideLayers = (object?)null, metadataJson = "{\"source\":\"swagger\"}" }),
                    ("assignHandDefaultAsDefault", "Assign hand-default as default", "Assigns reusable hand-default and marks it default for the card.", new { profileKey = "hand-default", isDefault = true, overrideDisplayName = "Default Hand Art", overrideLayers = (object?)null, metadataJson = "{\"source\":\"swagger\",\"defaultSurface\":\"hand\"}" }),
                    ("assignWithCardSpecificArtOverride", "Assign with per-card art override", "Uses template concept but overrides layers for one special card art path/crop.", new { profileKey = "hand-full-art", isDefault = true, overrideDisplayName = "Special Full Art", overrideLayers = new[] { new { surface = "hand", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}_special_full", sortOrder = 0, metadataJson = "{\"fullArt\":true,\"crop\":\"hero\"}" }, new { surface = "hand", layer = "frame", sourceKind = "sprite", assetRef = "card/frame/legendary_fullart", sortOrder = 20, metadataJson = "{\"rarity\":\"legendary\"}" }, new { surface = "hand", layer = "foil", sourceKind = "sprite", assetRef = "card/fx/foil_legendary_prism", sortOrder = 30, metadataJson = "{\"rainbow\":true}" } }, metadataJson = "{\"source\":\"swagger\",\"overrideReason\":\"special art crop\"}" })),
                "UpsertBattlePresentationRequest" => Examples(
                    ("meleeHeavy", "Heavy melee slam", "Slow melee impact with strong shake and heavy hit VFX/audio.", new { attackMotionLevel = 4, attackShakeLevel = 5, attackDeliveryType = "melee", impactFxId = "impact_hammer_heavy", attackAudioCueId = "sfx_hammer_heavy", metadataJson = "{\"cameraProfile\":\"boss-close\",\"hitStopMs\":140}" }),
                    ("fastProjectile", "Fast projectile", "Arrow/dagger style ranged attack.", new { attackMotionLevel = 2, attackShakeLevel = 1, attackDeliveryType = "projectile", impactFxId = "impact_arrow_light", attackAudioCueId = "sfx_arrow_light", metadataJson = "{\"projectileSpeed\":\"fast\",\"trail\":\"short\"}" }),
                    ("arcLob", "Arc / lob attack", "Curved projectile useful for fireballs, vines, thrown bombs, or mortar-like attacks.", new { attackMotionLevel = 3, attackShakeLevel = 4, attackDeliveryType = "arc", impactFxId = "impact_fire_arc", attackAudioCueId = "sfx_fire_arc", metadataJson = "{\"projectileCurve\":\"lob_high\",\"color\":\"#FF8A33FF\"}" }),
                    ("beamMagic", "Beam / channel attack", "Continuous beam presentation with longer trail/impact cues.", new { attackMotionLevel = 5, attackShakeLevel = 2, attackDeliveryType = "beam", impactFxId = "impact_arcane_beam", attackAudioCueId = "sfx_arcane_beam", metadataJson = "{\"trail\":\"long\",\"color\":\"#66D9FFFF\",\"channelMs\":650}" }),
                    ("fallback", "Safe fallback", "Neutral values that are easy for the client to support when custom art is missing.", new { attackMotionLevel = 0, attackShakeLevel = 0, attackDeliveryType = "melee", impactFxId = "impact_default", attackAudioCueId = "sfx_default_slash", metadataJson = "{\"fallback\":true}" })),
                "UpsertCardVisualProfileRequest" => Examples(
                    ("simpleHand", "Simple hand card", "Minimal hand-card composition: background, frame, art, text plate.", new
                    {
                        profileKey = "hand-default",
                        displayName = "Hand Default",
                        isDefault = true,
                        layers = new[]
                        {
                            new { surface = "hand", layer = "bg", sourceKind = "sprite", assetRef = "card/bg/common_blue", sortOrder = 0, metadataJson = "{\"theme\":\"neutral\"}" },
                            new { surface = "hand", layer = "frame", sourceKind = "sprite", assetRef = "card/frame/common_metal", sortOrder = 10, metadataJson = "{\"rarity\":\"common\"}" },
                            new { surface = "hand", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}", sortOrder = 20, metadataJson = "{\"crop\":\"portrait\"}" },
                            new { surface = "hand", layer = "rules", sourceKind = "sprite", assetRef = "card/ui/rules_text_default", sortOrder = 40, metadataJson = "{\"font\":\"serif-small\"}" }
                        }
                    }),
                    ("playedBoard", "Played board card", "Board-slot rendering with compact frame and tighter art crop.", new
                    {
                        profileKey = "played-default",
                        displayName = "Played Default",
                        isDefault = false,
                        layers = new[]
                        {
                            new { surface = "played", layer = "frame", sourceKind = "sprite", assetRef = "board/frame/common_metal", sortOrder = 0, metadataJson = "{}" },
                            new { surface = "played", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}", sortOrder = 10, metadataJson = "{\"crop\":\"board-close\"}" }
                        }
                    }),
                    ("fullArtLegendary", "Full-art legendary", "Full-bleed card art with legendary frame, halo and foil overlay.", new
                    {
                        profileKey = "hand-full-art",
                        displayName = "Hand Full Art",
                        isDefault = true,
                        layers = new[]
                        {
                            new { surface = "hand", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}_full", sortOrder = 0, metadataJson = "{\"fullArt\":true,\"safeTextTop\":0.14,\"safeTextBottom\":0.22}" },
                            new { surface = "hand", layer = "frame", sourceKind = "sprite", assetRef = "card/frame/legendary_fullart", sortOrder = 20, metadataJson = "{\"rarity\":\"legendary\"}" },
                            new { surface = "hand", layer = "halo", sourceKind = "sprite", assetRef = "card/fx/halo_divine", sortOrder = 30, metadataJson = "{\"blend\":\"screen\"}" },
                            new { surface = "hand", layer = "foil", sourceKind = "sprite", assetRef = "card/fx/foil_rainbow_soft", sortOrder = 40, metadataJson = "{\"scrollSpeed\":0.15}" }
                        }
                    }),
                    ("inspectGallery", "Inspect gallery view", "Large inspect-only art surface for collection/gallery screens.", new
                    {
                        profileKey = "inspect-gallery",
                        displayName = "Inspect Gallery",
                        isDefault = false,
                        layers = new[]
                        {
                            new { surface = "inspect", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}_gallery", sortOrder = 0, metadataJson = "{\"zoom\":\"free\",\"pan\":\"slow\"}" },
                            new { surface = "inspect", layer = "title-ornament", sourceKind = "sprite", assetRef = "card/ui/inspect_ornament_legendary", sortOrder = 10, metadataJson = "{}" },
                            new { surface = "inspect", layer = "caption-bar", sourceKind = "sprite", assetRef = "card/ui/lore_caption_bar", sortOrder = 20, metadataJson = "{\"supportsRichText\":true}" }
                        }
                    }),
                    ("rewardCard", "Reward card", "A card reward/pick screen layout with full bleed art and burst FX.", new
                    {
                        profileKey = "reward-card",
                        displayName = "Reward Card",
                        isDefault = false,
                        layers = new[]
                        {
                            new { surface = "reward", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}_reward", sortOrder = 0, metadataJson = "{\"fullBleed\":true}" },
                            new { surface = "reward", layer = "frame", sourceKind = "sprite", assetRef = "card/frame/reward_gold", sortOrder = 10, metadataJson = "{\"shine\":\"slow\"}" },
                            new { surface = "reward", layer = "burst", sourceKind = "sprite", assetRef = "card/fx/reward_burst_orange", sortOrder = 20, metadataJson = "{}" }
                        }
                    }),
                    ("decklistThumbnail", "Decklist thumbnail", "Small deck builder row/list representation.", new
                    {
                        profileKey = "decklist-minimal",
                        displayName = "Decklist Minimal",
                        isDefault = false,
                        layers = new[]
                        {
                            new { surface = "decklist", layer = "thumbnail", sourceKind = "image", assetRef = "card/thumb/{{cardId}}", sortOrder = 0, metadataJson = "{\"shape\":\"square\"}" },
                            new { surface = "decklist", layer = "rarity-pip", sourceKind = "sprite", assetRef = "card/icon/rarity_rare", sortOrder = 10, metadataJson = "{}" }
                        }
                    }),
                    ("factionPremium", "Faction premium", "Faction-colored frame with reactive foil and badge.", new
                    {
                        profileKey = "premium-faction",
                        displayName = "Premium Faction",
                        isDefault = true,
                        layers = new[]
                        {
                            new { surface = "hand", layer = "bg", sourceKind = "sprite", assetRef = "card/bg/ember_smoke", sortOrder = 0, metadataJson = "{\"faction\":\"ember\"}" },
                            new { surface = "hand", layer = "art", sourceKind = "image", assetRef = "card/art/{{cardId}}_premium", sortOrder = 10, metadataJson = "{\"fullArt\":true,\"premium\":true}" },
                            new { surface = "hand", layer = "frame", sourceKind = "sprite", assetRef = "card/frame/rare_ember", sortOrder = 20, metadataJson = "{\"rarity\":\"rare\",\"bevel\":\"heavy\"}" },
                            new { surface = "hand", layer = "foil", sourceKind = "sprite", assetRef = "card/fx/foil_reactive_gold", sortOrder = 30, metadataJson = "{\"reactive\":\"mousemove\",\"mask\":\"diagonal\"}" },
                            new { surface = "hand", layer = "badge", sourceKind = "sprite", assetRef = "card/badge/premium", sortOrder = 40, metadataJson = "{\"placement\":\"top-left\"}" }
                        }
                    })),
                "QueueForMatchRequest" => Examples(
                    ("casual", "Casual queue", "Server chooses the ruleset assigned to Casual mode.", new { playerId = "{{playerId}}", deckId = "{{deckId}}", mode = 0, rating = 1000 }),
                    ("ranked", "Ranked queue", "Server chooses the ruleset assigned to Ranked mode.", new { playerId = "{{playerId}}", deckId = "{{deckId}}", mode = 1, rating = 1200 })),
                "CreatePrivateMatchRequest" => Examples(
                    ("createPrivate", "Create private room", "Server chooses the ruleset assigned to Private mode.", new { playerId = "{{playerId}}", deckId = "{{deckId}}", matchName = "Swagger private room" })),
                "JoinPrivateMatchRequest" => Examples(
                    ("joinPrivate", "Join private room", "Use the room code from create-private.", new { playerId = "{{playerId}}", deckId = "{{deckId}}", roomCode = "{{roomCode}}" })),
                "DeckUpsertRequest" => Examples(
                    ("starterDeck", "Starter deck", "Replace card ids with ids from `GET /api/v1/cards` if needed.", new { playerId = "{{playerId}}", deckId = "{{deckId}}", displayName = "Swagger Test Deck", cardIds = Enumerable.Range(1, 20).Select(i => $"card_{i:000}").ToArray() })),
                "AddDeckCardRequest" => Examples(
                    ("appendSelectedCard", "Append selected card", "Uses the Card ID selected in the helper picker and adds it at the end of the deck.", new { cardId = "{{cardId}}", position = (int?)null }),
                    ("insertAtTop", "Insert at position 0", "Adds the selected card as the first authored deck entry.", new { cardId = "{{cardId}}", position = 0 })),
                "SetReadyRequest" => Examples(
                    ("ready", "Mark player ready", "Used before starting synchronized match flow.", new { matchId = "{{matchId}}", playerId = "{{playerId}}", isReady = true })),
                "PlayCardRequest" => Examples(
                    ("playFront", "Play card to front slot", "Use `runtimeHandKey` from your latest snapshot.", new { matchId = "{{matchId}}", playerId = "{{playerId}}", runtimeHandKey = "hand-1", slotIndex = 0 }),
                    ("playBackLeft", "Play card to back-left slot", "Server may shift occupied slots according to board rules.", new { matchId = "{{matchId}}", playerId = "{{playerId}}", runtimeHandKey = "hand-2", slotIndex = 1 })),
                "EndTurnRequest" => Examples(
                    ("endTurn", "End current turn", "Only valid if the authenticated player owns the active turn.", new { matchId = "{{matchId}}", playerId = "{{playerId}}" })),
                "ForfeitRequest" => Examples(
                    ("forfeit", "Forfeit match", "Ends the match and declares the opponent winner.", new { matchId = "{{matchId}}", playerId = "{{playerId}}" })),
                "MatchCompletionRequest" => Examples(
                    ("completeCasual", "Record completion", "Manual match completion payload.", new { playerId = "{{playerId}}", opponentId = "{{opponentId}}", playerWon = true, durationSeconds = 420, playerRatingBefore = 1000, opponentRatingBefore = 1000 })),
                "PostActionsRequest" => Examples(
                    ("singleAction", "Post action batch", "Legacy action ingestion example.", new { matchId = "{{matchId}}", actions = new[] { new { actionNumber = 1, sequence = 1, timestamp = DateTime.UtcNow, playerId = "{{playerId}}", actionType = "play_card", data = new { runtimeHandKey = "hand-1", slotIndex = 0 } } }, globalSequence = 1, timestamp = DateTime.UtcNow })),
                "UpsertGameRulesetRequest" => Examples(
                    ("standard", "Standard 20 HP rules", "Default-feeling match rules.", new { rulesetKey = "standard_20hp", displayName = "Standard 20 HP", description = "Balanced baseline ruleset.", isActive = true, isDefault = false, startingHeroHealth = 20, maxHeroHealth = 20, startingMana = 1, maxMana = 10, manaGrantedPerTurn = 1, manaGrantTiming = 0, initialDrawCount = 4, cardsDrawnOnTurnStart = 1, startingSeatIndex = 0, seatOverrides = Array.Empty<object>() }),
                    ("handicap", "Seat handicap test", "Example with seat-specific mana/HP changes.", new { rulesetKey = "handicap_test", displayName = "Handicap Test", description = "Seat 1 starts slightly ahead for testing.", isActive = true, isDefault = false, startingHeroHealth = 20, maxHeroHealth = 20, startingMana = 1, maxMana = 10, manaGrantedPerTurn = 1, manaGrantTiming = 0, initialDrawCount = 4, cardsDrawnOnTurnStart = 1, startingSeatIndex = 0, seatOverrides = new[] { new { seatIndex = 1, additionalHeroHealth = 5, additionalMaxHeroHealth = 5, additionalStartingMana = 1, additionalMaxMana = 0, additionalManaPerTurn = 0, additionalCardsDrawnOnTurnStart = 0 } } })),
                "AssignMatchmakingModeRulesetRequest" => Examples(
                    ("assign", "Assign mode ruleset", "After this, queue/private uses this ruleset automatically for the selected mode.", new { rulesetId = "{{rulesetId}}" })),
                "CreateCardRequest" => Examples(
                    ("battleUnit", "Battle-ready unit", "Includes presentation and visual profiles for Unity.", new
                    {
                        cardId = "swagger_ember_guard",
                        displayName = "Swagger Ember Guard",
                        description = "Example authored from Swagger.",
                        manaCost = 2,
                        attack = 2,
                        health = 4,
                        armor = 1,
                        cardType = 0,
                        cardRarity = 1,
                        cardFaction = 0,
                        unitType = 0,
                        allowedRow = 2,
                        defaultAttackSelector = 1,
                        turnsUntilCanAttack = 1,
                        isLimited = false,
                        battlePresentation = new { attackMotionLevel = 2, attackShakeLevel = 1, attackDeliveryType = "melee", impactFxId = "fx_ember_hit", attackAudioCueId = "sfx_sword_light", metadataJson = "{\"trail\":\"ember\"}" },
                        visualProfiles = new[]
                        {
                            new
                            {
                                profileKey = "standard",
                                displayName = "Standard",
                                isDefault = true,
                                layers = new[]
                                {
                                    new { surface = "hand", layer = "frame", sourceKind = "sprite", assetRef = "frames/rare/ember_hand", sortOrder = 0, metadataJson = "{}" },
                                    new { surface = "hand", layer = "art", sourceKind = "sprite", assetRef = "cards/ember_guard/full", sortOrder = 1, metadataJson = "{\"crop\":\"portrait\"}" },
                                    new { surface = "played", layer = "frame", sourceKind = "sprite", assetRef = "frames/rare/ember_played", sortOrder = 0, metadataJson = "{}" }
                                }
                            }
                        }
                    }),
                    ("fullArtUnit", "Full-art unit", "One request that creates gameplay stats plus full-art hand profile and played-board profile.", new
                    {
                        cardId = "swagger_full_art_guardian",
                        displayName = "Swagger Full Art Guardian",
                        description = "Full-art visual authoring example.",
                        manaCost = 5,
                        attack = 4,
                        health = 7,
                        armor = 2,
                        cardType = 0,
                        cardRarity = 3,
                        cardFaction = 3,
                        unitType = 0,
                        allowedRow = 2,
                        defaultAttackSelector = 1,
                        turnsUntilCanAttack = 1,
                        isLimited = false,
                        battlePresentation = new { attackMotionLevel = 4, attackShakeLevel = 5, attackDeliveryType = "melee", impactFxId = "impact_guardian_slam", attackAudioCueId = "sfx_guardian_slam", metadataJson = "{\"cameraProfile\":\"heavy\",\"hitStopMs\":120}" },
                        visualProfiles = new[]
                        {
                            new
                            {
                                profileKey = "hand-full-art",
                                displayName = "Hand Full Art",
                                isDefault = true,
                                layers = new[]
                                {
                                    new { surface = "hand", layer = "art", sourceKind = "image", assetRef = "card/art/swagger_full_art_guardian_full", sortOrder = 0, metadataJson = "{\"fullArt\":true,\"safeTextBottom\":0.22}" },
                                    new { surface = "hand", layer = "frame", sourceKind = "sprite", assetRef = "card/frame/legendary_fullart", sortOrder = 20, metadataJson = "{\"rarity\":\"legendary\"}" },
                                    new { surface = "hand", layer = "foil", sourceKind = "sprite", assetRef = "card/fx/foil_legendary_prism", sortOrder = 30, metadataJson = "{\"rainbow\":true}" }
                                }
                            },
                            new
                            {
                                profileKey = "played-premium",
                                displayName = "Played Premium",
                                isDefault = false,
                                layers = new[]
                                {
                                    new { surface = "played", layer = "frame", sourceKind = "sprite", assetRef = "board/frame/legendary_alloy", sortOrder = 0, metadataJson = "{}" },
                                    new { surface = "played", layer = "art", sourceKind = "image", assetRef = "card/art/swagger_full_art_guardian_board", sortOrder = 10, metadataJson = "{\"crop\":\"tight\"}" }
                                }
                            }
                        }
                    })),
                "CreateAbilityRequest" => Examples(
                    ("poisonModifier", "Poison attack modifier", "Normal attack applies poison and emits ordered battle events.", new { abilityId = "poison", displayName = "Poison", description = "Applies poison after dealing health damage.", triggerKind = 3, targetSelectorKind = 1, skillType = 4, animationCueId = "skill_poison_strike", conditionsJson = "{}", metadataJson = "{\"normalAttackModifier\":true}", effects = new[] { new { effectKind = 29, amount = 1, durationTurns = 2, targetSelectorKindOverride = 8, sequence = 0, metadataJson = "{\"statusKind\":\"poison\"}" } } }),
                    ("healAlly", "Regenerate left ally", "Battle phase skill that heals ally in back-left slot.", new { abilityId = "regenerate_left", displayName = "Regenerate Left", description = "Heals the ally in back-left slot.", triggerKind = 3, targetSelectorKind = 6, skillType = 0, animationCueId = "skill_regenerate", conditionsJson = "{}", metadataJson = "{\"animationGroup\":\"support\"}", effects = new[] { new { effectKind = 1, amount = 2, durationTurns = (int?)null, targetSelectorKindOverride = 6, sequence = 0, metadataJson = "{}" } } })),
                "CreateEffectRequest" => Examples(
                    ("damage", "Damage effect", "Deals direct card damage.", new { effectKind = 0, amount = 2, secondaryAmount = (int?)null, durationTurns = (int?)null, targetSelectorKindOverride = 1, sequence = 0, metadataJson = "{}" }),
                    ("shield", "Shield status", "Adds one shield charge.", new { effectKind = 28, amount = 1, secondaryAmount = (int?)null, durationTurns = 99, targetSelectorKindOverride = 0, sequence = 0, metadataJson = "{\"statusKind\":\"shield\"}" })),
                _ => new Dictionary<string, ExampleDefinition>()
            };
        }

        private static IReadOnlyDictionary<string, ExampleDefinition> Examples(params (string Key, string Summary, string Description, object Value)[] examples)
        {
            return examples.ToDictionary(
                example => example.Key,
                example => new ExampleDefinition(example.Summary, example.Description, example.Value),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
