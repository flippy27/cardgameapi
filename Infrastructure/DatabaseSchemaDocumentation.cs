namespace CardDuel.ServerApi.Infrastructure;

public static class DatabaseSchemaDocumentation
{
    public static readonly object[] Tables =
    {
        Table("cards", "Card gameplay definitions. This remains the source materialized to the client catalog.", new[]
        {
            Col("id", "Internal database id.", "c7f..."),
            Col("card_id", "Stable gameplay id used by decks, Swagger variables, and client catalog.", "alloy_0001"),
            Col("display_name", "Player-facing card name.", "Alloy Shieldbearer"),
            Col("description", "Card rules/lore text shown in UI.", "Taunt. Gains shield."),
            Col("mana_cost", "Mana required to play the card.", "3"),
            Col("attack", "Base attack used by normal attacks.", "2"),
            Col("health", "Base/max health once played.", "5"),
            Col("armor", "Starting armor. Damage hits armor before health unless trample ignores it.", "1"),
            Col("card_type", "FK-like enum value documented in lookup/card type docs. 0 Unit, 1 Utility, 2 Equipment, 3 Spell.", "0"),
            Col("card_rarity", "Card rarity enum. 0 Common, 1 Rare, 2 Epic, 3 Legendary.", "1"),
            Col("card_faction", "Faction enum. 0 Ember, 1 Tidal, 2 Grove, 3 Alloy, 4 Void.", "3"),
            Col("unit_type", "Optional unit archetype enum. Null for non-units.", "0"),
            Col("allowed_row", "Placement rule. 0 FrontOnly, 1 BackOnly, 2 Flexible.", "2"),
            Col("default_attack_selector", "Legacy/default selector for authored effects. Normal attack targeting is resolved from unit_type and current slot.", "1"),
            Col("turns_until_can_attack", "How many owner turns the card waits before attacking unless Haste applies.", "1"),
            Col("is_limited", "Designer flag for limited/special cards.", "false")
        }),
        Table("abilities", "Reusable ability definitions. Cards attach abilities through card_abilities.", new[]
        {
            Col("ability_id", "Stable ability key referenced by code/events/status source.", "poison"),
            Col("skill_type", "FK to ability_skill_type_definitions. Authoring category, not battle math by itself.", "4 = modifier"),
            Col("trigger_kind", "FK to ability_trigger_kind_definitions. When the server evaluates the ability.", "3 = on_battle_phase"),
            Col("target_selector_kind", "FK to target_selector_kind_definitions. Default target selection.", "1 = frontline_first"),
            Col("animation_cue_id", "Client animation cue emitted with ordered battle events.", "skill_poison_strike"),
            Col("tooltip_summary", "Short player-facing tooltip summary.", "Poisons damaged enemies."),
            Col("conditions_json", "Future/extensible server-side conditions object.", "{}"),
            Col("metadata_json", "Flexible metadata for authoring/client UI. Does not replace engine logic.", "{\"normalAttackModifier\":true}")
        }),
        Table("decks", "Deck headers owned by a user. Card membership lives in deck_cards, not in a giant JSON/list column.", new[]
        {
            Col("id", "Internal deck database id used by deck_cards.deck_id.", "deck-1"),
            Col("user_id", "Owner user id. Must match authenticated player id for deck endpoints.", "6c646037-69b0-4350-a98e-d09f77806192"),
            Col("deck_id", "Stable public deck id used by matchmaking and Unity.", "deck_playerone_1"),
            Col("display_name", "Player-facing deck name.", "PlayerOne Deck 1"),
            Col("created_at", "Creation timestamp.", "2026-04-25T15:20:00Z"),
            Col("updated_at", "Last edit timestamp.", "2026-04-25T15:40:00Z")
        }),
        Table("deck_cards", "Normalized deck contents. Each row is one card copy at one ordered position in one deck.", new[]
        {
            Col("id", "Stable entry id for removing one specific card copy from Swagger/API.", "deck-1-card-0"),
            Col("deck_id", "FK to decks.id.", "deck-1"),
            Col("card_definition_id", "FK to cards.id. This avoids storing card blobs inside decks.", "card-42"),
            Col("position", "Zero-based order inside the deck. Shuffle starts from this authored list.", "0"),
            Col("created_at", "When this card copy was added.", "2026-04-25T15:20:00Z")
        }),
        Table("effects", "Effect rows executed by an ability. These are the composable server math/status operations.", new[]
        {
            Col("effect_kind", "FK to effect_kind_definitions. Defines what operation runs; no magic number required.", "29 = apply_poison"),
            Col("amount", "Primary numeric value. Damage/heal/shield charges/poison tick amount depending on effect kind.", "1"),
            Col("secondary_amount", "Optional second numeric value reserved for advanced effects.", "null"),
            Col("duration_turns", "Optional status duration.", "2"),
            Col("target_selector_kind_override", "Optional FK to target_selector_kind_definitions overriding ability selector for this effect.", "8 = source_opponent"),
            Col("sequence", "Execution order inside the ability.", "0"),
            Col("metadata_json", "Per-effect metadata such as statusKind, animation override, or authoring notes.", "{\"statusKind\":\"poison\"}")
        }),
        Table("effect_kind_definitions", "Lookup table replacing magic effect_kind numbers with names, descriptions, categories, and icons.", new[]
        {
            Col("id", "Numeric id used by engine enum and effects.effect_kind FK.", "29"),
            Col("key", "Stable readable key.", "apply_poison"),
            Col("display_name", "Human-readable name.", "Apply Poison"),
            Col("description", "What this effect does in server logic.", "Applies poisoned status to a target."),
            Col("category", "Grouping such as math, modifier, status, reserved.", "status"),
            Col("produces_status_kind", "Optional FK-ish id to status_effect_kind_definitions.", "0"),
            Col("icon_asset_ref", "Default icon for this effect kind in authoring UI.", "effects/apply_poison")
        }),
        Table("status_effect_kind_definitions", "Buff/debuff/status indicators currently applied to runtime board cards.", new[]
        {
            Col("id", "Numeric id used by runtime StatusEffectKind.", "0"),
            Col("key", "Stable readable key.", "poison"),
            Col("display_name", "Client indicator label.", "Poisoned"),
            Col("category", "buff, debuff, cooldown, internal.", "debuff")
        })
    };

    private static object Table(string name, string description, object[] columns) => new { name, description, columns };
    private static object Col(string name, string description, string example) => new { name, description, example };
}
