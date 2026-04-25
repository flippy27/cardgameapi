-- Clear existing data (respect foreign keys)
DELETE FROM card_abilities;
DELETE FROM effects;
DELETE FROM abilities;
DELETE FROM deck_cards;
DELETE FROM decks;
DELETE FROM cards;
DELETE FROM users;

-- Insert abilities from cardgame reference.
-- skill_type: 0 defensive, 1 offensive, 2 equipable, 3 utility, 4 modifier.
-- trigger_kind: 0 on play, 1 turn start, 2 turn end, 3 battle phase.
-- target_selector_kind: 0 self, 1 frontline first, 2 backline first, 3 all enemies, 4 lowest health ally, 5 ally top, 6 ally left, 7 ally right, 8 mirrored opponent.
INSERT INTO abilities (id, ability_id, display_name, description, skill_type, trigger_kind, target_selector_kind, animation_cue_id, conditions_json, metadata_json, created_at) VALUES
('ability-1', 'armor', 'Armor', 'Gains persistent armor when played', 0, 0, 0, 'skill_armor_gain', '{}'::jsonb, '{"battleOrder":"on_play"}'::jsonb, NOW()),
('ability-2', 'shield', 'Shield', 'Negates the next damage event received', 0, 0, 0, 'skill_shield_gain', '{}'::jsonb, '{"battleOrder":"on_play"}'::jsonb, NOW()),
('ability-3', 'fly', 'Fly', 'Bypasses non-flying defenders and attacks hero directly', 4, 3, 0, 'skill_fly_bypass', '{}'::jsonb, '{"normalAttackModifier":true}'::jsonb, NOW()),
('ability-4', 'trample', 'Trample', 'Ignores armor when attacking', 4, 3, 0, 'skill_trample_hit', '{}'::jsonb, '{"normalAttackModifier":true}'::jsonb, NOW()),
('ability-5', 'poison', 'Poison', 'Applies poison to damaged enemies', 1, 3, 0, 'skill_poison_apply', '{}'::jsonb, '{"normalAttackModifier":true}'::jsonb, NOW()),
('ability-6', 'stun', 'Stun', 'The next damaged enemy skips its next attack; consumed after use', 1, 3, 0, 'skill_stun_apply', '{}'::jsonb, '{"oneShotPerCard":true}'::jsonb, NOW()),
('ability-7', 'leech', 'Leech', 'Heals the attacker for health damage dealt to cards', 1, 3, 0, 'skill_leech_heal', '{}'::jsonb, '{"normalAttackModifier":true}'::jsonb, NOW()),
('ability-8', 'enrage', 'Enrage', 'Attacks twice, then skips its next attack opportunity', 1, 3, 0, 'skill_enrage_double', '{}'::jsonb, '{"normalAttackModifier":true}'::jsonb, NOW()),
('ability-9', 'regenerate_left', 'Regenerate Left', 'Heals the ally left slot at end of turn', 3, 2, 6, 'skill_regenerate_left', '{}'::jsonb, '{"regenTarget":"left"}'::jsonb, NOW()),
('ability-10', 'taunt', 'Taunt', 'Enemy frontline targeting must choose this card first', 3, 3, 0, 'skill_taunt_pulse', '{}'::jsonb, '{"targetingModifier":true}'::jsonb, NOW()),
('ability-11', 'haste', 'Haste', 'Can attack on the turn it is played', 4, 3, 0, 'skill_haste_ready', '{}'::jsonb, '{"normalAttackModifier":true}'::jsonb, NOW());

-- Insert effects for abilities.
-- effect_kind: 2 gain armor, 27 haste marker, 28 shield, 29 poison status, 30 stun status.
INSERT INTO effects (id, ability_definition_id, effect_kind, amount, secondary_amount, duration_turns, target_selector_kind_override, sequence, metadata_json, created_at) VALUES
('effect-1', 'ability-1', 2, 3, NULL, NULL, NULL, 0, '{"animationStep":"armor"}'::jsonb, NOW()),
('effect-2', 'ability-2', 28, 1, NULL, 99, NULL, 0, '{"animationStep":"shield"}'::jsonb, NOW()),
('effect-3', 'ability-5', 29, 1, NULL, 2, NULL, 0, '{"animationStep":"poison"}'::jsonb, NOW()),
('effect-4', 'ability-6', 30, 0, NULL, 1, NULL, 0, '{"animationStep":"stun"}'::jsonb, NOW()),
('effect-5', 'ability-9', 1, 2, NULL, NULL, 6, 0, '{"animationStep":"regen-left"}'::jsonb, NOW()),
('effect-6', 'ability-11', 27, 1, NULL, NULL, NULL, 0, '{"animationStep":"haste-ready"}'::jsonb, NOW());

-- Insert 2 users
INSERT INTO users (id, email, username, password_hash, is_active, created_at) VALUES
('user-1', 'playerone@flippy.com', 'PlayerOne', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', true, NOW()),
('user-2', 'playertwo@flippy.com', 'PlayerTwo', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', true, NOW());

-- Insert 200 cards with deterministic properties
INSERT INTO cards (id, card_id, display_name, description, mana_cost, attack, health, armor, card_type, card_rarity, card_faction, unit_type, allowed_row, default_attack_selector, turns_until_can_attack, is_limited, created_at)
SELECT
    'card-' || i::text,
    CASE WHEN (i % 5) = 0 THEN 'ember_' WHEN (i % 5) = 1 THEN 'tidal_' WHEN (i % 5) = 2 THEN 'grove_' WHEN (i % 5) = 3 THEN 'alloy_' ELSE 'void_' END || LPAD(i::text, 4, '0'),
    CASE WHEN (i % 5) = 0 THEN 'Ember' WHEN (i % 5) = 1 THEN 'Tidal' WHEN (i % 5) = 2 THEN 'Grove' WHEN (i % 5) = 3 THEN 'Alloy' ELSE 'Void' END || ' Card #' || i::text,
    'A card from the faction',
    (i % 7) + 1,
    (i % 6) + 1,
    (i % 7) + 2,
    i % 3,
    i % 3,
    i % 5,
    i % 5,
    CASE WHEN (i % 3) = 0 THEN ((i / 3) % 3) ELSE NULL END,
    i % 3,
    1,
    1,
    (i % 20) = 0,
    NOW()
FROM generate_series(1, 200) AS t(i);

-- Insert CardAbilities (0-3 per card, deterministic based on card ID)
INSERT INTO card_abilities (id, card_definition_id, ability_definition_id, sequence)
WITH card_ability_pairs AS (
    SELECT
        c.id as card_id,
        a.id as ability_id,
        ROW_NUMBER() OVER (PARTITION BY c.id ORDER BY a.id) - 1 as seq
    FROM cards c
    CROSS JOIN abilities a
    WHERE (CAST(SUBSTRING(c.id, 6) AS int) * 7 + CAST(SUBSTRING(a.id, 9) AS int) * 3) % 10 < 3
)
SELECT
    'ca-' || ROW_NUMBER() OVER (ORDER BY card_id, seq)::text,
    card_id,
    ability_id,
    seq
FROM card_ability_pairs;

-- Insert 10 decks: 5 for playerone, 5 for playertwo.
INSERT INTO decks (id, user_id, deck_id, display_name, created_at)
WITH card_ranking AS (
    SELECT
        card_id,
        ROW_NUMBER() OVER (ORDER BY card_id) as card_row
    FROM cards
),
deck_assignment AS (
    SELECT
        card_id,
        (card_row - 1) / 20 + 1 as deck_num,
        MOD(card_row - 1, 20) + 1 as card_in_deck
    FROM card_ranking
)
SELECT
    'deck-' || deck_num::text,
    CASE WHEN deck_num <= 5 THEN 'user-1' ELSE 'user-2' END,
    'deck_' || CASE WHEN deck_num <= 5 THEN 'playerone' ELSE 'playertwo' END || '_' || (MOD(deck_num - 1, 5) + 1)::text,
    CASE WHEN deck_num <= 5 THEN 'PlayerOne' ELSE 'PlayerTwo' END || ' Deck ' || (MOD(deck_num - 1, 5) + 1)::text,
    NOW()
FROM deck_assignment
GROUP BY deck_num;

-- Insert deck card entries: one row per card copy, preserving deck order.
INSERT INTO deck_cards (id, deck_id, card_definition_id, position, created_at)
WITH card_ranking AS (
    SELECT
        id AS card_definition_id,
        card_id,
        ROW_NUMBER() OVER (ORDER BY card_id) as card_row
    FROM cards
),
deck_assignment AS (
    SELECT
        card_definition_id,
        (card_row - 1) / 20 + 1 as deck_num,
        MOD(card_row - 1, 20) as position
    FROM card_ranking
)
SELECT
    'deck-card-' || deck_num::text || '-' || position::text,
    'deck-' || deck_num::text,
    card_definition_id,
    position,
    NOW()
FROM deck_assignment
WHERE position < 20;

-- Verify counts
SELECT 'cards' as name, COUNT(*) as count FROM cards
UNION ALL SELECT 'abilities', COUNT(*) FROM abilities
UNION ALL SELECT 'effects', COUNT(*) FROM effects
UNION ALL SELECT 'card_abilities', COUNT(*) FROM card_abilities
UNION ALL SELECT 'decks', COUNT(*) FROM decks
UNION ALL SELECT 'deck_cards', COUNT(*) FROM deck_cards
UNION ALL SELECT 'users', COUNT(*) FROM users;
