# Skills Battle Phase Contract

This document describes the server-authoritative skill and battle phase contract that Unity should consume.

## Core Rule

The server owns all battle math.

The client must not recalculate:

- skill execution
- target selection
- damage
- armor absorption
- shield blocks
- poison ticks
- stun skips
- enrage cooldowns
- death and board compaction

The client should animate `battleEvents` in ascending `sequence` order, then reconcile against the final snapshot state.

## Snapshot Additions

`MatchSnapshot` now includes:

```json
{
  "battleEvents": []
}
```

Each board card now includes:

```json
{
  "statusEffects": [
    {
      "kind": 0,
      "amount": 1,
      "remainingTurns": 2,
      "sourceRuntimeId": "runtime-id",
      "abilityId": "poison"
    }
  ]
}
```

## Battle Event Shape

Every event has a stable order:

```json
{
  "eventId": "evt-000001",
  "sequence": 1,
  "kind": "skill_begin",
  "sourceSeatIndex": 0,
  "sourceRuntimeId": "runtime-source",
  "targetSeatIndex": 1,
  "targetRuntimeId": "runtime-target",
  "abilityId": "poison",
  "effectKind": 29,
  "amount": 1,
  "hpBefore": 5,
  "hpAfter": 4,
  "armorBefore": 2,
  "armorAfter": 0,
  "statusKind": 0,
  "durationTurns": 2,
  "message": "Poison applied to Target."
}
```

Fields can be `null` when they do not apply.

## Event Ordering

Unity should sort by `sequence` and play exactly in that order.

Typical order for one attacking card:

1. `skill_begin` events for declared battle-phase skills and modifiers.
2. `card_attack`.
3. `card_damage`, `shield_block`, or `hero_damage`.
4. status events such as `status_applied`.
5. follow-up skill events such as `heal`.
6. `death` or compaction-related final state via snapshot.

The snapshot is already resolved after the server finishes the full action.

## Current Event Kinds

- `skill_begin`
- `attack_not_ready`
- `card_attack`
- `card_damage`
- `hero_damage`
- `shield_block`
- `status_applied`
- `status_expired`
- `stun_skip`
- `enrage_cooldown_skip`
- `heal`
- `armor_gain`
- `attack_buff`
- `fly_bypass`
- `death`

Treat unknown event kinds as animation-safe no-ops and still continue to the next event.

## Status Effect Kinds

Current enum values:

- `0`: `Poison`
- `1`: `Stun`
- `2`: `Shield`
- `3`: `EnrageCooldown`

Use the enum number for compatibility, and optionally map it to display text locally.

## Ability And Effect Data

Abilities now carry more metadata:

```json
{
  "abilityId": "poison",
  "displayName": "Poison",
  "skillType": 1,
  "triggerKind": 3,
  "targetSelectorKind": 0,
  "animationCueId": "skill_poison_apply",
  "conditionsJson": "{}",
  "metadataJson": "{\"normalAttackModifier\":true}",
  "effects": []
}
```

Effects now support flexible data:

```json
{
  "effectKind": 29,
  "amount": 1,
  "secondaryAmount": null,
  "durationTurns": 2,
  "targetSelectorKindOverride": null,
  "sequence": 0,
  "metadataJson": "{\"animationStep\":\"poison\"}"
}
```

The client can use `animationCueId` and effect `metadataJson` for presentation, but must not use them for battle math.

## Current Skill Semantics

`armor`

On play, adds persistent armor to the card.

`shield`

On play, adds a shield status. The next damage event is blocked and emits `shield_block`.

`fly`

During normal attack, if the chosen defender does not also have `fly`, the attack bypasses that defender and hits the enemy hero.

`trample`

Normal attacks ignore armor and damage health directly.

`poison`

When the attacker deals health damage to an enemy card, poison is applied. Poison ticks at the beginning of that card owner's next battle phase.

`stun`

When the attacker deals health damage, stun is applied once. The stunned card skips its next attack and emits `stun_skip`.

`leech`

When the attacker deals health damage to a card, the attacker heals by that health damage amount. This can exceed original max HP.

`enrage`

The card attacks twice, then receives `EnrageCooldown`. On its next attack opportunity it emits `enrage_cooldown_skip` and does not attack.

`regenerate_left`

At end of turn, heals the ally left slot. Healing is capped to the card's starting max HP unless the event is leech.

`taunt`

Enemy `FrontlineFirst` targeting chooses the taunt card first while it is alive.

`haste`

The card can attack on the turn it is played.

## Client Implementation Checklist

- Read `snapshot.battleEvents`.
- Sort by `sequence`.
- Play animations using `kind`, `abilityId`, `effectKind`, `sourceRuntimeId`, and `targetRuntimeId`.
- Use `hpBefore/hpAfter` and `armorBefore/armorAfter` for visual counters during playback.
- Use `status_applied` and `status_expired` to animate status badges.
- After playback, apply the final snapshot as the authoritative board state.
- Keep raw `logs` as debug text only; do not parse them for battle playback.

## Compatibility Notes

The server sends a rolling window of recent battle events. The client should start playback from the newest sequence it has not processed yet.

If the client reconnects mid-match, it should skip old events and render the snapshot immediately unless it has a known previous sequence.
