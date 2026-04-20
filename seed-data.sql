-- Clear existing data (respect foreign keys)
DELETE FROM card_abilities;
DELETE FROM effects;
DELETE FROM abilities;
DELETE FROM decks;
DELETE FROM cards;
DELETE FROM users;

-- Insert 10 abilities from cardgame reference
INSERT INTO abilities (id, ability_id, display_name, description, trigger_kind, target_selector_kind, created_at) VALUES
('ability-1', 'armor', 'Armor', 'Absorbs incoming damage before health is reduced', 0, 4, NOW()),
('ability-2', 'shield', 'Shield', 'Absorbs one full attack (divine shield)', 0, 4, NOW()),
('ability-3', 'fly', 'Fly', 'Only flying units can attack this card', 0, 4, NOW()),
('ability-4', 'trample', 'Trample', 'Ignores armor when attacking', 2, 3, NOW()),
('ability-5', 'poison', 'Poison', 'Applies poison stacks: X damage per turn', 2, 1, NOW()),
('ability-6', 'stun', 'Stun', 'Target skips next attack', 0, 1, NOW()),
('ability-7', 'leech', 'Leech', 'Heals hero equal to damage dealt', 2, 4, NOW()),
('ability-8', 'enrage', 'Enrage', 'Gains +1 ATK when damaged', 1, 4, NOW()),
('ability-9', 'regenerate', 'Regenerate', 'Heals X HP at end of turn', 3, 4, NOW()),
('ability-10', 'taunt', 'Taunt', 'All enemy attacks must target this card if alive', 0, 4, NOW());

-- Insert effects for abilities
INSERT INTO effects (id, ability_definition_id, effect_kind, amount, sequence, created_at) VALUES
('effect-1', 'ability-1', 3, 5, 0, NOW()),
('effect-2', 'ability-2', 3, 10, 0, NOW()),
('effect-3', 'ability-3', 1, 1, 0, NOW()),
('effect-4', 'ability-4', 0, 2, 0, NOW()),
('effect-5', 'ability-5', 0, 1, 0, NOW()),
('effect-6', 'ability-6', 0, 3, 0, NOW()),
('effect-7', 'ability-7', 2, 2, 0, NOW()),
('effect-8', 'ability-8', 1, 2, 0, NOW()),
('effect-9', 'ability-9', 2, 1, 0, NOW()),
('effect-10', 'ability-10', 1, 1, 0, NOW());

-- Insert 2 users
INSERT INTO users (id, email, username, password_hash, is_active, created_at) VALUES
('user-1', 'playerone@flippy.com', 'PlayerOne', 'F6Qy4SHIl43C0v7BvDiaMF8PvQqLGHV6dFyYU9GxlXE=', true, NOW()),
('user-2', 'playertwo@flippy.com', 'PlayerTwo', 'F6Qy4SHIl43C0v7BvDiaMF8PvQqLGHV6dFyYU9GxlXE=', true, NOW());

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
    CASE WHEN (i % 3) = 0 THEN 0 ELSE NULL END,
    i % 3,
    i % 5,
    i % 2,
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

-- Insert 10 decks: 5 for playerone, 5 for playertwo, 20 cards each
INSERT INTO decks (id, user_id, deck_id, display_name, card_ids, created_at)
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
    STRING_AGG(card_id, ',') FILTER (WHERE card_in_deck <= 20),
    NOW()
FROM deck_assignment
GROUP BY deck_num;

-- Verify counts
SELECT 'cards' as name, COUNT(*) as count FROM cards
UNION ALL SELECT 'abilities', COUNT(*) FROM abilities
UNION ALL SELECT 'effects', COUNT(*) FROM effects
UNION ALL SELECT 'card_abilities', COUNT(*) FROM card_abilities
UNION ALL SELECT 'decks', COUNT(*) FROM decks
UNION ALL SELECT 'users', COUNT(*) FROM users;
