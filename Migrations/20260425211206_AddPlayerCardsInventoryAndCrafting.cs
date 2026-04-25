using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CardDuel.ServerApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerCardsInventoryAndCrafting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Guard: snapshot drift fix ────────────────────────────────────
            // The EF snapshot in ConvertToSnakeCase.Designer was incomplete.
            // We use IF NOT EXISTS / ON CONFLICT DO NOTHING throughout so this
            // migration is idempotent on both a fresh DB and the existing one.

            // ── Ensure pre-existing lookup tables exist (from earlier migrations) ─

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ability_skill_type_definitions (
                    id integer NOT NULL,
                    key character varying(64) NOT NULL,
                    display_name character varying(128) NOT NULL,
                    description character varying(1024) NOT NULL,
                    category character varying(64) NOT NULL,
                    icon_asset_ref character varying(255),
                    metadata_json jsonb NOT NULL,
                    CONSTRAINT ""PK_ability_skill_type_definitions"" PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ability_skill_type_definitions_key""
                    ON ability_skill_type_definitions (key);

                CREATE TABLE IF NOT EXISTS ability_trigger_kind_definitions (
                    id integer NOT NULL,
                    key character varying(64) NOT NULL,
                    display_name character varying(128) NOT NULL,
                    description character varying(1024) NOT NULL,
                    category character varying(64) NOT NULL,
                    icon_asset_ref character varying(255),
                    metadata_json jsonb NOT NULL,
                    CONSTRAINT ""PK_ability_trigger_kind_definitions"" PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ability_trigger_kind_definitions_key""
                    ON ability_trigger_kind_definitions (key);

                CREATE TABLE IF NOT EXISTS target_selector_kind_definitions (
                    id integer NOT NULL,
                    key character varying(64) NOT NULL,
                    display_name character varying(128) NOT NULL,
                    description character varying(1024) NOT NULL,
                    category character varying(64) NOT NULL,
                    icon_asset_ref character varying(255),
                    metadata_json jsonb NOT NULL,
                    CONSTRAINT ""PK_target_selector_kind_definitions"" PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_target_selector_kind_definitions_key""
                    ON target_selector_kind_definitions (key);

                CREATE TABLE IF NOT EXISTS status_effect_kind_definitions (
                    id integer NOT NULL,
                    key character varying(64) NOT NULL,
                    display_name character varying(128) NOT NULL,
                    description character varying(1024) NOT NULL,
                    category character varying(64) NOT NULL,
                    icon_asset_ref character varying(255),
                    vfx_cue_id character varying(128),
                    ui_color_hex character varying(16),
                    metadata_json jsonb NOT NULL,
                    CONSTRAINT ""PK_status_effect_kind_definitions"" PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_status_effect_kind_definitions_key""
                    ON status_effect_kind_definitions (key);

                CREATE TABLE IF NOT EXISTS effect_kind_definitions (
                    id integer NOT NULL,
                    key character varying(64) NOT NULL,
                    display_name character varying(128) NOT NULL,
                    description character varying(1024) NOT NULL,
                    category character varying(64) NOT NULL,
                    produces_status_kind integer,
                    icon_asset_ref character varying(255),
                    metadata_json jsonb NOT NULL,
                    CONSTRAINT ""PK_effect_kind_definitions"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_effect_kind_definitions_status_effect_kind_definitions_prod~""
                        FOREIGN KEY (produces_status_kind)
                        REFERENCES status_effect_kind_definitions (id)
                        ON DELETE RESTRICT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_effect_kind_definitions_key""
                    ON effect_kind_definitions (key);
                CREATE INDEX IF NOT EXISTS ""IX_effect_kind_definitions_produces_status_kind""
                    ON effect_kind_definitions (produces_status_kind);

                CREATE TABLE IF NOT EXISTS card_visual_profile_templates (
                    id text NOT NULL,
                    profile_key character varying(128) NOT NULL,
                    display_name character varying(128) NOT NULL,
                    description character varying(1024) NOT NULL,
                    is_active boolean NOT NULL,
                    layers_json jsonb NOT NULL,
                    metadata_json jsonb NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone,
                    CONSTRAINT ""PK_card_visual_profile_templates"" PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_card_visual_profile_templates_profile_key""
                    ON card_visual_profile_templates (profile_key);

                CREATE TABLE IF NOT EXISTS card_visual_profile_assignments (
                    id text NOT NULL,
                    card_definition_id text NOT NULL,
                    template_id text NOT NULL,
                    is_default boolean NOT NULL,
                    override_display_name character varying(128),
                    override_layers_json jsonb,
                    metadata_json jsonb NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone,
                    CONSTRAINT ""PK_card_visual_profile_assignments"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_card_visual_profile_assignments_card_visual_profile_templat~""
                        FOREIGN KEY (template_id)
                        REFERENCES card_visual_profile_templates (id)
                        ON DELETE CASCADE,
                    CONSTRAINT ""FK_card_visual_profile_assignments_cards_card_definition_id""
                        FOREIGN KEY (card_definition_id)
                        REFERENCES cards (id)
                        ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_card_visual_profile_assignments_card_definition_id_template~""
                    ON card_visual_profile_assignments (card_definition_id, template_id);
                CREATE INDEX IF NOT EXISTS ""IX_card_visual_profile_assignments_template_id""
                    ON card_visual_profile_assignments (template_id);
            ");

            // ── Seed lookup data (ON CONFLICT DO NOTHING = idempotent) ────────

            migrationBuilder.Sql(@"
                INSERT INTO ability_skill_type_definitions (id, key, display_name, description, category, icon_asset_ref, metadata_json) VALUES
                    (0, 'defensive', 'Defensive', 'Ability primarily protects, heals, shields, redirects, or improves survivability.', 'ability', 'ui/skill_type/defensive', '{}'),
                    (1, 'offensive',  'Offensive',  'Ability primarily deals damage, applies harmful status, or improves attacks.',                     'ability', 'ui/skill_type/offensive',  '{}'),
                    (2, 'equipable',  'Equipable',  'Ability is intended for equipment-style or attachment-style cards.',                                'ability', 'ui/skill_type/equipable',  '{}'),
                    (3, 'utility',    'Utility',    'Ability performs support behavior that is not purely offensive/defensive.',                         'ability', 'ui/skill_type/utility',    '{}'),
                    (4, 'modifier',   'Modifier',   'Ability modifies normal attack behavior, targeting, or timing instead of resolving as standalone math.', 'ability', 'ui/skill_type/modifier', '{}')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO ability_trigger_kind_definitions (id, key, display_name, description, category, icon_asset_ref, metadata_json) VALUES
                    (0, 'on_play',         'On Play',         'Runs when the card is played from hand onto the board.',                                                                           'trigger', 'ui/trigger/on_play',         '{}'),
                    (1, 'on_turn_start',   'On Turn Start',   'Runs at the start of the owning player''s turn.',                                                                                 'trigger', 'ui/trigger/on_turn_start',   '{}'),
                    (2, 'on_turn_end',     'On Turn End',     'Runs at the end of the owning player''s turn.',                                                                                   'trigger', 'ui/trigger/on_turn_end',     '{}'),
                    (3, 'on_battle_phase', 'On Battle Phase', 'Runs during battle phase before/around normal attack resolution, depending on the engine rule.', 'trigger', 'ui/trigger/on_battle_phase', '{}')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO target_selector_kind_definitions (id, key, display_name, description, category, icon_asset_ref, metadata_json) VALUES
                    (0, 'self',               'Self',               'Targets the card that owns the ability.',                                                             'selector', 'ui/selector/self',               '{}'),
                    (1, 'frontline_first',    'Frontline First',    'Targets enemy frontline first; taunt may force this selector to a taunting unit.',                    'selector', 'ui/selector/frontline_first',    '{}'),
                    (2, 'backline_first',     'Backline First',     'Targets back-left/back-right enemies before frontline depending on board availability.',              'selector', 'ui/selector/backline_first',     '{}'),
                    (3, 'all_enemies',        'All Enemies',        'Targets all enemy board cards.',                                                                      'selector', 'ui/selector/all_enemies',        '{}'),
                    (4, 'lowest_health_ally', 'Lowest Health Ally', 'Targets the allied board card with the lowest current health.',                                       'selector', 'ui/selector/lowest_health_ally', '{}'),
                    (5, 'ally_front',         'Ally Front',         'Targets allied frontline slot.',                                                                      'selector', 'ui/selector/ally_front',         '{}'),
                    (6, 'ally_back_left',     'Ally Back Left',     'Targets allied back-left slot.',                                                                      'selector', 'ui/selector/ally_back_left',     '{}'),
                    (7, 'ally_back_right',    'Ally Back Right',    'Targets allied back-right slot.',                                                                     'selector', 'ui/selector/ally_back_right',    '{}'),
                    (8, 'source_opponent',    'Source Opponent',    'Targets the current opposing card for normal attack modifier effects such as poison/stun.',           'selector', 'ui/selector/source_opponent',    '{}')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO status_effect_kind_definitions (id, key, display_name, description, category, icon_asset_ref, vfx_cue_id, ui_color_hex, metadata_json) VALUES
                    (0, 'poison',          'Poisoned',        'Debuff. Deals damage over time at battle-start processing.',                                              'debuff',   'status/poisoned',        'vfx_status_poison',          '#62B357', '{""clientIndicator"":""poisoned""}'),
                    (1, 'stun',            'Stunned',         'Debuff. The affected card skips its next attack, then the status is consumed.',                           'debuff',   'status/stunned',         'vfx_status_stun',            '#F2D14B', '{""clientIndicator"":""stunned""}'),
                    (2, 'shield',          'Shielded',        'Buff. Blocks incoming damage charges before health/armor are modified.',                                  'buff',     'status/shielded',        'vfx_status_shield',          '#61A8FF', '{""clientIndicator"":""shielded""}'),
                    (3, 'enrage_cooldown', 'Enrage Cooldown', 'Internal debuff/cooldown. Applied after enrage attacks twice; card skips its next attack.',               'cooldown', 'status/enrage_cooldown', 'vfx_status_enrage_cooldown', '#FF6A3D', '{""clientIndicator"":""enrageCooldown""}')
                ON CONFLICT (id) DO NOTHING;

                INSERT INTO effect_kind_definitions (id, key, display_name, description, category, icon_asset_ref, metadata_json, produces_status_kind) VALUES
                    (0,  'damage',          'Damage',           'Deals direct damage to one or more target cards.',                               'math',     'effects/damage',          '{}', NULL),
                    (1,  'heal',            'Heal',             'Restores health to a target card.',                                              'math',     'effects/heal',            '{}', NULL),
                    (2,  'gain_armor',      'Gain Armor',       'Adds armor to a card.',                                                          'math',     'effects/armor',           '{}', NULL),
                    (3,  'buff_attack',     'Buff Attack',      'Increases attack value on a card.',                                              'math',     'effects/buff_attack',     '{}', NULL),
                    (4,  'hit_hero',        'Hit Hero',         'Deals damage directly to the opposing hero.',                                    'math',     'effects/hit_hero',        '{}', NULL),
                    (5,  'stun',            'Stun Modifier',    'Legacy modifier kind for stun-style behavior.',                                  'modifier', 'effects/stun',            '{}', 1),
                    (6,  'poison',          'Poison Modifier',  'Legacy modifier kind for poison-style behavior.',                                'modifier', 'effects/poison',          '{}', 0),
                    (7,  'leech',           'Leech',            'Normal attack modifier. Heals source by health damage dealt.',                   'modifier', 'effects/leech',           '{}', NULL),
                    (8,  'evasion',         'Evasion',          'Reserved effect kind for future evasion behavior.',                              'reserved', 'effects/evasion',         '{}', NULL),
                    (9,  'shield',          'Shield Modifier',  'Legacy modifier kind for shield behavior.',                                      'modifier', 'effects/shield',          '{}', 2),
                    (10, 'reflection',      'Reflection',       'Reserved effect kind for future reflection behavior.',                           'reserved', 'effects/reflection',      '{}', NULL),
                    (11, 'dodge',           'Dodge',            'Reserved effect kind for future dodge behavior.',                                'reserved', 'effects/dodge',           '{}', NULL),
                    (12, 'enrage',          'Enrage',           'Normal attack modifier. Attacks twice then receives enrage cooldown.',            'modifier', 'effects/enrage',          '{}', 3),
                    (13, 'mana_burn',       'Mana Burn',        'Reserved effect kind for future mana disruption.',                               'reserved', 'effects/mana_burn',       '{}', NULL),
                    (14, 'regenerate',      'Regenerate',       'Legacy regeneration effect family.',                                             'modifier', 'effects/regenerate',      '{}', NULL),
                    (15, 'execute',         'Execute',          'Reserved effect kind for threshold kill behavior.',                              'reserved', 'effects/execute',         '{}', NULL),
                    (16, 'diagonal_attack', 'Diagonal Attack',  'Reserved effect kind for diagonal targeting behavior.',                          'reserved', 'effects/diagonal_attack', '{}', NULL),
                    (17, 'fly',             'Fly',              'Normal attack modifier. Bypasses non-flying defender rules.',                    'modifier', 'effects/fly',             '{}', NULL),
                    (18, 'armor',           'Armor',            'Legacy armor effect family.',                                                    'modifier', 'effects/armor',           '{}', NULL),
                    (19, 'chain',           'Chain',            'Reserved effect kind for chained attacks/effects.',                              'reserved', 'effects/chain',           '{}', NULL),
                    (20, 'charge',          'Charge',           'Reserved effect kind for immediate attack timing.',                              'reserved', 'effects/charge',          '{}', NULL),
                    (21, 'cleave',          'Cleave',           'Reserved effect kind for multi-target melee damage.',                            'reserved', 'effects/cleave',          '{}', NULL),
                    (22, 'last_stand',      'Last Stand',       'Reserved effect kind for low-health behavior.',                                  'reserved', 'effects/last_stand',      '{}', NULL),
                    (23, 'melee_range',     'Melee Range',      'Reserved effect kind for melee/range constraints.',                              'reserved', 'effects/melee_range',     '{}', NULL),
                    (24, 'ricochet',        'Ricochet',         'Reserved effect kind for bouncing projectiles.',                                 'reserved', 'effects/ricochet',        '{}', NULL),
                    (25, 'taunt',           'Taunt',            'Targeting modifier. Enemy frontline-first selectors prefer taunting cards.',     'modifier', 'effects/taunt',           '{}', NULL),
                    (26, 'trample',         'Trample',          'Normal attack modifier. Ignores armor during damage resolution.',                'modifier', 'effects/trample',         '{}', NULL),
                    (27, 'haste',           'Haste',            'Normal attack timing modifier. Allows attack before the usual wait.',            'modifier', 'effects/haste',           '{}', NULL),
                    (28, 'add_shield',      'Add Shield',       'Applies shielded status charges to a target.',                                  'status',   'effects/add_shield',      '{}', 2),
                    (29, 'apply_poison',    'Apply Poison',     'Applies poisoned status to a target.',                                           'status',   'effects/apply_poison',    '{}', 0),
                    (30, 'apply_stun',      'Apply Stun',       'Applies stunned status to a target.',                                            'status',   'effects/apply_stun',      '{}', 1)
                ON CONFLICT (id) DO NOTHING;
            ");

            // ── Fix abilities columns (may already exist if DB is up-to-date) ─

            migrationBuilder.Sql(@"
                ALTER TABLE abilities ADD COLUMN IF NOT EXISTS audio_cue_id character varying(128);
                ALTER TABLE abilities ADD COLUMN IF NOT EXISTS icon_asset_ref character varying(255);
                ALTER TABLE abilities ADD COLUMN IF NOT EXISTS status_icon_asset_ref character varying(255);
                ALTER TABLE abilities ADD COLUMN IF NOT EXISTS tooltip_summary character varying(512);
                ALTER TABLE abilities ADD COLUMN IF NOT EXISTS ui_color_hex character varying(16);
                ALTER TABLE abilities ADD COLUMN IF NOT EXISTS vfx_cue_id character varying(128);
            ");

            // ── Fix abilities foreign keys (may already exist) ────────────────

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_abilities_skill_type""
                    ON abilities (skill_type);
                CREATE INDEX IF NOT EXISTS ""IX_abilities_trigger_kind""
                    ON abilities (trigger_kind);
                CREATE INDEX IF NOT EXISTS ""IX_abilities_target_selector_kind""
                    ON abilities (target_selector_kind);

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_abilities_ability_skill_type_definitions_skill_type') THEN
                        ALTER TABLE abilities ADD CONSTRAINT ""FK_abilities_ability_skill_type_definitions_skill_type""
                            FOREIGN KEY (skill_type) REFERENCES ability_skill_type_definitions (id) ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_abilities_ability_trigger_kind_definitions_trigger_kind') THEN
                        ALTER TABLE abilities ADD CONSTRAINT ""FK_abilities_ability_trigger_kind_definitions_trigger_kind""
                            FOREIGN KEY (trigger_kind) REFERENCES ability_trigger_kind_definitions (id) ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_abilities_target_selector_kind_definitions_target_selector_~') THEN
                        ALTER TABLE abilities ADD CONSTRAINT ""FK_abilities_target_selector_kind_definitions_target_selector_~""
                            FOREIGN KEY (target_selector_kind) REFERENCES target_selector_kind_definitions (id) ON DELETE RESTRICT;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS ""IX_effects_effect_kind""
                    ON effects (effect_kind);
                CREATE INDEX IF NOT EXISTS ""IX_effects_target_selector_kind_override""
                    ON effects (target_selector_kind_override);

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_effects_effect_kind_definitions_effect_kind') THEN
                        ALTER TABLE effects ADD CONSTRAINT ""FK_effects_effect_kind_definitions_effect_kind""
                            FOREIGN KEY (effect_kind) REFERENCES effect_kind_definitions (id) ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_effects_target_selector_kind_definitions_target_selector_ki~') THEN
                        ALTER TABLE effects ADD CONSTRAINT ""FK_effects_target_selector_kind_definitions_target_selector_ki~""
                            FOREIGN KEY (target_selector_kind_override) REFERENCES target_selector_kind_definitions (id) ON DELETE RESTRICT;
                    END IF;
                END $$;
            ");

            // ── NEW: item_type_definitions ────────────────────────────────────

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS item_type_definitions (
                    id integer NOT NULL,
                    key character varying(64) NOT NULL,
                    display_name character varying(128) NOT NULL,
                    description character varying(512) NOT NULL,
                    category character varying(64) NOT NULL,
                    max_stack integer NOT NULL DEFAULT -1,
                    is_active boolean NOT NULL DEFAULT true,
                    icon_asset_ref character varying(255),
                    metadata_json jsonb NOT NULL,
                    CONSTRAINT ""PK_item_type_definitions"" PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_item_type_definitions_key""
                    ON item_type_definitions (key);

                INSERT INTO item_type_definitions (id, key, display_name, description, category, max_stack, is_active, icon_asset_ref, metadata_json) VALUES
                    (0, 'card_dust',       'Card Dust',       'Basic crafting material earned by playing matches. Required for all card crafts.',                   'crafting', -1, true, 'ui/items/card_dust',       '{}'),
                    (1, 'arcane_shard',    'Arcane Shard',    'Uncommon crafting material. Required for rare and epic cards.',                                       'crafting', -1, true, 'ui/items/arcane_shard',    '{}'),
                    (2, 'essence_of_void', 'Essence of Void', 'Rare crafting material obtained from special events. Required for legendary cards and upgrades.',    'crafting', -1, true, 'ui/items/essence_of_void', '{}'),
                    (3, 'faction_ember',   'Ember Ember',     'Faction-specific material from Ember faction matches. Used in Ember card crafting.',                 'faction',  -1, true, 'ui/items/faction_ember',   '{""faction"":0}'),
                    (4, 'faction_tidal',   'Tidal Droplet',   'Faction-specific material from Tidal faction matches.',                                              'faction',  -1, true, 'ui/items/faction_tidal',   '{""faction"":1}'),
                    (5, 'faction_grove',   'Grove Seed',      'Faction-specific material from Grove faction matches.',                                              'faction',  -1, true, 'ui/items/faction_grove',   '{""faction"":2}'),
                    (6, 'faction_alloy',   'Alloy Scrap',     'Faction-specific material from Alloy faction matches.',                                              'faction',  -1, true, 'ui/items/faction_alloy',   '{""faction"":3}'),
                    (7, 'faction_void',    'Void Crystal',    'Faction-specific material from Void faction matches.',                                               'faction',  -1, true, 'ui/items/faction_void',    '{""faction"":4}'),
                    (8, 'upgrade_stone',   'Upgrade Stone',   'Used to apply stat upgrades to player-owned cards.',                                                 'upgrade',  -1, true, 'ui/items/upgrade_stone',   '{}'),
                    (9, 'ability_tome',    'Ability Tome',    'Rare item needed to add a new ability to an owned card.',                                             'upgrade',  -1, true, 'ui/items/ability_tome',    '{}')
                ON CONFLICT (id) DO NOTHING;
            ");

            // ── NEW: player_cards ─────────────────────────────────────────────

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS player_cards (
                    id text NOT NULL,
                    user_id text NOT NULL,
                    card_definition_id text NOT NULL,
                    acquired_from character varying(64) NOT NULL,
                    acquired_at timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_player_cards"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_player_cards_cards_card_definition_id""
                        FOREIGN KEY (card_definition_id) REFERENCES cards (id) ON DELETE RESTRICT,
                    CONSTRAINT ""FK_player_cards_users_user_id""
                        FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_player_cards_user_id""
                    ON player_cards (user_id);
                CREATE INDEX IF NOT EXISTS ""IX_player_cards_card_definition_id""
                    ON player_cards (card_definition_id);
                CREATE INDEX IF NOT EXISTS ""IX_player_cards_user_id_card_definition_id""
                    ON player_cards (user_id, card_definition_id);
            ");

            // ── NEW: player_card_upgrades ─────────────────────────────────────

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS player_card_upgrades (
                    id text NOT NULL,
                    player_card_id text NOT NULL,
                    upgrade_kind character varying(64) NOT NULL,
                    int_value integer,
                    string_value character varying(255),
                    applied_at timestamp with time zone NOT NULL,
                    applied_by character varying(64) NOT NULL,
                    note character varying(512),
                    CONSTRAINT ""PK_player_card_upgrades"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_player_card_upgrades_player_cards_player_card_id""
                        FOREIGN KEY (player_card_id) REFERENCES player_cards (id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_player_card_upgrades_player_card_id""
                    ON player_card_upgrades (player_card_id);
            ");

            // ── NEW: player_items ─────────────────────────────────────────────

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS player_items (
                    id text NOT NULL,
                    user_id text NOT NULL,
                    item_type_id integer NOT NULL,
                    quantity bigint NOT NULL DEFAULT 0,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_player_items"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_player_items_item_type_definitions_item_type_id""
                        FOREIGN KEY (item_type_id) REFERENCES item_type_definitions (id) ON DELETE RESTRICT,
                    CONSTRAINT ""FK_player_items_users_user_id""
                        FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_player_items_user_id_item_type_id""
                    ON player_items (user_id, item_type_id);
                CREATE INDEX IF NOT EXISTS ""IX_player_items_item_type_id""
                    ON player_items (item_type_id);
            ");

            // ── NEW: card_crafting_requirements ───────────────────────────────

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS card_crafting_requirements (
                    id text NOT NULL,
                    card_definition_id text NOT NULL,
                    item_type_id integer NOT NULL,
                    quantity_required integer NOT NULL,
                    CONSTRAINT ""PK_card_crafting_requirements"" PRIMARY KEY (id),
                    CONSTRAINT ""FK_card_crafting_requirements_cards_card_definition_id""
                        FOREIGN KEY (card_definition_id) REFERENCES cards (id) ON DELETE CASCADE,
                    CONSTRAINT ""FK_card_crafting_requirements_item_type_definitions_item_type_~""
                        FOREIGN KEY (item_type_id) REFERENCES item_type_definitions (id) ON DELETE RESTRICT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_card_crafting_requirements_card_definition_id_item_type_id""
                    ON card_crafting_requirements (card_definition_id, item_type_id);
                CREATE INDEX IF NOT EXISTS ""IX_card_crafting_requirements_item_type_id""
                    ON card_crafting_requirements (item_type_id);
            ");

            // ── deck_cards: ensure table exists, then add player_card_id ────────
            // NormalizeDeckCards (earlier migration) creates deck_cards.
            // Guard here so this migration is self-sufficient if that one was skipped/rolled back.

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS deck_cards (
                    id text NOT NULL,
                    deck_id text NOT NULL,
                    card_definition_id text NOT NULL,
                    position integer NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    CONSTRAINT ""pk_deck_cards"" PRIMARY KEY (id),
                    CONSTRAINT ""fk_deck_cards_cards_card_definition_id""
                        FOREIGN KEY (card_definition_id) REFERENCES cards (id) ON DELETE RESTRICT,
                    CONSTRAINT ""fk_deck_cards_decks_deck_id""
                        FOREIGN KEY (deck_id) REFERENCES decks (id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""ix_deck_cards_card_definition_id""
                    ON deck_cards (card_definition_id);
                CREATE INDEX IF NOT EXISTS ""ix_deck_cards_deck_id_position""
                    ON deck_cards (deck_id, position);
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE deck_cards ADD COLUMN IF NOT EXISTS player_card_id text;
                CREATE INDEX IF NOT EXISTS ""IX_deck_cards_player_card_id""
                    ON deck_cards (player_card_id);

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_deck_cards_player_cards_player_card_id') THEN
                        ALTER TABLE deck_cards ADD CONSTRAINT ""FK_deck_cards_player_cards_player_card_id""
                            FOREIGN KEY (player_card_id) REFERENCES player_cards (id) ON DELETE SET NULL;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove only what this migration owns. Pre-existing tables stay.
            migrationBuilder.Sql(@"
                ALTER TABLE deck_cards DROP CONSTRAINT IF EXISTS ""FK_deck_cards_player_cards_player_card_id"";
                DROP INDEX IF EXISTS ""IX_deck_cards_player_card_id"";
                ALTER TABLE deck_cards DROP COLUMN IF EXISTS player_card_id;

                DROP TABLE IF EXISTS card_crafting_requirements;
                DROP TABLE IF EXISTS player_card_upgrades;
                DROP TABLE IF EXISTS player_items;
                DROP TABLE IF EXISTS player_cards;
                DROP TABLE IF EXISTS item_type_definitions;
            ");
        }
    }
}
