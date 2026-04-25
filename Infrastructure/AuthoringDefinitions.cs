using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Infrastructure;

public static class AuthoringDefinitions
{
    public static readonly SkillTypeDefinition[] SkillTypes =
    {
        new() { Id = 0, Key = "defensive", DisplayName = "Defensive", Description = "Ability primarily protects, heals, shields, redirects, or improves survivability.", Category = "ability", IconAssetRef = "ui/skill_type/defensive", MetadataJson = "{}" },
        new() { Id = 1, Key = "offensive", DisplayName = "Offensive", Description = "Ability primarily deals damage, applies harmful status, or improves attacks.", Category = "ability", IconAssetRef = "ui/skill_type/offensive", MetadataJson = "{}" },
        new() { Id = 2, Key = "equipable", DisplayName = "Equipable", Description = "Ability is intended for equipment-style or attachment-style cards.", Category = "ability", IconAssetRef = "ui/skill_type/equipable", MetadataJson = "{}" },
        new() { Id = 3, Key = "utility", DisplayName = "Utility", Description = "Ability performs support behavior that is not purely offensive/defensive.", Category = "ability", IconAssetRef = "ui/skill_type/utility", MetadataJson = "{}" },
        new() { Id = 4, Key = "modifier", DisplayName = "Modifier", Description = "Ability modifies normal attack behavior, targeting, or timing instead of resolving as standalone math.", Category = "ability", IconAssetRef = "ui/skill_type/modifier", MetadataJson = "{}" }
    };

    public static readonly TriggerKindDefinition[] TriggerKinds =
    {
        new() { Id = 0, Key = "on_play", DisplayName = "On Play", Description = "Runs when the card is played from hand onto the board.", Category = "trigger", IconAssetRef = "ui/trigger/on_play", MetadataJson = "{}" },
        new() { Id = 1, Key = "on_turn_start", DisplayName = "On Turn Start", Description = "Runs at the start of the owning player's turn.", Category = "trigger", IconAssetRef = "ui/trigger/on_turn_start", MetadataJson = "{}" },
        new() { Id = 2, Key = "on_turn_end", DisplayName = "On Turn End", Description = "Runs at the end of the owning player's turn.", Category = "trigger", IconAssetRef = "ui/trigger/on_turn_end", MetadataJson = "{}" },
        new() { Id = 3, Key = "on_battle_phase", DisplayName = "On Battle Phase", Description = "Runs during battle phase before/around normal attack resolution, depending on the engine rule.", Category = "trigger", IconAssetRef = "ui/trigger/on_battle_phase", MetadataJson = "{}" }
    };

    public static readonly TargetSelectorKindDefinition[] TargetSelectors =
    {
        new() { Id = 0, Key = "self", DisplayName = "Self", Description = "Targets the card that owns the ability.", Category = "selector", IconAssetRef = "ui/selector/self", MetadataJson = "{}" },
        new() { Id = 1, Key = "frontline_first", DisplayName = "Frontline First", Description = "Targets enemy frontline first; taunt may force this selector to a taunting unit.", Category = "selector", IconAssetRef = "ui/selector/frontline_first", MetadataJson = "{}" },
        new() { Id = 2, Key = "backline_first", DisplayName = "Backline First", Description = "Targets back-left/back-right enemies before frontline depending on board availability.", Category = "selector", IconAssetRef = "ui/selector/backline_first", MetadataJson = "{}" },
        new() { Id = 3, Key = "all_enemies", DisplayName = "All Enemies", Description = "Targets all enemy board cards.", Category = "selector", IconAssetRef = "ui/selector/all_enemies", MetadataJson = "{}" },
        new() { Id = 4, Key = "lowest_health_ally", DisplayName = "Lowest Health Ally", Description = "Targets the allied board card with the lowest current health.", Category = "selector", IconAssetRef = "ui/selector/lowest_health_ally", MetadataJson = "{}" },
        new() { Id = 5, Key = "ally_front", DisplayName = "Ally Front", Description = "Targets allied frontline slot.", Category = "selector", IconAssetRef = "ui/selector/ally_front", MetadataJson = "{}" },
        new() { Id = 6, Key = "ally_back_left", DisplayName = "Ally Back Left", Description = "Targets allied back-left slot.", Category = "selector", IconAssetRef = "ui/selector/ally_back_left", MetadataJson = "{}" },
        new() { Id = 7, Key = "ally_back_right", DisplayName = "Ally Back Right", Description = "Targets allied back-right slot.", Category = "selector", IconAssetRef = "ui/selector/ally_back_right", MetadataJson = "{}" },
        new() { Id = 8, Key = "source_opponent", DisplayName = "Source Opponent", Description = "Targets the current opposing card for normal attack modifier effects such as poison/stun.", Category = "selector", IconAssetRef = "ui/selector/source_opponent", MetadataJson = "{}" }
    };

    public static readonly StatusEffectKindDefinition[] StatusEffectKinds =
    {
        new() { Id = 0, Key = "poison", DisplayName = "Poisoned", Description = "Debuff. Deals damage over time at battle-start processing.", Category = "debuff", IconAssetRef = "status/poisoned", VfxCueId = "vfx_status_poison", UiColorHex = "#62B357", MetadataJson = "{\"clientIndicator\":\"poisoned\"}" },
        new() { Id = 1, Key = "stun", DisplayName = "Stunned", Description = "Debuff. The affected card skips its next attack, then the status is consumed.", Category = "debuff", IconAssetRef = "status/stunned", VfxCueId = "vfx_status_stun", UiColorHex = "#F2D14B", MetadataJson = "{\"clientIndicator\":\"stunned\"}" },
        new() { Id = 2, Key = "shield", DisplayName = "Shielded", Description = "Buff. Blocks incoming damage charges before health/armor are modified.", Category = "buff", IconAssetRef = "status/shielded", VfxCueId = "vfx_status_shield", UiColorHex = "#61A8FF", MetadataJson = "{\"clientIndicator\":\"shielded\"}" },
        new() { Id = 3, Key = "enrage_cooldown", DisplayName = "Enrage Cooldown", Description = "Internal debuff/cooldown. Applied after enrage attacks twice; card skips its next attack.", Category = "cooldown", IconAssetRef = "status/enrage_cooldown", VfxCueId = "vfx_status_enrage_cooldown", UiColorHex = "#FF6A3D", MetadataJson = "{\"clientIndicator\":\"enrageCooldown\"}" }
    };

    public static readonly ItemTypeDefinition[] ItemTypes =
    {
        new() { Id = 0, Key = "card_dust",        DisplayName = "Card Dust",        Description = "Basic crafting material earned by playing matches. Required for all card crafts.",                          Category = "crafting", MaxStack = -1, IsActive = true, IconAssetRef = "ui/items/card_dust",        MetadataJson = "{}" },
        new() { Id = 1, Key = "arcane_shard",     DisplayName = "Arcane Shard",     Description = "Uncommon crafting material. Required for rare and epic cards.",                                             Category = "crafting", MaxStack = -1, IsActive = true, IconAssetRef = "ui/items/arcane_shard",     MetadataJson = "{}" },
        new() { Id = 2, Key = "essence_of_void",  DisplayName = "Essence of Void",  Description = "Rare crafting material obtained from special events. Required for legendary cards and upgrades.",          Category = "crafting", MaxStack = -1, IsActive = true, IconAssetRef = "ui/items/essence_of_void",  MetadataJson = "{}" },
        new() { Id = 3, Key = "faction_ember",    DisplayName = "Ember Ember",      Description = "Faction-specific material from Ember faction matches. Used in Ember card crafting.",                       Category = "faction",  MaxStack = -1, IsActive = true, IconAssetRef = "ui/items/faction_ember",    MetadataJson = "{\"faction\":0}" },
        new() { Id = 4, Key = "faction_tidal",    DisplayName = "Tidal Droplet",    Description = "Faction-specific material from Tidal faction matches.",                                                    Category = "faction",  MaxStack = -1, IsActive = true, IconAssetRef = "ui/items/faction_tidal",    MetadataJson = "{\"faction\":1}" },
        new() { Id = 5, Key = "faction_grove",    DisplayName = "Grove Seed",       Description = "Faction-specific material from Grove faction matches.",                                                    Category = "faction",  MaxStack = -1, IsActive = true, IconAssetRef = "ui/items/faction_grove",    MetadataJson = "{\"faction\":2}" },
        new() { Id = 6, Key = "faction_alloy",    DisplayName = "Alloy Scrap",      Description = "Faction-specific material from Alloy faction matches.",                                                    Category = "faction",  MaxStack = -1, IsActive = true, IconAssetRef = "ui/items/faction_alloy",    MetadataJson = "{\"faction\":3}" },
        new() { Id = 7, Key = "faction_void",     DisplayName = "Void Crystal",     Description = "Faction-specific material from Void faction matches.",                                                    Category = "faction",  MaxStack = -1, IsActive = true, IconAssetRef = "ui/items/faction_void",     MetadataJson = "{\"faction\":4}" },
        new() { Id = 8, Key = "upgrade_stone",    DisplayName = "Upgrade Stone",    Description = "Used to apply stat upgrades to player-owned cards.",                                                       Category = "upgrade",  MaxStack = -1, IsActive = true, IconAssetRef = "ui/items/upgrade_stone",    MetadataJson = "{}" },
        new() { Id = 9, Key = "ability_tome",     DisplayName = "Ability Tome",     Description = "Rare item needed to add a new ability to an owned card.",                                                  Category = "upgrade",  MaxStack = -1, IsActive = true, IconAssetRef = "ui/items/ability_tome",     MetadataJson = "{}" }
    };

    public static readonly EffectKindDefinition[] EffectKinds =
    {
        new() { Id = 0, Key = "damage", DisplayName = "Damage", Description = "Deals direct damage to one or more target cards.", Category = "math", IconAssetRef = "effects/damage", MetadataJson = "{}" },
        new() { Id = 1, Key = "heal", DisplayName = "Heal", Description = "Restores health to a target card.", Category = "math", IconAssetRef = "effects/heal", MetadataJson = "{}" },
        new() { Id = 2, Key = "gain_armor", DisplayName = "Gain Armor", Description = "Adds armor to a card.", Category = "math", IconAssetRef = "effects/armor", MetadataJson = "{}" },
        new() { Id = 3, Key = "buff_attack", DisplayName = "Buff Attack", Description = "Increases attack value on a card.", Category = "math", IconAssetRef = "effects/buff_attack", MetadataJson = "{}" },
        new() { Id = 4, Key = "hit_hero", DisplayName = "Hit Hero", Description = "Deals damage directly to the opposing hero.", Category = "math", IconAssetRef = "effects/hit_hero", MetadataJson = "{}" },
        new() { Id = 5, Key = "stun", DisplayName = "Stun Modifier", Description = "Legacy modifier kind for stun-style behavior.", Category = "modifier", ProducesStatusKind = 1, IconAssetRef = "effects/stun", MetadataJson = "{}" },
        new() { Id = 6, Key = "poison", DisplayName = "Poison Modifier", Description = "Legacy modifier kind for poison-style behavior.", Category = "modifier", ProducesStatusKind = 0, IconAssetRef = "effects/poison", MetadataJson = "{}" },
        new() { Id = 7, Key = "leech", DisplayName = "Leech", Description = "Normal attack modifier. Heals source by health damage dealt.", Category = "modifier", IconAssetRef = "effects/leech", MetadataJson = "{}" },
        new() { Id = 8, Key = "evasion", DisplayName = "Evasion", Description = "Reserved effect kind for future evasion behavior.", Category = "reserved", IconAssetRef = "effects/evasion", MetadataJson = "{}" },
        new() { Id = 9, Key = "shield", DisplayName = "Shield Modifier", Description = "Legacy modifier kind for shield behavior.", Category = "modifier", ProducesStatusKind = 2, IconAssetRef = "effects/shield", MetadataJson = "{}" },
        new() { Id = 10, Key = "reflection", DisplayName = "Reflection", Description = "Reserved effect kind for future reflection behavior.", Category = "reserved", IconAssetRef = "effects/reflection", MetadataJson = "{}" },
        new() { Id = 11, Key = "dodge", DisplayName = "Dodge", Description = "Reserved effect kind for future dodge behavior.", Category = "reserved", IconAssetRef = "effects/dodge", MetadataJson = "{}" },
        new() { Id = 12, Key = "enrage", DisplayName = "Enrage", Description = "Normal attack modifier. Attacks twice then receives enrage cooldown.", Category = "modifier", ProducesStatusKind = 3, IconAssetRef = "effects/enrage", MetadataJson = "{}" },
        new() { Id = 13, Key = "mana_burn", DisplayName = "Mana Burn", Description = "Reserved effect kind for future mana disruption.", Category = "reserved", IconAssetRef = "effects/mana_burn", MetadataJson = "{}" },
        new() { Id = 14, Key = "regenerate", DisplayName = "Regenerate", Description = "Legacy regeneration effect family.", Category = "modifier", IconAssetRef = "effects/regenerate", MetadataJson = "{}" },
        new() { Id = 15, Key = "execute", DisplayName = "Execute", Description = "Reserved effect kind for threshold kill behavior.", Category = "reserved", IconAssetRef = "effects/execute", MetadataJson = "{}" },
        new() { Id = 16, Key = "diagonal_attack", DisplayName = "Diagonal Attack", Description = "Reserved effect kind for diagonal targeting behavior.", Category = "reserved", IconAssetRef = "effects/diagonal_attack", MetadataJson = "{}" },
        new() { Id = 17, Key = "fly", DisplayName = "Fly", Description = "Normal attack modifier. Bypasses non-flying defender rules.", Category = "modifier", IconAssetRef = "effects/fly", MetadataJson = "{}" },
        new() { Id = 18, Key = "armor", DisplayName = "Armor", Description = "Legacy armor effect family.", Category = "modifier", IconAssetRef = "effects/armor", MetadataJson = "{}" },
        new() { Id = 19, Key = "chain", DisplayName = "Chain", Description = "Reserved effect kind for chained attacks/effects.", Category = "reserved", IconAssetRef = "effects/chain", MetadataJson = "{}" },
        new() { Id = 20, Key = "charge", DisplayName = "Charge", Description = "Reserved effect kind for immediate attack timing.", Category = "reserved", IconAssetRef = "effects/charge", MetadataJson = "{}" },
        new() { Id = 21, Key = "cleave", DisplayName = "Cleave", Description = "Reserved effect kind for multi-target melee damage.", Category = "reserved", IconAssetRef = "effects/cleave", MetadataJson = "{}" },
        new() { Id = 22, Key = "last_stand", DisplayName = "Last Stand", Description = "Reserved effect kind for low-health behavior.", Category = "reserved", IconAssetRef = "effects/last_stand", MetadataJson = "{}" },
        new() { Id = 23, Key = "melee_range", DisplayName = "Melee Range", Description = "Reserved effect kind for melee/range constraints.", Category = "reserved", IconAssetRef = "effects/melee_range", MetadataJson = "{}" },
        new() { Id = 24, Key = "ricochet", DisplayName = "Ricochet", Description = "Reserved effect kind for bouncing projectiles.", Category = "reserved", IconAssetRef = "effects/ricochet", MetadataJson = "{}" },
        new() { Id = 25, Key = "taunt", DisplayName = "Taunt", Description = "Targeting modifier. Enemy frontline-first selectors prefer taunting cards.", Category = "modifier", IconAssetRef = "effects/taunt", MetadataJson = "{}" },
        new() { Id = 26, Key = "trample", DisplayName = "Trample", Description = "Normal attack modifier. Ignores armor during damage resolution.", Category = "modifier", IconAssetRef = "effects/trample", MetadataJson = "{}" },
        new() { Id = 27, Key = "haste", DisplayName = "Haste", Description = "Normal attack timing modifier. Allows attack before the usual wait.", Category = "modifier", IconAssetRef = "effects/haste", MetadataJson = "{}" },
        new() { Id = 28, Key = "add_shield", DisplayName = "Add Shield", Description = "Applies shielded status charges to a target.", Category = "status", ProducesStatusKind = 2, IconAssetRef = "effects/add_shield", MetadataJson = "{}" },
        new() { Id = 29, Key = "apply_poison", DisplayName = "Apply Poison", Description = "Applies poisoned status to a target.", Category = "status", ProducesStatusKind = 0, IconAssetRef = "effects/apply_poison", MetadataJson = "{}" },
        new() { Id = 30, Key = "apply_stun", DisplayName = "Apply Stun", Description = "Applies stunned status to a target.", Category = "status", ProducesStatusKind = 1, IconAssetRef = "effects/apply_stun", MetadataJson = "{}" }
    };
}
