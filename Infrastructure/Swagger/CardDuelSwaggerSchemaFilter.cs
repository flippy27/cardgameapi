using CardDuel.ServerApi.Game;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CardDuel.ServerApi.Infrastructure.Swagger;

public sealed class CardDuelSwaggerSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var enumType = Nullable.GetUnderlyingType(context.Type) ?? context.Type;
        if (enumType.IsEnum)
        {
            ApplyEnum(schema, enumType, "Select one of the supported values.");
        }

        ApplyIntegerEnum(schema, "mode", typeof(Contracts.QueueMode), "Matchmaking mode. Private matches use the private endpoints, not queue.");
        ApplyIntegerEnum(schema, "manaGrantTiming", typeof(Contracts.ManaGrantTiming), "When mana is granted by the active ruleset.");
        ApplyIntegerEnum(schema, "cardType", typeof(CardType), "Card category.");
        ApplyIntegerEnum(schema, "cardRarity", typeof(CardRarity), "Card rarity.");
        ApplyIntegerEnum(schema, "cardFaction", typeof(CardFaction), "Card faction.");
        ApplyIntegerEnum(schema, "unitType", typeof(UnitType), "Optional unit archetype.");
        ApplyIntegerEnum(schema, "allowedRow", typeof(AllowedRow), "Where the card may be played.");
        ApplyIntegerEnum(schema, "defaultAttackSelector", typeof(TargetSelectorKind), "Legacy/default selector for authored skills. Normal attacks are resolved from unit type and slot by the battle rules.");
        ApplyIntegerEnum(schema, "triggerKind", typeof(TriggerKind), "When an ability is evaluated by the server.");
        ApplyIntegerEnum(schema, "targetSelectorKind", typeof(TargetSelectorKind), "How the server resolves targets.");
        ApplyIntegerEnum(schema, "targetSelectorKindOverride", typeof(TargetSelectorKind), "Optional per-effect target selector override.");
        ApplyIntegerEnum(schema, "effectKind", typeof(EffectKind), "Server-side effect operation.");
        ApplyIntegerEnum(schema, "skillType", typeof(SkillType), "High-level ability category for authoring and UI grouping.");
        ApplyIntegerEnum(schema, "slotIndex", typeof(BoardSlot), "Board slot index.");
        ApplyJsonDescription(schema, "conditionsJson", "JSON object evaluated by server-side skill logic. Use `{}` when there are no conditions.");
        ApplyJsonDescription(schema, "metadataJson", "Flexible JSON object for animation, targeting, or future server-authoritative metadata.");
        ApplyJsonDescription(schema, "battlePresentation", "Server-authoritative attack animation and presentation hints.");
        ApplyJsonDescription(schema, "visualProfiles", "Visual layers used by the client to render hand and played card states.");
        ApplyPropertyDescriptions(schema);
    }

    private static void ApplyIntegerEnum(OpenApiSchema schema, string propertyName, Type enumType, string description)
    {
        if (schema.Properties == null || !schema.Properties.TryGetValue(propertyName, out var property))
        {
            return;
        }

        ApplyEnum(property, enumType, description);
    }

    private static void ApplyEnum(OpenApiSchema schema, Type enumType, string description)
    {
        var values = Enum.GetValues(enumType).Cast<object>().ToArray();
        var names = Enum.GetNames(enumType);

        schema.Enum = values
            .Select(value => (IOpenApiAny)new OpenApiInteger(Convert.ToInt32(value)))
            .ToList();

        var enumNames = new OpenApiArray();
        foreach (var name in names)
        {
            enumNames.Add(new OpenApiString(name));
        }

        schema.Extensions["x-enumNames"] = enumNames;

        var enumDocs = string.Join(", ", values.Select((value, index) => $"{Convert.ToInt32(value)} = {names[index]}"));
        schema.Description = Append(schema.Description, $"{description} Values: {enumDocs}.");
    }

    private static void ApplyJsonDescription(OpenApiSchema schema, string propertyName, string description)
    {
        if (schema.Properties == null || !schema.Properties.TryGetValue(propertyName, out var property))
        {
            return;
        }

        property.Description = Append(property.Description, description);
        property.Example ??= OpenApiAnyFactory.CreateFromJson("\"{}\"");
    }

    private static void ApplyPropertyDescriptions(OpenApiSchema schema)
    {
        if (schema.Properties == null)
        {
            return;
        }

        foreach (var (propertyName, description) in PropertyDocs)
        {
            if (schema.Properties.TryGetValue(propertyName, out var property))
            {
                property.Description = Append(property.Description, description);
            }
        }
    }

    private static string Append(string? current, string addition)
    {
        return string.IsNullOrWhiteSpace(current)
            ? addition
            : $"{current}\n\n{addition}";
    }

    private static readonly Dictionary<string, string> PropertyDocs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["email"] = "Account email used for login and identity lookup.",
        ["username"] = "Public player display name.",
        ["password"] = "Plain password only for register/login requests. Example seeded profiles use `123456`.",
        ["token"] = "JWT access token returned by login/register. Swagger stores this locally and sends it as Bearer auth.",
        ["userId"] = "Stable server player id. Use this exact value as `playerId` in protected requests.",
        ["playerId"] = "Player id performing the action. It must match the JWT subject or the server rejects the request.",
        ["opponentId"] = "Other player involved in a completed match or test flow.",
        ["itemTypeKey"] = "Stable item/economy key. Discover valid values from `GET /api/v1/items` or the Swagger DB picker.",
        ["playerCardId"] = "Unique player-owned card instance id from collection endpoints. This is not the shared card definition id.",
        ["upgradeId"] = "Unique upgrade row id attached to a player-owned card instance.",
        ["requirementId"] = "Unique crafting requirement row id attached to a card recipe.",
        ["runtimeCardId"] = "Ephemeral board card id from a live match snapshot. Use this for destroy/sacrifice actions against a current in-play card.",
        ["deckId"] = "Stable deck id owned by the player, for example `deck_playerone_1`.",
        ["entryId"] = "Stable id for one deck card row. Removing by entry id removes one copy, not every copy of that card.",
        ["position"] = "Zero-based order inside an authored list, such as a deck or effect sequence.",
        ["cards"] = "Ordered deck card entries. Each entry is one card copy from the normalized deck_cards table.",
        ["displayName"] = "Human-readable name shown in tools/UI.",
        ["description"] = "Human-readable description for designers, Swagger, or client UI.",
        ["matchId"] = "Stable match id returned by matchmaking/private room creation.",
        ["roomCode"] = "Short private-room code shared with another player.",
        ["reconnectToken"] = "Per-seat token used to reconnect to a match reservation.",
        ["seatIndex"] = "Local seat assigned by the server: 0 or 1.",
        ["waitingForOpponent"] = "True when the reservation is waiting for a second player.",
        ["status"] = "Human-readable match/reservation state.",
        ["rules"] = "Authoritative rules snapshot used by this match.",
        ["rulesetId"] = "Stable database id for a game ruleset.",
        ["rulesetKey"] = "Designer-friendly unique key for a ruleset.",
        ["isActive"] = "Whether this ruleset can be used by matchmaking assignments.",
        ["isDefault"] = "Marks this item as the default choice, such as the default ruleset or default visual profile.",
        ["startingHeroHealth"] = "Hero HP at match start before per-seat overrides.",
        ["maxHeroHealth"] = "Maximum hero HP before per-seat overrides.",
        ["startingMana"] = "Mana available at match start before per-seat overrides.",
        ["maxMana"] = "Maximum mana cap before per-seat overrides.",
        ["manaGrantedPerTurn"] = "Mana gained each turn before per-seat overrides.",
        ["initialDrawCount"] = "Cards drawn when the match initializes.",
        ["cardsDrawnOnTurnStart"] = "Cards drawn at the start of a player's turn.",
        ["startingSeatIndex"] = "Seat that takes the first turn.",
        ["seatOverrides"] = "Optional per-seat handicap deltas applied over base rules.",
        ["additionalHeroHealth"] = "Extra starting hero HP for this seat.",
        ["additionalMaxHeroHealth"] = "Extra max hero HP for this seat.",
        ["additionalStartingMana"] = "Extra starting mana for this seat.",
        ["additionalMaxMana"] = "Extra mana cap for this seat.",
        ["additionalManaPerTurn"] = "Extra mana gained per turn for this seat.",
        ["additionalCardsDrawnOnTurnStart"] = "Extra cards drawn at turn start for this seat.",
        ["cardId"] = "Stable card gameplay id referenced by decks and card catalog APIs.",
        ["manaCost"] = "Mana required to play this card.",
        ["attack"] = "Base attack damage used by normal attacks.",
        ["health"] = "Base health of the played card.",
        ["armor"] = "Damage prevention pool on the card. Trample can ignore armor.",
        ["unitType"] = "Optional combat archetype for unit cards. Melee attacks only from Front; Ranged attacks from back slots straight lane then Front; Magic attacks from back slots diagonal lane then Front.",
        ["turnsUntilCanAttack"] = "Number of owner turns the card waits before normal attacking unless Haste overrides it.",
        ["isLimited"] = "Designer flag for limited/special cards.",
        ["attackMotionLevel"] = "Server-authored intensity for attack motion animation.",
        ["attackShakeLevel"] = "Server-authored impact/camera shake intensity.",
        ["attackDeliveryType"] = "Animation family such as `melee`, `projectile`, `beam`, or `arc`.",
        ["impactFxId"] = "Client asset id for the impact VFX.",
        ["attackAudioCueId"] = "Client asset id for the attack sound cue.",
        ["profileKey"] = "Unique visual profile key, for example `standard`, `full_art`, or `alt_skin_01`.",
        ["templateId"] = "Database id of a reusable card visual profile template.",
        ["overrideDisplayName"] = "Optional per-card display name override when assigning a reusable visual template.",
        ["overrideLayers"] = "Optional per-card layer override. If null, the template layer stack is used.",
        ["isActive"] = "Whether this authoring item should currently be used.",
        ["layers"] = "Ordered visual layers used to compose a card.",
        ["surface"] = "Where the layer is used, commonly `hand` or `played`.",
        ["layer"] = "Layer role such as `frame`, `art`, `foil`, `badge`, or `overlay`.",
        ["sourceKind"] = "Asset source type, for example `sprite`, `addressable`, `url`, or future custom resolvers.",
        ["assetRef"] = "Client-resolvable asset reference.",
        ["sortOrder"] = "Draw order within a visual profile; lower values render first.",
        ["abilityId"] = "Stable ability key, for example `poison`, `shield`, or `regenerate_left`.",
        ["iconAssetRef"] = "Client-resolvable icon asset reference for ability/effect/status badges.",
        ["statusIconAssetRef"] = "Client-resolvable status indicator icon commonly produced by this ability.",
        ["animationCueId"] = "Client animation cue emitted with ordered battle events.",
        ["vfxCueId"] = "Client-resolvable VFX cue for ability/status/effect presentation.",
        ["audioCueId"] = "Client-resolvable audio cue for ability/status/effect presentation.",
        ["uiColorHex"] = "Optional UI accent color in hex format.",
        ["tooltipSummary"] = "Short player-facing explanation for badges/tooltips.",
        ["effects"] = "Ordered effect list executed by the server for this ability.",
        ["acquiredFrom"] = "Source label describing how a player obtained this card instance, for example `crafted`, `match_reward`, or `swagger_grant`.",
        ["quantity"] = "How many units of the item are being granted, consumed, or currently owned.",
        ["quantityRequired"] = "How many units of this item are required by one crafting recipe row.",
        ["requirements"] = "Full crafting recipe replacement list. Each entry is one required item type and quantity.",
        ["upgradeKind"] = "Free-form upgrade category key such as `attack_bonus`, `health_bonus`, `armor_bonus`, `level_up`, `added_ability`, or a future extensible upgrade.",
        ["intValue"] = "Optional numeric payload for upgrades/effects. Example: `1` for `attack_bonus` or `health_bonus`.",
        ["stringValue"] = "Optional string payload for upgrades/effects. Example: an `abilityId` when `upgradeKind` is `added_ability`.",
        ["appliedBy"] = "Free-form source label describing who or what applied the upgrade.",
        ["note"] = "Optional human-readable note explaining why the upgrade row exists.",
        ["key"] = "Stable readable lookup key. Prefer this in authoring UI instead of showing raw ids.",
        ["category"] = "Human-readable grouping for filtering authoring lists.",
        ["producesStatusKind"] = "Optional status kind id produced by an effect kind.",
        ["amount"] = "Primary numeric value used by the effect, such as damage, heal, shield charges, or poison tick damage.",
        ["secondaryAmount"] = "Optional second numeric value reserved for advanced effects.",
        ["durationTurns"] = "Optional duration in turns for status-like effects.",
        ["sequence"] = "Deterministic execution order. Lower values execute first; battle event `sequence` is what Unity should animate.",
        ["runtimeHandKey"] = "Ephemeral key from the latest snapshot identifying a specific card in hand.",
        ["slotIndex"] = "Target board slot: 0 Front, 1 BackLeft, 2 BackRight.",
        ["isReady"] = "Whether the player is ready in the match flow.",
        ["playerWon"] = "True when `playerId` is the winner for match completion.",
        ["durationSeconds"] = "Match duration in seconds.",
        ["actionType"] = "Client/server action name used by replay/action ingestion.",
        ["data"] = "Action-specific payload object.",
        ["globalSequence"] = "Client batch sequence marker for action ingestion.",
        ["timestamp"] = "Client action timestamp.",
        ["page"] = "1-based page index.",
        ["pageSize"] = "Maximum records returned in this page.",
        ["region"] = "Leaderboard/rating region key, usually `global`."
    };
}
