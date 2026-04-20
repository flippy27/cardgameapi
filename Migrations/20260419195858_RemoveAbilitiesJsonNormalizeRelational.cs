using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAbilitiesJsonNormalizeRelational : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbilitiesJson",
                table: "Cards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbilitiesJson",
                table: "Cards",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }
    }
}
