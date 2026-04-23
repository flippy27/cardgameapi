using System;
using CardDuel.ServerApi.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260421041000_AddMatchmakingModeRulesetAssignments")]
    public partial class AddMatchmakingModeRulesetAssignments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "matchmaking_mode_ruleset_assignments",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    ruleset_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matchmaking_mode_ruleset_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_matchmaking_mode_ruleset_assignments_game_rulesets_ruleset_id",
                        column: x => x.ruleset_id,
                        principalTable: "game_rulesets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_matchmaking_mode_ruleset_assignments_mode",
                table: "matchmaking_mode_ruleset_assignments",
                column: "mode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matchmaking_mode_ruleset_assignments_ruleset_id",
                table: "matchmaking_mode_ruleset_assignments",
                column: "ruleset_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "matchmaking_mode_ruleset_assignments");
        }
    }
}
