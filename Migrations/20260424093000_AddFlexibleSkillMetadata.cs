using CardDuel.ServerApi.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260424093000_AddFlexibleSkillMetadata")]
    public partial class AddFlexibleSkillMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "skill_type",
                table: "abilities",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "animation_cue_id",
                table: "abilities",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "conditions_json",
                table: "abilities",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "metadata_json",
                table: "abilities",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<int>(
                name: "secondary_amount",
                table: "effects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "duration_turns",
                table: "effects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "target_selector_kind_override",
                table: "effects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_json",
                table: "effects",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "skill_type", table: "abilities");
            migrationBuilder.DropColumn(name: "animation_cue_id", table: "abilities");
            migrationBuilder.DropColumn(name: "conditions_json", table: "abilities");
            migrationBuilder.DropColumn(name: "metadata_json", table: "abilities");
            migrationBuilder.DropColumn(name: "secondary_amount", table: "effects");
            migrationBuilder.DropColumn(name: "duration_turns", table: "effects");
            migrationBuilder.DropColumn(name: "target_selector_kind_override", table: "effects");
            migrationBuilder.DropColumn(name: "metadata_json", table: "effects");
        }
    }
}
