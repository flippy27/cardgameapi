using Microsoft.EntityFrameworkCore.Migrations;
using CardDuel.ServerApi.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
#nullable disable

namespace CardDuel.ServerApi.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20260425103000_AddAuthoringLookupsAndVisualProfileTemplates")]
public partial class AddAuthoringLookupsAndVisualProfileTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("audio_cue_id", "abilities", "character varying(128)", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<string>("icon_asset_ref", "abilities", "character varying(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<string>("status_icon_asset_ref", "abilities", "character varying(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<string>("tooltip_summary", "abilities", "character varying(512)", maxLength: 512, nullable: true);
        migrationBuilder.AddColumn<string>("ui_color_hex", "abilities", "character varying(16)", maxLength: 16, nullable: true);
        migrationBuilder.AddColumn<string>("vfx_cue_id", "abilities", "character varying(128)", maxLength: 128, nullable: true);

        migrationBuilder.CreateTable(
            name: "ability_skill_type_definitions",
            columns: table => new
            {
                id = table.Column<int>(nullable: false),
                key = table.Column<string>(maxLength: 64, nullable: false),
                display_name = table.Column<string>(maxLength: 128, nullable: false),
                description = table.Column<string>(maxLength: 1024, nullable: false),
                category = table.Column<string>(maxLength: 64, nullable: false),
                icon_asset_ref = table.Column<string>(maxLength: 255, nullable: true),
                metadata_json = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_ability_skill_type_definitions", x => x.id));

        migrationBuilder.CreateTable(
            name: "ability_trigger_kind_definitions",
            columns: table => new
            {
                id = table.Column<int>(nullable: false),
                key = table.Column<string>(maxLength: 64, nullable: false),
                display_name = table.Column<string>(maxLength: 128, nullable: false),
                description = table.Column<string>(maxLength: 1024, nullable: false),
                category = table.Column<string>(maxLength: 64, nullable: false),
                icon_asset_ref = table.Column<string>(maxLength: 255, nullable: true),
                metadata_json = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_ability_trigger_kind_definitions", x => x.id));

        migrationBuilder.CreateTable(
            name: "target_selector_kind_definitions",
            columns: table => new
            {
                id = table.Column<int>(nullable: false),
                key = table.Column<string>(maxLength: 64, nullable: false),
                display_name = table.Column<string>(maxLength: 128, nullable: false),
                description = table.Column<string>(maxLength: 1024, nullable: false),
                category = table.Column<string>(maxLength: 64, nullable: false),
                icon_asset_ref = table.Column<string>(maxLength: 255, nullable: true),
                metadata_json = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_target_selector_kind_definitions", x => x.id));

        migrationBuilder.CreateTable(
            name: "status_effect_kind_definitions",
            columns: table => new
            {
                id = table.Column<int>(nullable: false),
                key = table.Column<string>(maxLength: 64, nullable: false),
                display_name = table.Column<string>(maxLength: 128, nullable: false),
                description = table.Column<string>(maxLength: 1024, nullable: false),
                category = table.Column<string>(maxLength: 64, nullable: false),
                icon_asset_ref = table.Column<string>(maxLength: 255, nullable: true),
                vfx_cue_id = table.Column<string>(maxLength: 128, nullable: true),
                ui_color_hex = table.Column<string>(maxLength: 16, nullable: true),
                metadata_json = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_status_effect_kind_definitions", x => x.id));

        migrationBuilder.CreateTable(
            name: "effect_kind_definitions",
            columns: table => new
            {
                id = table.Column<int>(nullable: false),
                key = table.Column<string>(maxLength: 64, nullable: false),
                display_name = table.Column<string>(maxLength: 128, nullable: false),
                description = table.Column<string>(maxLength: 1024, nullable: false),
                category = table.Column<string>(maxLength: 64, nullable: false),
                produces_status_kind = table.Column<int>(nullable: true),
                icon_asset_ref = table.Column<string>(maxLength: 255, nullable: true),
                metadata_json = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_effect_kind_definitions", x => x.id);
                table.ForeignKey("fk_effect_kind_definitions_status_effect_kind_definitions_produces_status_kind", x => x.produces_status_kind, "status_effect_kind_definitions", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "card_visual_profile_templates",
            columns: table => new
            {
                id = table.Column<string>(nullable: false),
                profile_key = table.Column<string>(maxLength: 128, nullable: false),
                display_name = table.Column<string>(maxLength: 128, nullable: false),
                description = table.Column<string>(maxLength: 1024, nullable: false),
                is_active = table.Column<bool>(nullable: false),
                layers_json = table.Column<string>(type: "jsonb", nullable: false),
                metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTimeOffset>(nullable: false),
                updated_at = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_card_visual_profile_templates", x => x.id));

        migrationBuilder.CreateTable(
            name: "card_visual_profile_assignments",
            columns: table => new
            {
                id = table.Column<string>(nullable: false),
                card_definition_id = table.Column<string>(nullable: false),
                template_id = table.Column<string>(nullable: false),
                is_default = table.Column<bool>(nullable: false),
                override_display_name = table.Column<string>(maxLength: 128, nullable: true),
                override_layers_json = table.Column<string>(type: "jsonb", nullable: true),
                metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTimeOffset>(nullable: false),
                updated_at = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_card_visual_profile_assignments", x => x.id);
                table.ForeignKey("fk_card_visual_profile_assignments_cards_card_definition_id", x => x.card_definition_id, "cards", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_card_visual_profile_assignments_card_visual_profile_templates_template_id", x => x.template_id, "card_visual_profile_templates", "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql("""
            INSERT INTO status_effect_kind_definitions (id, key, display_name, description, category, icon_asset_ref, vfx_cue_id, ui_color_hex, metadata_json) VALUES
            (0, 'poison', 'Poisoned', 'Debuff. Deals damage over time at battle-start processing.', 'debuff', 'status/poisoned', 'vfx_status_poison', '#62B357', '{"clientIndicator":"poisoned"}'),
            (1, 'stun', 'Stunned', 'Debuff. The affected card skips its next attack, then the status is consumed.', 'debuff', 'status/stunned', 'vfx_status_stun', '#F2D14B', '{"clientIndicator":"stunned"}'),
            (2, 'shield', 'Shielded', 'Buff. Blocks incoming damage charges before health/armor are modified.', 'buff', 'status/shielded', 'vfx_status_shield', '#61A8FF', '{"clientIndicator":"shielded"}'),
            (3, 'enrage_cooldown', 'Enrage Cooldown', 'Internal debuff/cooldown. Applied after enrage attacks twice; card skips its next attack.', 'cooldown', 'status/enrage_cooldown', 'vfx_status_enrage_cooldown', '#FF6A3D', '{"clientIndicator":"enrageCooldown"}')
            ON CONFLICT (id) DO NOTHING;
            """);

        migrationBuilder.Sql("""
            INSERT INTO ability_skill_type_definitions (id, key, display_name, description, category, icon_asset_ref, metadata_json) VALUES
            (0, 'defensive', 'Defensive', 'Protects, heals, shields, redirects, or improves survivability.', 'ability', 'ui/skill_type/defensive', '{}'),
            (1, 'offensive', 'Offensive', 'Deals damage, applies harmful status, or improves attacks.', 'ability', 'ui/skill_type/offensive', '{}'),
            (2, 'equipable', 'Equipable', 'Equipment-style or attachment-style ability.', 'ability', 'ui/skill_type/equipable', '{}'),
            (3, 'utility', 'Utility', 'Support behavior that is not purely offensive/defensive.', 'ability', 'ui/skill_type/utility', '{}'),
            (4, 'modifier', 'Modifier', 'Modifies normal attack behavior, targeting, or timing.', 'ability', 'ui/skill_type/modifier', '{}')
            ON CONFLICT (id) DO NOTHING;
            """);

        migrationBuilder.Sql("""
            INSERT INTO ability_trigger_kind_definitions (id, key, display_name, description, category, icon_asset_ref, metadata_json) VALUES
            (0, 'on_play', 'On Play', 'Runs when the card is played from hand onto the board.', 'trigger', 'ui/trigger/on_play', '{}'),
            (1, 'on_turn_start', 'On Turn Start', 'Runs at the start of the owning player turn.', 'trigger', 'ui/trigger/on_turn_start', '{}'),
            (2, 'on_turn_end', 'On Turn End', 'Runs at the end of the owning player turn.', 'trigger', 'ui/trigger/on_turn_end', '{}'),
            (3, 'on_battle_phase', 'On Battle Phase', 'Runs during battle phase before/around normal attack resolution.', 'trigger', 'ui/trigger/on_battle_phase', '{}')
            ON CONFLICT (id) DO NOTHING;
            """);

        migrationBuilder.Sql("""
            INSERT INTO target_selector_kind_definitions (id, key, display_name, description, category, icon_asset_ref, metadata_json) VALUES
            (0, 'self', 'Self', 'Targets the card that owns the ability.', 'selector', 'ui/selector/self', '{}'),
            (1, 'frontline_first', 'Frontline First', 'Targets enemy frontline first; taunt may force this selector.', 'selector', 'ui/selector/frontline_first', '{}'),
            (2, 'backline_first', 'Backline First', 'Targets backline enemies before frontline.', 'selector', 'ui/selector/backline_first', '{}'),
            (3, 'all_enemies', 'All Enemies', 'Targets all enemy board cards.', 'selector', 'ui/selector/all_enemies', '{}'),
            (4, 'lowest_health_ally', 'Lowest Health Ally', 'Targets allied card with lowest current health.', 'selector', 'ui/selector/lowest_health_ally', '{}'),
            (5, 'ally_front', 'Ally Front', 'Targets allied frontline slot.', 'selector', 'ui/selector/ally_front', '{}'),
            (6, 'ally_back_left', 'Ally Back Left', 'Targets allied back-left slot.', 'selector', 'ui/selector/ally_back_left', '{}'),
            (7, 'ally_back_right', 'Ally Back Right', 'Targets allied back-right slot.', 'selector', 'ui/selector/ally_back_right', '{}'),
            (8, 'source_opponent', 'Source Opponent', 'Targets the current opposing card for attack modifiers.', 'selector', 'ui/selector/source_opponent', '{}')
            ON CONFLICT (id) DO NOTHING;
            """);

        migrationBuilder.Sql("""
            INSERT INTO effect_kind_definitions (id, key, display_name, description, category, produces_status_kind, icon_asset_ref, metadata_json) VALUES
            (0, 'damage', 'Damage', 'Deals direct damage to target cards.', 'math', NULL, 'effects/damage', '{}'),
            (1, 'heal', 'Heal', 'Restores health to target cards.', 'math', NULL, 'effects/heal', '{}'),
            (2, 'gain_armor', 'Gain Armor', 'Adds armor to target cards.', 'math', NULL, 'effects/armor', '{}'),
            (3, 'buff_attack', 'Buff Attack', 'Increases attack value on target cards.', 'math', NULL, 'effects/buff_attack', '{}'),
            (4, 'hit_hero', 'Hit Hero', 'Deals damage directly to the opposing hero.', 'math', NULL, 'effects/hit_hero', '{}'),
            (5, 'stun', 'Stun Modifier', 'Legacy stun modifier.', 'modifier', 1, 'effects/stun', '{}'),
            (6, 'poison', 'Poison Modifier', 'Legacy poison modifier.', 'modifier', 0, 'effects/poison', '{}'),
            (7, 'leech', 'Leech', 'Normal attack modifier that heals source by health damage dealt.', 'modifier', NULL, 'effects/leech', '{}'),
            (8, 'evasion', 'Evasion', 'Reserved future evasion behavior.', 'reserved', NULL, 'effects/evasion', '{}'),
            (9, 'shield', 'Shield Modifier', 'Legacy shield modifier.', 'modifier', 2, 'effects/shield', '{}'),
            (10, 'reflection', 'Reflection', 'Reserved future reflection behavior.', 'reserved', NULL, 'effects/reflection', '{}'),
            (11, 'dodge', 'Dodge', 'Reserved future dodge behavior.', 'reserved', NULL, 'effects/dodge', '{}'),
            (12, 'enrage', 'Enrage', 'Attacks twice then receives enrage cooldown.', 'modifier', 3, 'effects/enrage', '{}'),
            (13, 'mana_burn', 'Mana Burn', 'Reserved future mana disruption.', 'reserved', NULL, 'effects/mana_burn', '{}'),
            (14, 'regenerate', 'Regenerate', 'Legacy regeneration family.', 'modifier', NULL, 'effects/regenerate', '{}'),
            (15, 'execute', 'Execute', 'Reserved future threshold kill behavior.', 'reserved', NULL, 'effects/execute', '{}'),
            (16, 'diagonal_attack', 'Diagonal Attack', 'Reserved future diagonal targeting.', 'reserved', NULL, 'effects/diagonal_attack', '{}'),
            (17, 'fly', 'Fly', 'Bypasses non-flying defender rules.', 'modifier', NULL, 'effects/fly', '{}'),
            (18, 'armor', 'Armor', 'Legacy armor family.', 'modifier', NULL, 'effects/armor', '{}'),
            (19, 'chain', 'Chain', 'Reserved future chained effects.', 'reserved', NULL, 'effects/chain', '{}'),
            (20, 'charge', 'Charge', 'Reserved future immediate attack timing.', 'reserved', NULL, 'effects/charge', '{}'),
            (21, 'cleave', 'Cleave', 'Reserved future multi-target melee damage.', 'reserved', NULL, 'effects/cleave', '{}'),
            (22, 'last_stand', 'Last Stand', 'Reserved future low-health behavior.', 'reserved', NULL, 'effects/last_stand', '{}'),
            (23, 'melee_range', 'Melee Range', 'Reserved future melee/range constraints.', 'reserved', NULL, 'effects/melee_range', '{}'),
            (24, 'ricochet', 'Ricochet', 'Reserved future bouncing projectiles.', 'reserved', NULL, 'effects/ricochet', '{}'),
            (25, 'taunt', 'Taunt', 'Targeting modifier that attracts frontline-first selectors.', 'modifier', NULL, 'effects/taunt', '{}'),
            (26, 'trample', 'Trample', 'Ignores armor during damage resolution.', 'modifier', NULL, 'effects/trample', '{}'),
            (27, 'haste', 'Haste', 'Allows attack before usual wait.', 'modifier', NULL, 'effects/haste', '{}'),
            (28, 'add_shield', 'Add Shield', 'Applies shielded status charges.', 'status', 2, 'effects/add_shield', '{}'),
            (29, 'apply_poison', 'Apply Poison', 'Applies poisoned status.', 'status', 0, 'effects/apply_poison', '{}'),
            (30, 'apply_stun', 'Apply Stun', 'Applies stunned status.', 'status', 1, 'effects/apply_stun', '{}')
            ON CONFLICT (id) DO NOTHING;
            """);

        migrationBuilder.CreateIndex("ix_ability_skill_type_definitions_key", "ability_skill_type_definitions", "key", unique: true);
        migrationBuilder.CreateIndex("ix_ability_trigger_kind_definitions_key", "ability_trigger_kind_definitions", "key", unique: true);
        migrationBuilder.CreateIndex("ix_target_selector_kind_definitions_key", "target_selector_kind_definitions", "key", unique: true);
        migrationBuilder.CreateIndex("ix_status_effect_kind_definitions_key", "status_effect_kind_definitions", "key", unique: true);
        migrationBuilder.CreateIndex("ix_effect_kind_definitions_key", "effect_kind_definitions", "key", unique: true);
        migrationBuilder.CreateIndex("ix_effect_kind_definitions_produces_status_kind", "effect_kind_definitions", "produces_status_kind");
        migrationBuilder.CreateIndex("ix_card_visual_profile_templates_profile_key", "card_visual_profile_templates", "profile_key", unique: true);
        migrationBuilder.CreateIndex("ix_card_visual_profile_assignments_card_definition_id_template_id", "card_visual_profile_assignments", new[] { "card_definition_id", "template_id" }, unique: true);
        migrationBuilder.CreateIndex("ix_card_visual_profile_assignments_template_id", "card_visual_profile_assignments", "template_id");
        migrationBuilder.CreateIndex("ix_abilities_skill_type", "abilities", "skill_type");
        migrationBuilder.CreateIndex("ix_abilities_trigger_kind", "abilities", "trigger_kind");
        migrationBuilder.CreateIndex("ix_abilities_target_selector_kind", "abilities", "target_selector_kind");
        migrationBuilder.CreateIndex("ix_effects_effect_kind", "effects", "effect_kind");
        migrationBuilder.CreateIndex("ix_effects_target_selector_kind_override", "effects", "target_selector_kind_override");

        migrationBuilder.AddForeignKey("fk_abilities_ability_skill_type_definitions_skill_type", "abilities", "skill_type", "ability_skill_type_definitions", principalColumn: "id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_abilities_ability_trigger_kind_definitions_trigger_kind", "abilities", "trigger_kind", "ability_trigger_kind_definitions", principalColumn: "id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_abilities_target_selector_kind_definitions_target_selector_kind", "abilities", "target_selector_kind", "target_selector_kind_definitions", principalColumn: "id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_effects_effect_kind_definitions_effect_kind", "effects", "effect_kind", "effect_kind_definitions", principalColumn: "id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_effects_target_selector_kind_definitions_target_selector_kind_override", "effects", "target_selector_kind_override", "target_selector_kind_definitions", principalColumn: "id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("fk_effects_target_selector_kind_definitions_target_selector_kind_override", "effects");
        migrationBuilder.DropForeignKey("fk_effects_effect_kind_definitions_effect_kind", "effects");
        migrationBuilder.DropForeignKey("fk_abilities_target_selector_kind_definitions_target_selector_kind", "abilities");
        migrationBuilder.DropForeignKey("fk_abilities_ability_trigger_kind_definitions_trigger_kind", "abilities");
        migrationBuilder.DropForeignKey("fk_abilities_ability_skill_type_definitions_skill_type", "abilities");
        migrationBuilder.DropTable("card_visual_profile_assignments");
        migrationBuilder.DropTable("effect_kind_definitions");
        migrationBuilder.DropTable("card_visual_profile_templates");
        migrationBuilder.DropTable("status_effect_kind_definitions");
        migrationBuilder.DropTable("target_selector_kind_definitions");
        migrationBuilder.DropTable("ability_trigger_kind_definitions");
        migrationBuilder.DropTable("ability_skill_type_definitions");
        migrationBuilder.DropIndex("ix_effects_target_selector_kind_override", "effects");
        migrationBuilder.DropIndex("ix_effects_effect_kind", "effects");
        migrationBuilder.DropIndex("ix_abilities_target_selector_kind", "abilities");
        migrationBuilder.DropIndex("ix_abilities_trigger_kind", "abilities");
        migrationBuilder.DropIndex("ix_abilities_skill_type", "abilities");
        migrationBuilder.DropColumn("audio_cue_id", "abilities");
        migrationBuilder.DropColumn("icon_asset_ref", "abilities");
        migrationBuilder.DropColumn("status_icon_asset_ref", "abilities");
        migrationBuilder.DropColumn("tooltip_summary", "abilities");
        migrationBuilder.DropColumn("ui_color_hex", "abilities");
        migrationBuilder.DropColumn("vfx_cue_id", "abilities");
    }
}
