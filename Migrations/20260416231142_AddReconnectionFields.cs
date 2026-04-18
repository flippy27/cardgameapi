using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    /// <inheritdoc />
    public partial class AddReconnectionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Player1DisconnectedAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Player1ReconnectToken",
                table: "Matches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Player2DisconnectedAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Player2ReconnectToken",
                table: "Matches",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReplayLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MatchId = table.Column<string>(type: "text", nullable: false),
                    ActionNumber = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<string>(type: "text", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    ActionData = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplayLogs");

            migrationBuilder.DropColumn(
                name: "Player1DisconnectedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Player1ReconnectToken",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Player2DisconnectedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Player2ReconnectToken",
                table: "Matches");
        }
    }
}
