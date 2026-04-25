using System;
using CardDuel.ServerApi.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260425152000_NormalizeDeckCards")]
    public partial class NormalizeDeckCards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent rewrite: use IF NOT EXISTS / DO $$ guards throughout.
            // Original auto-generated CreateTable/DropColumn would fail on a DB
            // whose schema partially diverged from the EF snapshot.

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS deck_cards (
                    id text NOT NULL,
                    deck_id text NOT NULL,
                    card_definition_id text NOT NULL,
                    position integer NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    CONSTRAINT ""pk_deck_cards"" PRIMARY KEY (id),
                    CONSTRAINT ""fk_deck_cards_cards_card_definition_id""
                        FOREIGN KEY (card_definition_id) REFERENCES cards (id) ON DELETE RESTRICT,
                    CONSTRAINT ""fk_deck_cards_decks_deck_id""
                        FOREIGN KEY (deck_id) REFERENCES decks (id) ON DELETE CASCADE
                );
            ");

            // Migrate data from decks.card_ids → deck_cards (only if card_ids column exists).
            // ON CONFLICT DO NOTHING = safe if rows already inserted.
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name   = 'decks'
                          AND column_name  = 'card_ids'
                    ) THEN
                        INSERT INTO deck_cards (id, deck_id, card_definition_id, position, created_at)
                        SELECT
                            d.id || '-card-' || (entry.ordinality - 1)::text,
                            d.id,
                            c.id,
                            (entry.ordinality - 1)::integer,
                            COALESCE(d.created_at, NOW())
                        FROM decks AS d
                        CROSS JOIN LATERAL unnest(string_to_array(COALESCE(d.card_ids, ''), ','))
                            WITH ORDINALITY AS entry(card_id, ordinality)
                        INNER JOIN cards AS c ON c.card_id = entry.card_id
                        WHERE entry.card_id <> ''
                        ON CONFLICT (id) DO NOTHING;
                    END IF;
                END $$;
            ");

            // Drop card_ids only if it still exists.
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name   = 'decks'
                          AND column_name  = 'card_ids'
                    ) THEN
                        ALTER TABLE decks DROP COLUMN card_ids;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""ix_deck_cards_card_definition_id""
                    ON deck_cards (card_definition_id);
                CREATE INDEX IF NOT EXISTS ""ix_deck_cards_deck_id_position""
                    ON deck_cards (deck_id, position);
            ");

            migrationBuilder.Sql(@"
                UPDATE cards
                SET turns_until_can_attack = 1
                WHERE card_type = 0 AND turns_until_can_attack < 1;

                UPDATE cards
                SET default_attack_selector = 1
                WHERE card_type = 0;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore card_ids column (add if missing, leave if exists).
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name   = 'decks'
                          AND column_name  = 'card_ids'
                    ) THEN
                        ALTER TABLE decks ADD COLUMN card_ids text NOT NULL DEFAULT '';
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
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
            ");

            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS deck_cards;
            ");
        }
    }
}
