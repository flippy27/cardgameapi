#!/usr/bin/env python3
"""
CardDuel seed generator — production-quality catalog.

Emits a deterministic seed-data.sql with:
  - 11 abilities + 6 backing effects (the only abilities the MatchEngine implements)
  - default game ruleset + matchmaking assignments
  - 2 users (playerone/playertwo, password 123456)
  - 200 hand-curated cards (40 per faction) with themed names, balanced stats,
    faction-appropriate ability loadouts
  - card_abilities links (units only)
  - 4 valid starter decks (2 per user) so login + single-player are playable

Enums (authoritative, from Game/MatchEngine.cs):
  CardType  : Unit=0 Utility=1 Equipment=2 Spell=3
  CardRarity: Common=0 Rare=1 Epic=2 Legendary=3
  CardFaction: Ember=0 Tidal=1 Grove=2 Alloy=3 Void=4
  UnitType  : Melee=0 Ranged=1 Magic=2
  AllowedRow: FrontOnly=0 BackOnly=1 Flexible=2
  TargetSelectorKind: Self=0 FrontlineFirst=1 ...
"""
import random

random.seed(20260612)  # reproducible

# ability_id -> 'ability-N' (must match the abilities INSERT below / engine string gates)
ABIL = {
    "armor": "ability-1", "shield": "ability-2", "fly": "ability-3",
    "trample": "ability-4", "poison": "ability-5", "stun": "ability-6",
    "leech": "ability-7", "enrage": "ability-8", "regenerate_left": "ability-9",
    "taunt": "ability-10", "haste": "ability-11",
    # newly implemented in Game/MatchEngine.cs
    "cleave": "ability-12", "execute": "ability-13", "reflection": "ability-14",
    "last_stand": "ability-15", "regenerate": "ability-16",
    # OnPlay abilities for non-unit cards (Spell=3 / Equipment=2 / Utility=1).
    # These resolve against the PlayCard target the player chooses.
    "spell_strike": "ability-17",      # direct damage to target unit
    "spell_mend": "ability-18",        # heal target unit
    "spell_venom": "ability-19",       # apply poison to target unit
    "spell_freeze": "ability-20",      # stun target unit
    "equip_weapon": "ability-21",      # +attack buff for N turns
    "equip_armor": "ability-22",       # +armor buff for N turns
    "equip_curse": "ability-23",       # -attack debuff for N turns
    "util_bulwark": "ability-24",      # grant shield to friendly target
    "util_blessing": "ability-25",     # heal + small attack buff to friendly target
}

ABIL_DISPLAY = {
    "armor": "Armor", "shield": "Shield", "fly": "Fly", "trample": "Trample",
    "poison": "Poison", "stun": "Stun", "leech": "Leech", "enrage": "Enrage",
    "regenerate_left": "Regenerate Left", "taunt": "Taunt", "haste": "Haste",
    "cleave": "Cleave", "execute": "Execute", "reflection": "Reflection",
    "last_stand": "Last Stand", "regenerate": "Regenerate",
    "spell_strike": "Strike", "spell_mend": "Mend", "spell_venom": "Envenom",
    "spell_freeze": "Freeze", "equip_weapon": "Weapon", "equip_armor": "Plating",
    "equip_curse": "Curse", "util_bulwark": "Bulwark", "util_blessing": "Blessing",
}

MELEE, RANGED, MAGIC = 0, 1, 2

# ----------------------------------------------------------------------------
# Faction definitions
# ----------------------------------------------------------------------------
FACTIONS = {
    0: {
        "key": "ember", "name": "Ember",
        "atk_lean": 0.62, "armor_tend": 0.10, "armor_max": 1,
        "unit_weights": [(MELEE, 0.60), (RANGED, 0.25), (MAGIC, 0.15)],
        "ability_pool": [("enrage", 3), ("haste", 3), ("trample", 2),
                         ("poison", 2), ("leech", 1), ("stun", 1),
                         ("execute", 2), ("cleave", 2)],
        "prefixes": ["Cinder", "Ember", "Ash", "Magma", "Pyre", "Scorch", "Forge",
                     "Char", "Blaze", "Molten", "Soot", "Flare", "Brimstone", "Coal",
                     "Smolder", "Wildfire"],
        "cores": ["Berserker", "Drake", "Wyrmling", "Golem", "Reaver", "Marauder",
                  "Salamander", "Hound", "Brute", "Zealot", "Imp", "Warbrand",
                  "Devil", "Conscript", "Vanguard", "Charger", "Razorback", "Pyromancer"],
        "legendaries": ["Ignar, the Everburning", "Vaelmora, Forgemother",
                        "Ashkhan the Unquenched"],
        "spells": ["Eruption", "Flame Lance", "Pyroclasm", "Ashen Bolt", "Wildfire Surge"],
        "equipment": ["Forgeheart Blade", "Molten Gauntlets", "Cinderbrand Axe"],
        "utility": ["War Banner", "Forge Rite"],
        "flavor": ["Born from the deepest forge-fires.",
                   "Its rage burns hotter than the anvil.",
                   "Ash trails wherever it treads.",
                   "Quench it and it only burns brighter.",
                   "Smoke and cinder herald its charge."],
    },
    1: {
        "key": "tidal", "name": "Tidal",
        "atk_lean": 0.45, "armor_tend": 0.30, "armor_max": 2,
        "unit_weights": [(MELEE, 0.30), (RANGED, 0.35), (MAGIC, 0.35)],
        "ability_pool": [("shield", 3), ("stun", 3), ("armor", 2),
                         ("taunt", 1), ("regenerate_left", 1), ("fly", 1),
                         ("reflection", 1), ("regenerate", 2)],
        "prefixes": ["Tide", "Frost", "Brine", "Coral", "Abyss", "Glacier", "Wave",
                     "Mist", "Riptide", "Hailstone", "Pearl", "Surge", "Maelstrom",
                     "Deepwater", "Sleet", "Undine"],
        "cores": ["Leviathan", "Mystic", "Naiad", "Serpent", "Warden", "Oracle",
                  "Tidecaller", "Hexer", "Turtle", "Mariner", "Siren", "Frostling",
                  "Guardian", "Tempest", "Channeler", "Eel", "Drowned", "Mistweaver"],
        "legendaries": ["Nereth, Voice of the Deep", "Glacius, the Frozen Crown",
                        "Maris, Tidemother"],
        "spells": ["Tidal Wave", "Deep Freeze", "Frost Nova", "Riptide", "Whirlpool"],
        "equipment": ["Frostforged Spear", "Coral Aegis", "Tideglass Trident"],
        "utility": ["Tide Charm", "Frozen Ward"],
        "flavor": ["Patient as the pull of the moon.",
                   "It rose from the lightless trench.",
                   "Frost rimes its every breath.",
                   "The current bends to its will.",
                   "Drowned silence follows in its wake."],
    },
    2: {
        "key": "grove", "name": "Grove",
        "atk_lean": 0.40, "armor_tend": 0.12, "armor_max": 1,
        "unit_weights": [(MELEE, 0.45), (RANGED, 0.15), (MAGIC, 0.40)],
        "ability_pool": [("poison", 3), ("regenerate_left", 3), ("leech", 2),
                         ("taunt", 2), ("armor", 1),
                         ("regenerate", 3), ("reflection", 2), ("last_stand", 1)],
        "prefixes": ["Thorn", "Moss", "Bramble", "Spore", "Verdant", "Root", "Bloom",
                     "Vine", "Fang", "Petal", "Bog", "Sap", "Wild", "Grove", "Briar",
                     "Toxin"],
        "cores": ["Treant", "Druid", "Stalker", "Beast", "Shaman", "Spriggan",
                  "Warden", "Serpent", "Boar", "Spider", "Witch", "Ent", "Howler",
                  "Guardian", "Venomspine", "Lurker", "Antler", "Bloomling"],
        "legendaries": ["Sylvaan, the Worldroot", "Mirethorn, the Plagueroot",
                        "Kaelith, Warden of Roots"],
        "spells": ["Entangle", "Toxic Bloom", "Wild Growth", "Spore Cloud", "Regrowth"],
        "equipment": ["Thornvine Bow", "Barkplate Armor", "Venomfang Sickle"],
        "utility": ["Healing Totem", "Verdant Ritual"],
        "flavor": ["The grove remembers every wound.",
                   "Roots drink deep where it falls.",
                   "Venom drips from leaf and fang.",
                   "Life and rot grow from one seed.",
                   "It regrows faster than steel can cut."],
    },
    3: {
        "key": "alloy", "name": "Alloy",
        "atk_lean": 0.36, "armor_tend": 0.70, "armor_max": 3,
        "unit_weights": [(MELEE, 0.50), (RANGED, 0.40), (MAGIC, 0.10)],
        "ability_pool": [("armor", 4), ("taunt", 3), ("shield", 2), ("trample", 1),
                         ("last_stand", 2), ("reflection", 2), ("cleave", 1)],
        "prefixes": ["Iron", "Steel", "Brass", "Cog", "Rune", "Gear", "Forge",
                     "Chrome", "Bolt", "Plated", "Alloy", "Bronze", "Rivet", "Anvil",
                     "Tungsten", "Piston"],
        "cores": ["Golem", "Automaton", "Sentry", "Knight", "Colossus", "Turret",
                  "Guardian", "Construct", "Juggernaut", "Defender", "Warforged",
                  "Bastion", "Engine", "Vanguard", "Reckoner", "Marshal", "Cuirass",
                  "Sentinel"],
        "legendaries": ["Magnaron, the Iron Wall", "Cogsworth Prime",
                        "Aurex, the Living Bastion"],
        "spells": ["Overcharge", "Steel Rain", "System Shock", "Magnetize", "Recalibrate"],
        "equipment": ["Tungsten Greataxe", "Aegis Plating", "Pneumatic Hammer"],
        "utility": ["Repair Bay", "Power Core"],
        "flavor": ["Built to outlast every siege.",
                   "Bolts tighten as the battle wears on.",
                   "No blade has found its seam.",
                   "Gears grind on long after flesh tires.",
                   "It answers only to the foundry."],
    },
    4: {
        "key": "void", "name": "Void",
        "atk_lean": 0.50, "armor_tend": 0.08, "armor_max": 1,
        "unit_weights": [(MELEE, 0.25), (RANGED, 0.30), (MAGIC, 0.45)],
        "ability_pool": [("leech", 3), ("fly", 2), ("stun", 2),
                         ("poison", 2), ("enrage", 1), ("haste", 1),
                         ("execute", 3), ("last_stand", 1)],
        "prefixes": ["Void", "Shadow", "Dusk", "Wraith", "Null", "Eclipse", "Umbra",
                     "Hollow", "Gloom", "Star", "Rift", "Nether", "Phantom", "Abyssal",
                     "Whisper", "Oblivion"],
        "cores": ["Horror", "Sorcerer", "Apparition", "Reaper", "Devourer", "Seer",
                  "Stalker", "Fiend", "Specter", "Warlock", "Banshee", "Revenant",
                  "Maw", "Acolyte", "Drifter", "Eidolon", "Shade", "Herald"],
        "legendaries": ["Nyxara, the Unmaking", "Vorthal, Eye of the Rift",
                        "Mordreth the Hollow King"],
        "spells": ["Void Bolt", "Soul Drain", "Eclipse", "Oblivion", "Rift Tear"],
        "equipment": ["Nightfang Dagger", "Shroud of Umbra", "Starless Scepter"],
        "utility": ["Cursed Sigil", "Rift Beacon"],
        "flavor": ["It feeds on what it unmakes.",
                   "Light bends and dies around it.",
                   "A whisper from beyond the rift.",
                   "It remembers a world that never was.",
                   "Where it gazes, things cease to be."],
    },
}

RARITY_NAME = {0: "Common", 1: "Rare", 2: "Epic", 3: "Legendary"}

# Non-unit OnPlay loadouts per faction. Each non-unit card gets exactly one OnPlay ability whose
# effect scales with mana (computed at emit time). Themed so each faction plays to its identity.
#   spell  -> offensive/instant (damage, poison, stun)
#   equip  -> timed buff (friendly weapon/armor) or debuff (enemy curse)
#   util   -> friendly support (heal+buff or shield)
NONUNIT_LOADOUTS = {
    0: {"spell": "spell_strike", "equip": "equip_weapon", "util": "util_blessing"},  # Ember: burn + sharpen
    1: {"spell": "spell_freeze", "equip": "equip_armor",  "util": "util_bulwark"},   # Tidal: control + shield
    2: {"spell": "spell_venom",  "equip": "equip_curse",  "util": "util_blessing"},  # Grove: poison + sap
    3: {"spell": "spell_strike", "equip": "equip_armor",  "util": "util_bulwark"},   # Alloy: steel + plate
    4: {"spell": "spell_strike", "equip": "equip_curse",  "util": "util_blessing"},  # Void: drain + curse
}

# rarity -> mana range (weighted toward middle of range)
MANA_RANGE = {0: (1, 4), 1: (2, 6), 2: (4, 8), 3: (6, 10)}
RARITY_BONUS = {0: 0, 1: 1, 2: 2, 3: 4}
# number-of-abilities distribution per rarity (units)
NABIL_DIST = {
    0: [(0, 0.55), (1, 0.45)],
    1: [(1, 0.80), (2, 0.20)],
    2: [(1, 0.40), (2, 0.60)],
    3: [(2, 0.60), (3, 0.40)],
}

# per-faction slot plan (rarity, card_type) -> 40 cards
def faction_slots():
    slots = []
    slots += [(3, 0)] * 3                       # Legendary: 3 units
    slots += [(2, 0)] * 5 + [(2, 3), (2, 2)]     # Epic: 5 units, 1 spell, 1 equip
    slots += [(1, 0)] * 9 + [(1, 3), (1, 2), (1, 1)]  # Rare: 9 units,1 spell,1 equip,1 util
    slots += [(0, 0)] * 15 + [(0, 3), (0, 2), (0, 1)]  # Common:15 units,1 spell,1 equip,1 util
    assert len(slots) == 40, len(slots)
    return slots

def wpick(pairs):
    items = [i for i, _ in pairs]
    weights = [w for _, w in pairs]
    return random.choices(items, weights=weights, k=1)[0]

def mana_for(rarity):
    lo, hi = MANA_RANGE[rarity]
    # triangular toward middle for a smoother curve
    return int(round(random.triangular(lo, hi, (lo + hi) / 2)))

def sql_str(s):
    return "'" + s.replace("'", "''") + "'"

# ----------------------------------------------------------------------------
# Generate cards
# ----------------------------------------------------------------------------
cards = []          # dicts
card_abilities = [] # (card_id, ability_def_id, seq)
gid = 0             # global card numeric id
ca_id = 0

for fac_id, fac in FACTIONS.items():
    used_names = set()
    legend_iter = iter(fac["legendaries"])
    spell_iter = iter(fac["spells"])
    equip_iter = iter(fac["equipment"])
    util_iter = iter(fac["utility"])
    fac_card_index = 0

    for (rarity, ctype) in faction_slots():
        gid += 1
        fac_card_index += 1
        card_id = f'{fac["key"]}_{fac_card_index:04d}'
        cid = f"card-{gid}"
        mana = mana_for(rarity)

        if ctype == 0:  # Unit
            if rarity == 3:
                name = next(legend_iter)
            else:
                for _ in range(200):
                    cand = f'{random.choice(fac["prefixes"])} {random.choice(fac["cores"])}'
                    if cand not in used_names:
                        name = cand
                        break
                else:
                    name = cand + f" {fac_card_index}"
            used_names.add(name)

            unit_type = wpick(fac["unit_weights"])
            # abilities
            n_ab = wpick(NABIL_DIST[rarity])
            pool = fac["ability_pool"]
            chosen = []
            attempts = 0
            while len(chosen) < n_ab and attempts < 30:
                a = wpick(pool)
                if a not in chosen:
                    chosen.append(a)
                attempts += 1
            # taunt sensible on front; if magic/ranged got taunt keep (legal). leave as is.

            # stat budget
            budget = mana * 2 + 1 + RARITY_BONUS[rarity] - len(chosen)
            budget = max(budget, 2)
            armor = 0
            if random.random() < fac["armor_tend"]:
                armor = random.randint(1, fac["armor_max"])
            if "armor" in chosen and armor == 0:
                armor = random.randint(1, fac["armor_max"])
            atk = max(1, int(round(budget * fac["atk_lean"])))
            hp = max(1, budget - atk)
            # legendaries are bombs: round up
            if rarity == 3:
                atk += 1
                hp += 2

            allowed_row = 0 if unit_type == MELEE else 1
            if rarity >= 2 and random.random() < 0.20:
                allowed_row = 2  # flexible elites

            # description
            flavor = random.choice(fac["flavor"])
            if chosen:
                kw = ", ".join(ABIL_DISPLAY[a] for a in chosen)
                desc = f"{flavor} {kw}."
            else:
                desc = flavor

            cards.append(dict(
                id=cid, card_id=card_id, name=name, desc=desc, mana=mana,
                atk=atk, hp=hp, armor=armor, ctype=0, rarity=rarity, faction=fac_id,
                unit_type=unit_type, allowed_row=allowed_row, selector=1,
                turns=1, limited=False,
            ))
            for seq, a in enumerate(chosen):
                ca_id += 1
                card_abilities.append((cid, ABIL[a], seq))

        else:  # non-unit (Spell=3 / Equipment=2 / Utility=1)
            loadout = NONUNIT_LOADOUTS[fac_id]
            if ctype == 3:
                name = next(spell_iter)
                ability_key = loadout["spell"]
                desc_base = f"Target a unit on play. {ABIL_DISPLAY[ability_key]}, then discard."
            elif ctype == 2:
                name = next(equip_iter)
                ability_key = loadout["equip"]
                desc_base = f"Attach to a unit on play. {ABIL_DISPLAY[ability_key]} for a few turns."
            else:
                name = next(util_iter)
                ability_key = loadout["util"]
                desc_base = f"Target a friendly unit on play. {ABIL_DISPLAY[ability_key]}."
            flavor = random.choice(fac["flavor"])
            desc = f"{flavor} {desc_base}"
            cards.append(dict(
                id=cid, card_id=card_id, name=name, desc=desc, mana=mana,
                atk=0, hp=0, armor=0, ctype=ctype, rarity=rarity, faction=fac_id,
                unit_type=None, allowed_row=2, selector=0, turns=0, limited=False,
            ))
            ca_id += 1
            card_abilities.append((cid, ABIL[ability_key], 0))

# ----------------------------------------------------------------------------
# Decks: 2 per user, built from units of two factions each (<=3 copies, 24 cards)
# ----------------------------------------------------------------------------
units_by_faction = {f: [c for c in cards if c["faction"] == f and c["ctype"] == 0]
                    for f in FACTIONS}

def build_deck(fac_ids, size=24):
    pool = []
    for f in fac_ids:
        pool += units_by_faction[f]
    # sort by mana for a sensible curve, take copies
    pool = sorted(pool, key=lambda c: (c["mana"], c["card_id"]))
    chosen = []
    i = 0
    while len(chosen) < size and pool:
        c = pool[i % len(pool)]
        copies = min(2, size - len(chosen))
        for _ in range(copies):
            chosen.append(c)
        i += 1
        if i > len(pool) * 3:
            break
    return chosen[:size]

# Test deck: 30 cards = 15 units + 5 spell + 5 equipment + 5 utility, so every card type/effect
# can be drawn and exercised. Distinct cards (no duplicates) across factions.
def build_test_deck():
    units = sorted([c for c in cards if c["ctype"] == 0], key=lambda c: (c["mana"], c["card_id"]))
    spells = [c for c in cards if c["ctype"] == 3]
    equips = [c for c in cards if c["ctype"] == 2]
    utils = [c for c in cards if c["ctype"] == 1]
    return units[:15] + spells[:5] + equips[:5] + utils[:5]

DECKS = [
    ("deck-1", "user-1", "deck_playerone_ember", "Ember Aggro", build_deck([0])),
    ("deck-2", "user-1", "deck_playerone_alloy", "Alloy Wall", build_deck([3])),
    ("deck-3", "user-2", "deck_playertwo_grove", "Grove Venom", build_deck([2])),
    ("deck-4", "user-2", "deck_playertwo_void",  "Void Tempo", build_deck([4])),
    ("deck-5", "user-1", "deck_playerone_test",  "Test All Types", build_test_deck()),
    ("deck-6", "user-2", "deck_playertwo_test",  "Test All Types", build_test_deck()),
]

# ----------------------------------------------------------------------------
# Emit SQL
# ----------------------------------------------------------------------------
out = []
w = out.append

w("-- ============================================================")
w("-- CardDuel production seed (generated by tools/generate_seed.py)")
w("-- 11 abilities, 200 curated cards, 2 users, 4 starter decks.")
w("-- Idempotent: clears gameplay/catalog data first. Lookup/definition")
w("-- tables (enum lookups, item types) are left untouched.")
w("-- ============================================================")
w("BEGIN;")
w("")
w("-- Clear dependents first (FKs into cards/users/matches) so the catalog can be re-seeded.")
w("DELETE FROM player_card_upgrades;")
w("DELETE FROM player_cards;")
w("DELETE FROM player_items;")
w("DELETE FROM card_crafting_requirements;")
w("DELETE FROM match_actions;")
w("DELETE FROM replay_logs;")
w("DELETE FROM ratings;")
w("DELETE FROM matches;")
w("DELETE FROM audit_logs;")
w("DELETE FROM deck_cards;")
w("DELETE FROM decks;")
w("DELETE FROM card_abilities;")
w("DELETE FROM effects;")
w("DELETE FROM abilities;")
w("DELETE FROM cards;")
w("DELETE FROM matchmaking_mode_ruleset_assignments;")
w("DELETE FROM game_rulesets;")
w("DELETE FROM users;")
w("")

# Abilities (the 11 the engine implements)
w("-- Abilities. skill_type: 0 def,1 off,2 equip,3 util,4 modifier. trigger: 0 play,1 turn-start,2 turn-end,3 battle.")
w("INSERT INTO abilities (id, ability_id, display_name, description, skill_type, trigger_kind, target_selector_kind, animation_cue_id, conditions_json, metadata_json, created_at) VALUES")
abilities_rows = [
 ("ability-1","armor","Armor","Gains persistent armor when played",0,0,0,"skill_armor_gain",'{"battleOrder":"on_play"}'),
 ("ability-2","shield","Shield","Negates the next damage event received",0,0,0,"skill_shield_gain",'{"battleOrder":"on_play"}'),
 ("ability-3","fly","Fly","Bypasses non-flying defenders and attacks the hero directly",4,3,0,"skill_fly_bypass",'{"normalAttackModifier":true}'),
 ("ability-4","trample","Trample","Ignores armor when attacking",4,3,0,"skill_trample_hit",'{"normalAttackModifier":true}'),
 ("ability-5","poison","Poison","Applies poison to damaged enemies",1,3,0,"skill_poison_apply",'{"normalAttackModifier":true}'),
 ("ability-6","stun","Stun","The next damaged enemy skips its next attack; consumed after use",1,3,0,"skill_stun_apply",'{"oneShotPerCard":true}'),
 ("ability-7","leech","Leech","Heals the attacker for health damage dealt to cards",1,3,0,"skill_leech_heal",'{"normalAttackModifier":true}'),
 ("ability-8","enrage","Enrage","Attacks twice, then skips its next attack opportunity",1,3,0,"skill_enrage_double",'{"normalAttackModifier":true}'),
 ("ability-9","regenerate_left","Regenerate","Heals the ally in the left slot at end of turn",3,2,6,"skill_regenerate_left",'{"regenTarget":"left"}'),
 ("ability-10","taunt","Taunt","Enemy frontline targeting must choose this card first",3,3,0,"skill_taunt_pulse",'{"targetingModifier":true}'),
 ("ability-11","haste","Haste","Can attack on the turn it is played",4,3,0,"skill_haste_ready",'{"normalAttackModifier":true}'),
 ("ability-12","cleave","Cleave","Also strikes the enemy's other units when it attacks",4,3,0,"skill_cleave",'{"normalAttackModifier":true}'),
 ("ability-13","execute","Execute","Instantly destroys a damaged enemy at or below its execute threshold",1,3,0,"skill_execute",'{"normalAttackModifier":true}'),
 ("ability-14","reflection","Reflection","Reflects fixed damage back at attackers that strike it",0,3,0,"skill_reflection",'{"normalAttackModifier":true}'),
 ("ability-15","last_stand","Last Stand","The first lethal blow leaves it at 1 health; used once",0,3,0,"skill_last_stand",'{"normalAttackModifier":true}'),
 ("ability-16","regenerate","Regenerate","Heals itself at the end of its turn",3,2,0,"skill_regenerate",'{"regenTarget":"self"}'),
 # OnPlay (trigger 0) abilities for non-unit cards. Resolve against the chosen PlayCard target.
 ("ability-17","spell_strike","Strike","Deal direct damage to the target unit",1,0,1,"spell_strike",'{"cardType":"spell"}'),
 ("ability-18","spell_mend","Mend","Heal the target unit",3,0,4,"spell_mend",'{"cardType":"spell"}'),
 ("ability-19","spell_venom","Envenom","Poison the target unit for several turns",1,0,1,"spell_venom",'{"cardType":"spell"}'),
 ("ability-20","spell_freeze","Freeze","Stun the target unit so it skips its next attack",1,0,1,"spell_freeze",'{"cardType":"spell"}'),
 ("ability-21","equip_weapon","Weapon","Grants bonus attack for a few turns",2,0,5,"equip_weapon",'{"cardType":"equipment"}'),
 ("ability-22","equip_armor","Plating","Grants bonus armor for a few turns",0,0,2,"equip_armor",'{"cardType":"equipment"}'),
 ("ability-23","equip_curse","Curse","Saps the target unit's attack for a few turns",1,0,3,"equip_curse",'{"cardType":"equipment"}'),
 ("ability-24","util_bulwark","Bulwark","Grants a friendly unit a protective shield",0,0,5,"util_bulwark",'{"cardType":"utility"}'),
 ("ability-25","util_blessing","Blessing","Heals and sharpens a friendly unit",3,0,5,"util_blessing",'{"cardType":"utility"}'),
]
rows = []
for (i,aid,dn,desc,st,tk,ts,cue,meta) in abilities_rows:
    rows.append(f"('{i}', '{aid}', {sql_str(dn)}, {sql_str(desc)}, {st}, {tk}, {ts}, '{cue}', '{{}}'::jsonb, '{meta}'::jsonb, NOW())")
w(",\n".join(rows) + ";")
w("")

# Effects backing triggered abilities. effect_kind: 1 Heal,2 GainArmor,27 Haste,28 AddShield,29 ApplyPoison,30 ApplyStun.
w("-- Effects backing triggered abilities.")
w("INSERT INTO effects (id, ability_definition_id, effect_kind, amount, secondary_amount, duration_turns, target_selector_kind_override, sequence, metadata_json, created_at) VALUES")
effect_rows = [
 ("effect-1","ability-1",2,3,"NULL","NULL","NULL",0,'{"animationStep":"armor"}'),
 ("effect-2","ability-2",28,1,"NULL",99,"NULL",0,'{"animationStep":"shield"}'),
 ("effect-3","ability-5",29,1,"NULL",2,"NULL",0,'{"animationStep":"poison"}'),
 ("effect-4","ability-6",30,0,"NULL",1,"NULL",0,'{"animationStep":"stun"}'),
 ("effect-5","ability-9",1,2,"NULL","NULL",6,0,'{"animationStep":"regen-left"}'),
 ("effect-6","ability-11",27,1,"NULL","NULL","NULL",0,'{"animationStep":"haste-ready"}'),
 ("effect-7","ability-13",15,3,"NULL","NULL","NULL",0,'{"animationStep":"execute","note":"executeThreshold"}'),
 ("effect-8","ability-14",10,2,"NULL","NULL","NULL",0,'{"animationStep":"reflection","note":"reflectDamage"}'),
 ("effect-9","ability-12",21,0,"NULL","NULL","NULL",0,'{"animationStep":"cleave","note":"usesAttackerAttack"}'),
 ("effect-10","ability-15",22,0,"NULL","NULL","NULL",0,'{"animationStep":"last-stand"}'),
 ("effect-11","ability-16",1,2,"NULL","NULL","NULL",0,'{"animationStep":"regen-self"}'),
 # Non-unit OnPlay effects. effect_kind: 0 Damage,1 Heal,2 GainArmor,3 BuffAttack,28 AddShield,29 ApplyPoison,30 ApplyStun.
 ("effect-12","ability-17",0,4,"NULL","NULL","NULL",0,'{"animationStep":"spell-strike"}'),
 ("effect-13","ability-18",1,4,"NULL","NULL","NULL",0,'{"animationStep":"spell-mend"}'),
 ("effect-14","ability-19",29,2,"NULL",3,"NULL",0,'{"animationStep":"spell-venom"}'),
 ("effect-15","ability-20",30,0,"NULL",1,"NULL",0,'{"animationStep":"spell-freeze"}'),
 ("effect-16","ability-21",3,2,"NULL",3,"NULL",0,'{"animationStep":"equip-weapon"}'),
 ("effect-17","ability-22",2,3,"NULL",3,"NULL",0,'{"animationStep":"equip-armor"}'),
 ("effect-18","ability-23",3,-2,"NULL",3,"NULL",0,'{"animationStep":"equip-curse"}'),
 ("effect-19","ability-24",28,1,"NULL",99,"NULL",0,'{"animationStep":"util-bulwark"}'),
 ("effect-20","ability-25",1,3,"NULL","NULL","NULL",0,'{"animationStep":"util-blessing-heal"}'),
 ("effect-21","ability-25",3,1,"NULL",3,"NULL",1,'{"animationStep":"util-blessing-buff"}'),
]
rows = []
for (i,ad,ek,amt,sec,dur,tso,seq,meta) in effect_rows:
    rows.append(f"('{i}', '{ad}', {ek}, {amt}, {sec}, {dur}, {tso}, {seq}, '{meta}'::jsonb, NOW())")
w(",\n".join(rows) + ";")
w("")

# Default ruleset + matchmaking assignments
w("-- Default ruleset (hero 20, full 20 mana from turn 1 to match the client DuelRulesProfile, draw 4 +1/turn).")
w("INSERT INTO game_rulesets (id, ruleset_key, display_name, description, is_active, is_default, starting_hero_health, max_hero_health, starting_mana, max_mana, mana_granted_per_turn, mana_grant_timing, initial_draw_count, cards_drawn_on_turn_start, starting_seat_index, created_at) VALUES")
w("('ruleset-default', 'default', 'Default Rules', 'Default server ruleset for casual, ranked and private matches.', true, true, 20, 20, 20, 20, 20, 0, 4, 1, 0, NOW());")
w("")
w("INSERT INTO matchmaking_mode_ruleset_assignments (id, mode, ruleset_id, created_at) VALUES")
w("('mmr-casual', 0, 'ruleset-default', NOW()),")
w("('mmr-ranked', 1, 'ruleset-default', NOW()),")
w("('mmr-private', 2, 'ruleset-default', NOW());")
w("")

# Users (password 123456, SHA256 legacy fallback hash accepted by the API)
w("-- Users. password = '123456' (SHA256 base64 legacy hash; the API accepts it).")
w("INSERT INTO users (id, email, username, password_hash, is_active, created_at) VALUES")
w("('user-1', 'playerone@flippy.com', 'PlayerOne', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', true, NOW()),")
w("('user-2', 'playertwo@flippy.com', 'PlayerTwo', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', true, NOW());")
w("")

# Cards
w(f"-- {len(cards)} cards.")
w("INSERT INTO cards (id, card_id, display_name, description, mana_cost, attack, health, armor, card_type, card_rarity, card_faction, unit_type, allowed_row, default_attack_selector, turns_until_can_attack, is_limited, created_at) VALUES")
rows = []
for c in cards:
    ut = "NULL" if c["unit_type"] is None else str(c["unit_type"])
    lim = "true" if c["limited"] else "false"
    rows.append(
        f"('{c['id']}', '{c['card_id']}', {sql_str(c['name'])}, {sql_str(c['desc'])}, "
        f"{c['mana']}, {c['atk']}, {c['hp']}, {c['armor']}, {c['ctype']}, {c['rarity']}, "
        f"{c['faction']}, {ut}, {c['allowed_row']}, {c['selector']}, {c['turns']}, {lim}, NOW())"
    )
w(",\n".join(rows) + ";")
w("")

# Card abilities
w(f"-- {len(card_abilities)} card/ability links (units only).")
w("INSERT INTO card_abilities (id, card_definition_id, ability_definition_id, sequence) VALUES")
rows = []
for n, (cid, aid, seq) in enumerate(card_abilities, start=1):
    rows.append(f"('ca-{n}', '{cid}', '{aid}', {seq})")
w(",\n".join(rows) + ";")
w("")

# Decks
w("-- Starter decks (2 per user).")
w("INSERT INTO decks (id, user_id, deck_id, display_name, created_at) VALUES")
rows = [f"('{d[0]}', '{d[1]}', '{d[2]}', {sql_str(d[3])}, NOW())" for d in DECKS]
w(",\n".join(rows) + ";")
w("")
w("INSERT INTO deck_cards (id, deck_id, card_definition_id, position, created_at) VALUES")
rows = []
for d in DECKS:
    deck_id = d[0]
    for pos, c in enumerate(d[4]):
        rows.append(f"('dc-{deck_id}-{pos}', '{deck_id}', '{c['id']}', {pos}, NOW())")
w(",\n".join(rows) + ";")
w("")

w("COMMIT;")
w("")
w("-- Verify")
w("SELECT 'cards' n, count(*) FROM cards UNION ALL SELECT 'abilities', count(*) FROM abilities "
  "UNION ALL SELECT 'effects', count(*) FROM effects UNION ALL SELECT 'card_abilities', count(*) FROM card_abilities "
  "UNION ALL SELECT 'game_rulesets', count(*) FROM game_rulesets UNION ALL SELECT 'users', count(*) FROM users "
  "UNION ALL SELECT 'decks', count(*) FROM decks UNION ALL SELECT 'deck_cards', count(*) FROM deck_cards ORDER BY 1;")

with open("seed-data.sql", "w", encoding="utf-8") as f:
    f.write("\n".join(out))

# console summary
from collections import Counter
print(f"cards={len(cards)} card_abilities={len(card_abilities)}")
for f, fac in FACTIONS.items():
    fc = [c for c in cards if c["faction"] == f]
    rc = Counter(c["rarity"] for c in fc)
    tc = Counter(c["ctype"] for c in fc)
    print(f"  {fac['name']:6} total={len(fc)} rarity(C/R/E/L)={rc[0]}/{rc[1]}/{rc[2]}/{rc[3]} "
          f"type(U/Ut/Eq/Sp)={tc[0]}/{tc[1]}/{tc[2]}/{tc[3]}")
print("deck sizes:", [len(d[4]) for d in DECKS])
