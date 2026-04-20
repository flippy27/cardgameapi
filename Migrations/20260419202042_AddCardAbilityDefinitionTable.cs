using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCardAbilityDefinitionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Abilities_Cards_CardDefinitionId",
                table: "Abilities");

            migrationBuilder.DropIndex(
                name: "IX_Abilities_CardDefinitionId_AbilityId",
                table: "Abilities");

            migrationBuilder.DropColumn(
                name: "CardDefinitionId",
                table: "Abilities");

            migrationBuilder.CreateTable(
                name: "CardAbilities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CardDefinitionId = table.Column<string>(type: "text", nullable: false),
                    AbilityDefinitionId = table.Column<string>(type: "text", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardAbilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardAbilities_Abilities_AbilityDefinitionId",
                        column: x => x.AbilityDefinitionId,
                        principalTable: "Abilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardAbilities_Cards_CardDefinitionId",
                        column: x => x.CardDefinitionId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Abilities_AbilityId",
                table: "Abilities",
                column: "AbilityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardAbilities_AbilityDefinitionId",
                table: "CardAbilities",
                column: "AbilityDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CardAbilities_CardDefinitionId_Sequence",
                table: "CardAbilities",
                columns: new[] { "CardDefinitionId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardAbilities");

            migrationBuilder.DropIndex(
                name: "IX_Abilities_AbilityId",
                table: "Abilities");

            migrationBuilder.AddColumn<string>(
                name: "CardDefinitionId",
                table: "Abilities",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Abilities_CardDefinitionId_AbilityId",
                table: "Abilities",
                columns: new[] { "CardDefinitionId", "AbilityId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Abilities_Cards_CardDefinitionId",
                table: "Abilities",
                column: "CardDefinitionId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
