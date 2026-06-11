using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCardArtAndVisualSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "card_visual_profile_assignments");

            migrationBuilder.DropTable(
                name: "card_visual_profile_templates");

            migrationBuilder.DropColumn(
                name: "icon_asset_ref",
                table: "status_effect_kind_definitions");

            migrationBuilder.DropColumn(
                name: "ui_color_hex",
                table: "status_effect_kind_definitions");

            migrationBuilder.DropColumn(
                name: "vfx_cue_id",
                table: "status_effect_kind_definitions");

            migrationBuilder.DropColumn(
                name: "battle_presentation_json",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "visual_profiles_json",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "audio_cue_id",
                table: "abilities");

            migrationBuilder.DropColumn(
                name: "icon_asset_ref",
                table: "abilities");

            migrationBuilder.DropColumn(
                name: "status_icon_asset_ref",
                table: "abilities");

            migrationBuilder.DropColumn(
                name: "ui_color_hex",
                table: "abilities");

            migrationBuilder.DropColumn(
                name: "vfx_cue_id",
                table: "abilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "icon_asset_ref",
                table: "status_effect_kind_definitions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ui_color_hex",
                table: "status_effect_kind_definitions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vfx_cue_id",
                table: "status_effect_kind_definitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "battle_presentation_json",
                table: "cards",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "visual_profiles_json",
                table: "cards",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "audio_cue_id",
                table: "abilities",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "icon_asset_ref",
                table: "abilities",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_icon_asset_ref",
                table: "abilities",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ui_color_hex",
                table: "abilities",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vfx_cue_id",
                table: "abilities",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "card_visual_profile_templates",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    layers_json = table.Column<string>(type: "jsonb", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    profile_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_visual_profile_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "card_visual_profile_assignments",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    card_definition_id = table.Column<string>(type: "text", nullable: false),
                    template_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    override_display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    override_layers_json = table.Column<string>(type: "jsonb", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_visual_profile_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_card_visual_profile_assignments_card_visual_profile_templat~",
                        column: x => x.template_id,
                        principalTable: "card_visual_profile_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_card_visual_profile_assignments_cards_card_definition_id",
                        column: x => x.card_definition_id,
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "status_effect_kind_definitions",
                keyColumn: "id",
                keyValue: 0,
                columns: new[] { "icon_asset_ref", "ui_color_hex", "vfx_cue_id" },
                values: new object[] { "status/poisoned", "#62B357", "vfx_status_poison" });

            migrationBuilder.UpdateData(
                table: "status_effect_kind_definitions",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "icon_asset_ref", "ui_color_hex", "vfx_cue_id" },
                values: new object[] { "status/stunned", "#F2D14B", "vfx_status_stun" });

            migrationBuilder.UpdateData(
                table: "status_effect_kind_definitions",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "icon_asset_ref", "ui_color_hex", "vfx_cue_id" },
                values: new object[] { "status/shielded", "#61A8FF", "vfx_status_shield" });

            migrationBuilder.UpdateData(
                table: "status_effect_kind_definitions",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "icon_asset_ref", "ui_color_hex", "vfx_cue_id" },
                values: new object[] { "status/enrage_cooldown", "#FF6A3D", "vfx_status_enrage_cooldown" });

            migrationBuilder.CreateIndex(
                name: "IX_card_visual_profile_assignments_card_definition_id_template~",
                table: "card_visual_profile_assignments",
                columns: new[] { "card_definition_id", "template_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_card_visual_profile_assignments_template_id",
                table: "card_visual_profile_assignments",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_card_visual_profile_templates_profile_key",
                table: "card_visual_profile_templates",
                column: "profile_key",
                unique: true);
        }
    }
}
