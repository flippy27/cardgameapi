# Battle And Card Visuals Frontend Contract

This document explains what the Unity client must consume after the latest server changes around:

- authoritative board placement and slot shifting
- parseable gameplay errors
- authoritative turn ownership fields
- battle presentation metadata
- flexible card visual composition for hand and played states

## 1. Placement rules are now server-authoritative

The server now models the board with the documented priority:

- `top`
- `left`
- `right`

Internally that maps to:

- `BoardSlot.Front` = `top`
- `BoardSlot.BackLeft` = `left`
- `BoardSlot.BackRight` = `right`

### Authoritative shift behavior

When a card is played into an occupied slot, the server shifts cards instead of rejecting the move when space exists:

- play into `top`
  - old `top` moves to `left`
  - old `left` moves to `right`
- play into `left`
  - old `left` moves to `right`
- play into `right`
  - no shifting; this still requires `right` to be free

### Authoritative compaction behavior

When a board card dies:

- if `top` becomes empty, `left` moves to `top` and `right` moves to `left`
- if `left` becomes empty, `right` moves to `left`

### Frontend implication

The client should not locally reject a play only because the hovered slot is already occupied.

Instead:

- ask the server to play the card in the target slot
- animate optimistic preview using the same shift rules
- always reconcile with the next authoritative snapshot

## 2. Match snapshot fields the client should trust

Each snapshot now includes self-consistent turn ownership data:

```json
{
  "localSeatIndex": 0,
  "activeSeatIndex": 0,
  "activePlayerId": "player-uuid",
  "isLocalPlayersTurn": true
}
```

Each seat also includes its real player id:

```json
{
  "seatIndex": 0,
  "playerId": "player-uuid",
  "connected": true,
  "ready": true
}
```

### Frontend implication

Use these fields exactly as sent:

- use `isLocalPlayersTurn` to enable or disable turn actions
- use `activeSeatIndex` for turn banners and battle flow
- use `seats[i].playerId` for identity mapping instead of guessing from hidden-hand state

Do not infer turn ownership from previous local state.

## 3. Parseable gameplay errors

For gameplay actions, the server now returns structured errors instead of only raw text.

### REST shape

For endpoints like:

- `POST /api/v1/matches/{matchId}/play`
- `POST /api/v1/matches/{matchId}/end-turn`
- `POST /api/v1/matches/{matchId}/ready`
- `POST /api/v1/matches/{matchId}/forfeit`

the response body on `400` is:

```json
{
  "code": "not_enough_mana",
  "message": "Not enough mana."
}
```

### SignalR shape

For hub methods like:

- `PlayCard`
- `EndTurn`
- `SetReady`
- `Forfeit`

the server throws a `HubException` whose message is a JSON string with the same shape:

```json
{
  "code": "not_enough_mana",
  "message": "Not enough mana."
}
```

### Current error codes

- `match_not_playable`
- `player_not_in_match`
- `not_your_turn`
- `card_not_found`
- `not_enough_mana`
- `front_slot_required`
- `left_slot_required`
- `board_lane_full`
- `invalid_row_front_only`
- `invalid_row_back_only`

### Frontend implication

The Unity client should parse the payload and branch on `code`, not on human text.

Recommended handling:

- `not_enough_mana`: show mana warning toast or inline banner
- `not_your_turn`: disable action UI and force-refresh local turn state from latest snapshot
- `front_slot_required` or `left_slot_required`: show placement guidance
- `board_lane_full`: show slot-full guidance only when no legal shift exists

## 4. Board-card battle presentation metadata

Board cards in snapshots now carry presentation hints:

```json
{
  "runtimeId": "runtime-card-id",
  "cardId": "card-id",
  "displayName": "Card Name",
  "attack": 4,
  "currentHealth": 5,
  "maxHealth": 5,
  "armor": 1,
  "attackMotionLevel": 4,
  "attackShakeLevel": 2,
  "attackDeliveryType": "beam"
}
```

### Meaning

- `attackMotionLevel`
  - `0` = client fallback/auto
  - `1..5` = choose motion preset
- `attackShakeLevel`
  - `0` = client fallback/auto
  - `1..5` = choose shake preset
- `attackDeliveryType`
  - optional explicit delivery hint
  - expected examples: `melee`, `projectile`, `beam`, `arc`

### Frontend implication

Use server values when present.

Fallback only when values are `0` or `null`.

The client should stop inferring delivery only from slot position when `attackDeliveryType` exists.

## 5. Flexible card visuals: no rigid full-art bool

The card catalog now supports flexible visual composition using `visualProfiles`.

This is meant to support:

- normal hand card layouts
- played/on-board card layouts
- rarity-based frames
- faction-based frames
- alternate art
- full-art variants
- future presentation modes without schema redesign

### Card DTO shape

`GET /api/v1/cards/{cardId}` now returns:

```json
{
  "cardId": "visual_card",
  "displayName": "Visual Card",
  "battlePresentation": {
    "attackMotionLevel": 4,
    "attackShakeLevel": 2,
    "attackDeliveryType": "beam",
    "impactFxId": "beam-hit",
    "attackAudioCueId": "beam-audio",
    "metadataJson": "{\"trail\":\"long\"}"
  },
  "visualProfiles": [
    {
      "profileKey": "hand-default",
      "displayName": "Hand Default",
      "isDefault": true,
      "layers": [
        {
          "surface": "hand",
          "layer": "frame",
          "sourceKind": "sprite",
          "assetRef": "frames/epic-hand",
          "sortOrder": 0,
          "metadataJson": null
        },
        {
          "surface": "hand",
          "layer": "art",
          "sourceKind": "image",
          "assetRef": "art/visual-card",
          "sortOrder": 1,
          "metadataJson": null
        }
      ]
    }
  ]
}
```

### Important model concepts

- `visualProfiles`
  - a card can expose many profiles
- `surface`
  - where the visual is meant to be used
  - current expected values: `hand`, `played`
- `layer`
  - semantic layer inside a card composition
  - current expected values include `frame`, `art`
- `sourceKind`
  - how the client resolves the resource
  - current expected values include `sprite`, `image`
- `assetRef`
  - server-authoritative resource identifier the client resolves through its asset pipeline
- `metadataJson`
  - optional future-safe extension point for per-layer configuration

### Recommended Unity selection strategy

For a given card state:

1. choose the requested `surface`
2. choose the desired profile by exact `profileKey` if the experience requests one
3. otherwise use the default profile for that surface
4. if there is no surface-specific default, fall back to the first profile that contains layers for that surface
5. render layers ordered by `sortOrder`

### Why there is no `isFullArt` bool

Full-art is a presentation variant, not a stable card property.

Using profiles/layers allows:

- one card to have many layouts
- temporary event skins
- premium variants
- future states besides only `hand` and `played`

without adding more booleans or schema branches.

## 6. Endpoints the frontend will consume

### Match flow

- `POST /api/v1/matchmaking/queue`
- `GET /api/v1/matches/{matchId}/snapshot/{playerId}`
- `POST /api/v1/matches/{matchId}/play`
- `POST /api/v1/matches/{matchId}/end-turn`
- SignalR hub `/hubs/match`

### Rules

- `GET /api/v1/matches/{matchId}/rules/{playerId}`

### Card catalog

- `GET /api/v1/cards`
- `GET /api/v1/cards/{cardId}`
- `GET /api/v1/cards/by-deck`

## 7. What Unity should change now

### Required changes

- stop treating an occupied `top` or `left` as automatically invalid
- parse structured gameplay errors by `code`
- trust `isLocalPlayersTurn` from the snapshot
- use `seats[].playerId` instead of inferring player identity
- consume `attackMotionLevel`, `attackShakeLevel`, and `attackDeliveryType`
- build card visuals from `visualProfiles` + `layers`

### Recommended changes

- add a card rendering resolver that maps `assetRef` to local assets
- add a surface-aware card renderer for `hand` vs `played`
- keep optimistic preview animations aligned with server shift rules
- when a hub action fails, parse `HubException.Message` as JSON before showing UI feedback

## 8. What is intentionally not finished yet

The server still does not expose structured `battleEvents`.

Battle animation playback is still based on snapshots plus logs, with better presentation hints but without a full event stream yet.

That should be the next major API improvement once the card presentation and placement contract is stable.
