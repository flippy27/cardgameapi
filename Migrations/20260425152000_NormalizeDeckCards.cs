using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    public partial class NormalizeDeckCards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deck_cards",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    deck_id = table.Column<string>(type: "text", nullable: false),
                    card_definition_id = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deck_cards", x => x.id);
                    table.ForeignKey(
                        name: "fk_deck_cards_cards_card_definition_id",
                        column: x => x.card_definition_id,
                        principalTable: "cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deck_cards_decks_deck_id",
                        column: x => x.deck_id,
                        principalTable: "decks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO deck_cards (id, deck_id, card_definition_id, position, created_at)
                SELECT
                    d.id || '-card-' || (entry.ordinality - 1)::text,
                    d.id,
                    c.id,
                    (entry.ordinality - 1)::integer,
                    COALESCE(d.created_at, NOW())
                FROM decks AS d
                CROSS JOIN LATERAL unnest(string_to_array(COALESCE(d.card_ids, ''), ',')) WITH ORDINALITY AS entry(card_id, ordinality)
                INNER JOIN cards AS c ON c.card_id = entry.card_id
                WHERE entry.card_id <> '';
                """);

            migrationBuilder.DropColumn(
                name: "card_ids",
                table: "decks");

            migrationBuilder.CreateIndex(
                name: "ix_deck_cards_card_definition_id",
                table: "deck_cards",
                column: "card_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_deck_cards_deck_id_position",
                table: "deck_cards",
                columns: new[] { "deck_id", "position" });

            migrationBuilder.Sql("""
                UPDATE cards
                SET turns_until_can_attack = 1
                WHERE card_type = 0 AND turns_until_can_attack < 1;

                UPDATE cards
                SET default_attack_selector = 1
                WHERE card_type = 0;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "card_ids",
                table: "decks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE decks AS d
                SET card_ids = COALESCE(cards.card_ids, '')
                FROM (
                    SELECT
                        dc.deck_id,
                        string_agg(c.card_id, ',' ORDER BY dc.position) AS card_ids
                    FROM deck_cards AS dc
                    INNER JOIN cards AS c ON c.id = dc.card_definition_id
                    GROUP BY dc.deck_id
                ) AS cards
                WHERE cards.deck_id = d.id;
                """);

            migrationBuilder.DropTable(
                name: "deck_cards");
        }
    }
}
