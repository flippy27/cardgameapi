using CardDuel.ServerApi.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260421033000_AddGameRulesets")]
    public partial class AddGameRulesets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "game_ruleset_id",
                table: "matches",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "game_ruleset_name",
                table: "matches",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "game_rules_snapshot_json",
                table: "matches",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.CreateTable(
                name: "game_rulesets",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    ruleset_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    starting_hero_health = table.Column<int>(type: "integer", nullable: false),
                    max_hero_health = table.Column<int>(type: "integer", nullable: false),
                    starting_mana = table.Column<int>(type: "integer", nullable: false),
                    max_mana = table.Column<int>(type: "integer", nullable: false),
                    mana_granted_per_turn = table.Column<int>(type: "integer", nullable: false),
                    mana_grant_timing = table.Column<int>(type: "integer", nullable: false),
                    initial_draw_count = table.Column<int>(type: "integer", nullable: false),
                    cards_drawn_on_turn_start = table.Column<int>(type: "integer", nullable: false),
                    starting_seat_index = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_rulesets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_ruleset_seat_overrides",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    game_ruleset_id = table.Column<string>(type: "text", nullable: false),
                    seat_index = table.Column<int>(type: "integer", nullable: false),
                    additional_hero_health = table.Column<int>(type: "integer", nullable: false),
                    additional_max_hero_health = table.Column<int>(type: "integer", nullable: false),
                    additional_starting_mana = table.Column<int>(type: "integer", nullable: false),
                    additional_max_mana = table.Column<int>(type: "integer", nullable: false),
                    additional_mana_per_turn = table.Column<int>(type: "integer", nullable: false),
                    additional_cards_drawn_on_turn_start = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_ruleset_seat_overrides", x => x.id);
                    table.ForeignKey(
                        name: "FK_game_ruleset_seat_overrides_game_rulesets_game_ruleset_id",
                        column: x => x.game_ruleset_id,
                        principalTable: "game_rulesets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_ruleset_seat_overrides_game_ruleset_id_seat_index",
                table: "game_ruleset_seat_overrides",
                columns: new[] { "game_ruleset_id", "seat_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_rulesets_is_default",
                table: "game_rulesets",
                column: "is_default");

            migrationBuilder.CreateIndex(
                name: "IX_game_rulesets_ruleset_key",
                table: "game_rulesets",
                column: "ruleset_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matches_game_ruleset_id",
                table: "matches",
                column: "game_ruleset_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_ruleset_seat_overrides");

            migrationBuilder.DropTable(
                name: "game_rulesets");

            migrationBuilder.DropIndex(
                name: "IX_matches_game_ruleset_id",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "game_ruleset_id",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "game_ruleset_name",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "game_rules_snapshot_json",
                table: "matches");
        }
    }
}
