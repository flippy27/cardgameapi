using CardDuel.ServerApi.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260422033000_AddCardPresentationMetadata")]
    public partial class AddCardPresentationMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "battle_presentation_json",
                table: "cards",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "visual_profiles_json",
                table: "cards",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "battle_presentation_json",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "visual_profiles_json",
                table: "cards");
        }
    }
}
