BEGIN;

-- Assign a variety of battle presentation and visual profile examples to random existing cards.
-- PostgreSQL / jsonb version.
-- Safe to run on a database that already has cards and the new jsonb columns.
--
-- What this demonstrates:
-- - multiple surfaces: hand, played, inspect, reward, decklist
-- - multiple profile variants per card
-- - layered composition: frame, art, bg, badge, foil, glow, rules, nameplate, portrait-mask
-- - battle presentation hints: motion, shake, delivery, fx/audio ids
-- - metadataJson examples for future extensibility
--
-- Notes:
-- - This intentionally updates a random sample of existing cards each time you run it.
-- - The client can ignore surfaces or layers it does not support yet.

WITH picked_cards AS (
    SELECT
        id,
        card_id,
        display_name,
        row_number() OVER (ORDER BY random()) AS example_index
    FROM cards
    ORDER BY random()
    LIMIT 12
)
UPDATE cards AS c
SET
    battle_presentation_json = CASE picked_cards.example_index
        WHEN 1 THEN jsonb_build_object(
            'AttackMotionLevel', 2,
            'AttackShakeLevel', 1,
            'AttackDeliveryType', 'projectile',
            'ImpactFxId', 'impact_arrow_light',
            'AttackAudioCueId', 'sfx_arrow_light',
            'MetadataJson', '{"theme":"starter-ranged","notes":"simple readable baseline"}'
        )
        WHEN 2 THEN jsonb_build_object(
            'AttackMotionLevel', 4,
            'AttackShakeLevel', 5,
            'AttackDeliveryType', 'melee',
            'ImpactFxId', 'impact_hammer_heavy',
            'AttackAudioCueId', 'sfx_hammer_heavy',
            'MetadataJson', '{"theme":"heavy-bruiser","cameraProfile":"boss-close"}'
        )
        WHEN 3 THEN jsonb_build_object(
            'AttackMotionLevel', 5,
            'AttackShakeLevel', 2,
            'AttackDeliveryType', 'beam',
            'ImpactFxId', 'impact_arcane_beam',
            'AttackAudioCueId', 'sfx_arcane_beam',
            'MetadataJson', '{"trail":"long","color":"#66D9FFFF"}'
        )
        WHEN 4 THEN jsonb_build_object(
            'AttackMotionLevel', 3,
            'AttackShakeLevel', 4,
            'AttackDeliveryType', 'arc',
            'ImpactFxId', 'impact_fire_arc',
            'AttackAudioCueId', 'sfx_fire_arc',
            'MetadataJson', '{"projectileCurve":"lob_high","color":"#FF8A33FF"}'
        )
        WHEN 5 THEN jsonb_build_object(
            'AttackMotionLevel', 1,
            'AttackShakeLevel', 1,
            'AttackDeliveryType', 'projectile',
            'ImpactFxId', 'impact_dagger_fast',
            'AttackAudioCueId', 'sfx_dagger_fast',
            'MetadataJson', '{"theme":"assassin","speed":"fast"}'
        )
        WHEN 6 THEN jsonb_build_object(
            'AttackMotionLevel', 0,
            'AttackShakeLevel', 0,
            'AttackDeliveryType', 'melee',
            'ImpactFxId', 'impact_default',
            'AttackAudioCueId', 'sfx_default_slash',
            'MetadataJson', '{"theme":"client-fallback-demo"}'
        )
        WHEN 7 THEN jsonb_build_object(
            'AttackMotionLevel', 4,
            'AttackShakeLevel', 3,
            'AttackDeliveryType', 'projectile',
            'ImpactFxId', 'impact_crystal_shard',
            'AttackAudioCueId', 'sfx_crystal_shard',
            'MetadataJson', '{"material":"glass","trail":"shards"}'
        )
        WHEN 8 THEN jsonb_build_object(
            'AttackMotionLevel', 2,
            'AttackShakeLevel', 4,
            'AttackDeliveryType', 'melee',
            'ImpactFxId', 'impact_taunt_slam',
            'AttackAudioCueId', 'sfx_taunt_slam',
            'MetadataJson', '{"role":"tank","screenShakeHint":"broad"}'
        )
        WHEN 9 THEN jsonb_build_object(
            'AttackMotionLevel', 5,
            'AttackShakeLevel', 5,
            'AttackDeliveryType', 'beam',
            'ImpactFxId', 'impact_divine_blast',
            'AttackAudioCueId', 'sfx_divine_blast',
            'MetadataJson', '{"rarityFx":"legendary","bloom":"high"}'
        )
        WHEN 10 THEN jsonb_build_object(
            'AttackMotionLevel', 3,
            'AttackShakeLevel', 2,
            'AttackDeliveryType', 'arc',
            'ImpactFxId', 'impact_nature_vine',
            'AttackAudioCueId', 'sfx_nature_vine',
            'MetadataJson', '{"secondaryColor":"#7BC96FFF"}'
        )
        WHEN 11 THEN jsonb_build_object(
            'AttackMotionLevel', 1,
            'AttackShakeLevel', 3,
            'AttackDeliveryType', 'projectile',
            'ImpactFxId', 'impact_token_pop',
            'AttackAudioCueId', 'sfx_token_pop',
            'MetadataJson', '{"theme":"token-unit","shortLived":true}'
        )
        ELSE jsonb_build_object(
            'AttackMotionLevel', 4,
            'AttackShakeLevel', 4,
            'AttackDeliveryType', 'projectile',
            'ImpactFxId', 'impact_premium_shot',
            'AttackAudioCueId', 'sfx_premium_shot',
            'MetadataJson', '{"theme":"premium-showcase","foilReactive":true}'
        )
    END,
    visual_profiles_json = CASE picked_cards.example_index
        WHEN 1 THEN jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'hand-default',
                'DisplayName', 'Hand Default',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'bg', 'SourceKind', 'sprite', 'AssetRef', 'card/bg/common_blue', 'SortOrder', 0, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/common_metal', 'SortOrder', 10, 'MetadataJson', '{"rarity":"common"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id), 'SortOrder', 20, 'MetadataJson', '{"crop":"portrait"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'nameplate', 'SourceKind', 'sprite', 'AssetRef', 'card/ui/nameplate_default', 'SortOrder', 30, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'rules', 'SourceKind', 'sprite', 'AssetRef', 'card/ui/rules_text_default', 'SortOrder', 40, 'MetadataJson', '{"font":"serif-small"}')
                )
            ),
            jsonb_build_object(
                'ProfileKey', 'played-default',
                'DisplayName', 'Played Default',
                'IsDefault', false,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'played', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'board/frame/common_metal', 'SortOrder', 0, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'played', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id), 'SortOrder', 10, 'MetadataJson', '{"crop":"tight"}')
                )
            )
        )
        WHEN 2 THEN jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'hand-bruiser',
                'DisplayName', 'Hand Bruiser',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'bg', 'SourceKind', 'sprite', 'AssetRef', 'card/bg/ember_smoke', 'SortOrder', 0, 'MetadataJson', '{"faction":"ember"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/rare_ember', 'SortOrder', 10, 'MetadataJson', '{"rarity":"rare","bevel":"heavy"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'portrait-mask', 'SourceKind', 'sprite', 'AssetRef', 'card/mask/vertical_slit', 'SortOrder', 15, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_bruiser'), 'SortOrder', 20, 'MetadataJson', '{"crop":"aggressive"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'badge', 'SourceKind', 'sprite', 'AssetRef', 'card/badge/frontline', 'SortOrder', 30, 'MetadataJson', '{"placement":"top-left"}')
                )
            ),
            jsonb_build_object(
                'ProfileKey', 'played-bruiser',
                'DisplayName', 'Played Bruiser',
                'IsDefault', false,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'played', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'board/frame/rare_ember', 'SortOrder', 0, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'played', 'Layer', 'glow', 'SourceKind', 'sprite', 'AssetRef', 'board/fx/glow_red_soft', 'SortOrder', 5, 'MetadataJson', '{"blend":"add"}'),
                    jsonb_build_object('Surface', 'played', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_bruiser'), 'SortOrder', 10, 'MetadataJson', '{"crop":"board-close"}')
                )
            )
        )
        WHEN 3 THEN jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'hand-full-art',
                'DisplayName', 'Hand Full Art',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_full'), 'SortOrder', 0, 'MetadataJson', '{"fullArt":true,"safeTextTop":0.14,"safeTextBottom":0.22}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/legendary_fullart', 'SortOrder', 20, 'MetadataJson', '{"rarity":"legendary"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'foil', 'SourceKind', 'sprite', 'AssetRef', 'card/fx/foil_rainbow_soft', 'SortOrder', 30, 'MetadataJson', '{"scrollSpeed":0.15}')
                )
            ),
            jsonb_build_object(
                'ProfileKey', 'inspect-gallery',
                'DisplayName', 'Inspect Gallery',
                'IsDefault', false,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'inspect', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_gallery'), 'SortOrder', 0, 'MetadataJson', '{"zoom":"free"}'),
                    jsonb_build_object('Surface', 'inspect', 'Layer', 'title-ornament', 'SourceKind', 'sprite', 'AssetRef', 'card/ui/inspect_ornament_legendary', 'SortOrder', 10, 'MetadataJson', NULL)
                )
            )
        )
        WHEN 4 THEN jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'spell-hand',
                'DisplayName', 'Spell Hand',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'bg', 'SourceKind', 'sprite', 'AssetRef', 'card/bg/spell_arcane', 'SortOrder', 0, 'MetadataJson', '{"type":"spell"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/spell_epic', 'SortOrder', 10, 'MetadataJson', '{"rarity":"epic"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_spell'), 'SortOrder', 20, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'sigil', 'SourceKind', 'sprite', 'AssetRef', 'card/icon/spell_sigil_arcane', 'SortOrder', 30, 'MetadataJson', NULL)
                )
            ),
            jsonb_build_object(
                'ProfileKey', 'reward-card',
                'DisplayName', 'Reward Card',
                'IsDefault', false,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'reward', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_reward'), 'SortOrder', 0, 'MetadataJson', '{"fullBleed":true}'),
                    jsonb_build_object('Surface', 'reward', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/reward_gold', 'SortOrder', 10, 'MetadataJson', '{"shine":"slow"}'),
                    jsonb_build_object('Surface', 'reward', 'Layer', 'burst', 'SourceKind', 'sprite', 'AssetRef', 'card/fx/reward_burst_orange', 'SortOrder', 20, 'MetadataJson', NULL)
                )
            )
        )
        WHEN 5 THEN jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'assassin-hand',
                'DisplayName', 'Assassin Hand',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'bg', 'SourceKind', 'sprite', 'AssetRef', 'card/bg/void_smoke', 'SortOrder', 0, 'MetadataJson', '{"faction":"void"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/rare_void', 'SortOrder', 10, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_assassin'), 'SortOrder', 20, 'MetadataJson', '{"focus":"weapon"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'overlay', 'SourceKind', 'sprite', 'AssetRef', 'card/fx/scratch_overlay_subtle', 'SortOrder', 30, 'MetadataJson', '{"opacity":0.35}')
                )
            ),
            jsonb_build_object(
                'ProfileKey', 'decklist-minimal',
                'DisplayName', 'Decklist Minimal',
                'IsDefault', false,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'decklist', 'Layer', 'thumbnail', 'SourceKind', 'image', 'AssetRef', concat('card/thumb/', picked_cards.card_id), 'SortOrder', 0, 'MetadataJson', '{"shape":"square"}'),
                    jsonb_build_object('Surface', 'decklist', 'Layer', 'rarity-pip', 'SourceKind', 'sprite', 'AssetRef', 'card/icon/rarity_rare', 'SortOrder', 10, 'MetadataJson', NULL)
                )
            )
        )
        WHEN 6 THEN jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'fallback-clean',
                'DisplayName', 'Fallback Clean',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/plain_neutral', 'SortOrder', 10, 'MetadataJson', '{"fallback":true}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id), 'SortOrder', 20, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'played', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'board/frame/plain_neutral', 'SortOrder', 0, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'played', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id), 'SortOrder', 10, 'MetadataJson', NULL)
                )
            )
        )
        WHEN 7 THEN jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'crystal-premium',
                'DisplayName', 'Crystal Premium',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'bg', 'SourceKind', 'sprite', 'AssetRef', 'card/bg/crystal_tidal', 'SortOrder', 0, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/epic_tidal', 'SortOrder', 10, 'MetadataJson', '{"rarity":"epic","facet":"octagon"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_crystal'), 'SortOrder', 20, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'foil', 'SourceKind', 'sprite', 'AssetRef', 'card/fx/foil_crystal', 'SortOrder', 30, 'MetadataJson', '{"refraction":0.8}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'badge', 'SourceKind', 'sprite', 'AssetRef', 'card/badge/premium', 'SortOrder', 40, 'MetadataJson', NULL)
                )
            ),
            jsonb_build_object(
                'ProfileKey', 'played-premium',
                'DisplayName', 'Played Premium',
                'IsDefault', false,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'played', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'board/frame/epic_tidal', 'SortOrder', 0, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'played', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_crystal'), 'SortOrder', 10, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'played', 'Layer', 'foil', 'SourceKind', 'sprite', 'AssetRef', 'board/fx/foil_crystal', 'SortOrder', 20, 'MetadataJson', '{"reactive":"hp-loss"}')
                )
            )
        )
        WHEN 8 THEN jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'tank-hand',
                'DisplayName', 'Tank Hand',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/alloy_fortified', 'SortOrder', 10, 'MetadataJson', '{"faction":"alloy"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_tank'), 'SortOrder', 20, 'MetadataJson', '{"crop":"defensive"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'badge', 'SourceKind', 'sprite', 'AssetRef', 'card/badge/taunt', 'SortOrder', 30, 'MetadataJson', NULL)
                )
            ),
            jsonb_build_object(
                'ProfileKey', 'inspect-lore',
                'DisplayName', 'Inspect Lore',
                'IsDefault', false,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'inspect', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_lore'), 'SortOrder', 0, 'MetadataJson', '{"pan":"slow"}'),
                    jsonb_build_object('Surface', 'inspect', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/ui/lore_frame_alloy', 'SortOrder', 10, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'inspect', 'Layer', 'caption-bar', 'SourceKind', 'sprite', 'AssetRef', 'card/ui/lore_caption_bar', 'SortOrder', 20, 'MetadataJson', '{"supportsRichText":true}')
                )
            )
        )
        WHEN 9 THEN jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'legendary-hand',
                'DisplayName', 'Legendary Hand',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_legendary'), 'SortOrder', 0, 'MetadataJson', '{"fullArt":true,"parallaxDepth":0.25}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/legendary_divine', 'SortOrder', 20, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'halo', 'SourceKind', 'sprite', 'AssetRef', 'card/fx/halo_divine', 'SortOrder', 30, 'MetadataJson', '{"blend":"screen"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'foil', 'SourceKind', 'sprite', 'AssetRef', 'card/fx/foil_legendary_prism', 'SortOrder', 40, 'MetadataJson', '{"rainbow":true}')
                )
            ),
            jsonb_build_object(
                'ProfileKey', 'reward-legendary',
                'DisplayName', 'Reward Legendary',
                'IsDefault', false,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'reward', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_legendary'), 'SortOrder', 0, 'MetadataJson', '{"fullBleed":true}'),
                    jsonb_build_object('Surface', 'reward', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/reward_legendary', 'SortOrder', 10, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'reward', 'Layer', 'burst', 'SourceKind', 'sprite', 'AssetRef', 'card/fx/reward_burst_legendary', 'SortOrder', 20, 'MetadataJson', NULL)
                )
            )
        )
        WHEN 10 THEN jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'nature-hand',
                'DisplayName', 'Nature Hand',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'bg', 'SourceKind', 'sprite', 'AssetRef', 'card/bg/grove_foliage', 'SortOrder', 0, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/grove_rare', 'SortOrder', 10, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_nature'), 'SortOrder', 20, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'vine-overlay', 'SourceKind', 'sprite', 'AssetRef', 'card/fx/vines_soft', 'SortOrder', 30, 'MetadataJson', '{"windReactive":true}')
                )
            ),
            jsonb_build_object(
                'ProfileKey', 'played-nature',
                'DisplayName', 'Played Nature',
                'IsDefault', false,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'played', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_nature'), 'SortOrder', 0, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'played', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'board/frame/grove_rare', 'SortOrder', 10, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'played', 'Layer', 'slot-aura', 'SourceKind', 'sprite', 'AssetRef', 'board/fx/grove_slot_aura', 'SortOrder', 20, 'MetadataJson', '{"pulseOnAttack":true}')
                )
            )
        )
        WHEN 11 THEN jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'token-hand',
                'DisplayName', 'Token Hand',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/token_plain', 'SortOrder', 10, 'MetadataJson', '{"token":true}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_token'), 'SortOrder', 20, 'MetadataJson', '{"desaturated":false}')
                )
            ),
            jsonb_build_object(
                'ProfileKey', 'played-token',
                'DisplayName', 'Played Token',
                'IsDefault', false,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'played', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'board/frame/token_plain', 'SortOrder', 0, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'played', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_token'), 'SortOrder', 10, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'played', 'Layer', 'badge', 'SourceKind', 'sprite', 'AssetRef', 'card/badge/summoned', 'SortOrder', 20, 'MetadataJson', NULL)
                )
            )
        )
        ELSE jsonb_build_array(
            jsonb_build_object(
                'ProfileKey', 'premium-hand',
                'DisplayName', 'Premium Hand',
                'IsDefault', true,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'hand', 'Layer', 'bg', 'SourceKind', 'sprite', 'AssetRef', 'card/bg/premium_dark', 'SortOrder', 0, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_premium'), 'SortOrder', 10, 'MetadataJson', '{"fullArt":true,"premium":true}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/frame/premium_reactive', 'SortOrder', 20, 'MetadataJson', '{"reactive":"mousemove"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'foil', 'SourceKind', 'sprite', 'AssetRef', 'card/fx/foil_reactive_gold', 'SortOrder', 30, 'MetadataJson', '{"scrollSpeed":0.4,"mask":"diagonal"}'),
                    jsonb_build_object('Surface', 'hand', 'Layer', 'signature', 'SourceKind', 'sprite', 'AssetRef', 'card/ui/artist_signature_gold', 'SortOrder', 40, 'MetadataJson', '{"position":"bottom-right"}')
                )
            ),
            jsonb_build_object(
                'ProfileKey', 'inspect-premium-showcase',
                'DisplayName', 'Inspect Premium Showcase',
                'IsDefault', false,
                'Layers', jsonb_build_array(
                    jsonb_build_object('Surface', 'inspect', 'Layer', 'art', 'SourceKind', 'image', 'AssetRef', concat('card/art/', picked_cards.card_id, '_premium_showcase'), 'SortOrder', 0, 'MetadataJson', '{"zoom":"cinematic"}'),
                    jsonb_build_object('Surface', 'inspect', 'Layer', 'frame', 'SourceKind', 'sprite', 'AssetRef', 'card/ui/inspect_frame_premium', 'SortOrder', 10, 'MetadataJson', NULL),
                    jsonb_build_object('Surface', 'inspect', 'Layer', 'foil', 'SourceKind', 'sprite', 'AssetRef', 'card/fx/foil_reactive_gold', 'SortOrder', 20, 'MetadataJson', '{"reactive":"gyro"}')
                )
            )
        )
    END,
    updated_at = now()
FROM picked_cards
WHERE c.id = picked_cards.id;

-- Optional inspection helper:
-- SELECT card_id, display_name, battle_presentation_json, visual_profiles_json
-- FROM cards
-- WHERE updated_at > now() - interval '5 minutes'
-- ORDER BY updated_at DESC;

COMMIT;
