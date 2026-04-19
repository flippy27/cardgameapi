using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAbilitiesAndEffectsModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CardFaction",
                table: "Cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CardRarity",
                table: "Cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CardType",
                table: "Cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Cards",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsLimited",
                table: "Cards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TurnsUntilCanAttack",
                table: "Cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnitType",
                table: "Cards",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Abilities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AbilityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TriggerKind = table.Column<int>(type: "integer", nullable: false),
                    TargetSelectorKind = table.Column<int>(type: "integer", nullable: false),
                    CardDefinitionId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Abilities_Cards_CardDefinitionId",
                        column: x => x.CardDefinitionId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Effects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    EffectKind = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    AbilityDefinitionId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Effects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Effects_Abilities_AbilityDefinitionId",
                        column: x => x.AbilityDefinitionId,
                        principalTable: "Abilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Abilities_CardDefinitionId_AbilityId",
                table: "Abilities",
                columns: new[] { "CardDefinitionId", "AbilityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Effects_AbilityDefinitionId_Sequence",
                table: "Effects",
                columns: new[] { "AbilityDefinitionId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Effects");

            migrationBuilder.DropTable(
                name: "Abilities");

            migrationBuilder.DropColumn(
                name: "CardFaction",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CardRarity",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CardType",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "IsLimited",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "TurnsUntilCanAttack",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "UnitType",
                table: "Cards");
        }
    }
}
