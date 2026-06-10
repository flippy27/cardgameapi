# Server ↔ Unity Client Contract Review

Review date: 2026-06-10. Server: `CardDuel.ServerApi` (ASP.NET, System.Text.Json).
Client: `cardsGame` (Unity, `UnityEngine.JsonUtility`).

## Baseline (verified, matches)

- **JSON casing: camelCase on both sides.** Server uses `AddControllers()` with no
  `.AddJsonOptions(...)` and no Newtonsoft → System.Text.Json ASP.NET defaults =
  camelCase property names. Client `JsonUtility` fields are camelCase. ✅
- **Enums serialized as integers** on both sides. No global `JsonStringEnumConverter`
  registered server-side, so enums emit as numbers; client DTOs read them as `int`
  (`mode`, `phase`, `slot`, `kind`, `effectKind`, …). ✅
- **Gameplay request/response DTOs match** field-for-field: auth, matchmaking,
  match play (ready/play/end-turn/destroy-card/forfeit), `MatchSnapshot` +
  nested seat/hand/board/battle-event snapshots, decks upsert, player cards,
  inventory, crafting. ✅
- **SignalR hub** `/hubs/match?matchId=&access_token=`: client invoke names
  (`ConnectToMatch`, `SetReady`, `PlayCard`, `EndTurn`, `DestroyCard`, `Forfeit`)
  and the server→client `MatchSnapshot` event match. ✅
- **Auth**: JWT bearer in `Authorization` header for REST; `?access_token=` query
  param for the hub (server reads it in `Program.cs` `OnMessageReceived`). ✅

The match spine works in practice (real battle logs exist under
`cardsGame/battle_phases/`).

## Mismatches found

### 1. Card catalog enum fields — FIXED (client)
Server `ServerCardDefinition` (`Game/MatchEngine.cs:170`, returned by
`GET /api/v1/cards`, `/cards/{id}`, `/cards/search`, `/cards/by-deck`) emits
`cardType`, `cardRarity`, `cardFaction` as **int**. The client DTO
(`CommonDtos.cs ServerCardDefinition`) had `cardType` as **string**, `rarity` as
**string** (wrong name), and **no** faction field. `JsonUtility` cannot place a
JSON number into a `string` field → those fields stayed empty →
`LocalSinglePlayerCoordinator.BuildRuntimeCard` parsed **every catalog card as
`Unit`/`Common`**, and faction was lost.

Fix applied (client, low blast radius — string fields were consumed in exactly one
place):
- `CommonDtos.cs`: `cardType` string→`int`, `rarity` string→`cardRarity int`,
  added `cardFaction int` (all default `-1`).
- `LocalSinglePlayerCoordinator.cs:322-323`: map int→enum with `-1` fallback.

> Needs a Unity compile + single-player playtest to confirm. The old `ParseEnum<T>`
> helper is now unused (harmless).

### 2. `MatchHistoryEntryDto.createdAt` type — REPORT
Server sends `createdAt` as `DateTimeOffset` (ISO-8601 string); client DTO
(`MatchHistoryApiClient.MatchHistoryEntryDto`) declares `long createdAt` →
`JsonUtility` leaves it `0`. Client also omits `rulesetId`/`rulesetName` (additive,
harmless). **Recommended:** change client `createdAt` to `string`. Same pattern to
verify on `DeckDto.createdAt`/`updatedAt` (`long`) vs whatever `DecksController`
list returns.

### 3. Deck-level DELETE endpoint missing — REPORT
Server only exposes `DELETE /api/v1/decks/{playerId}/{deckId}/cards/{entryId}`
(card-level). No `DELETE /api/v1/decks/{playerId}/{deckId}`. Client works around it.
Add the deck-level endpoint if deck deletion is wanted in UI.

### 4. Deck ownership/ID authority — REPORT (design)
`PUT /api/v1/decks` requires client-supplied `deckId` + `displayName`; client
generates `deck_{guid}`. `POST .../cards` validates the whole deck, blocking
incremental builds, so the client edits a local copy and PUTs when valid. Server
does not enforce card ownership on deck save — the client is only a convenience
layer (`GET /players/{userId}/cards/summary`). Move ownership + copy-count
validation server-side. Deck limits both sides: min 20, max 30, max 3 copies.

### 5. Battle-event semantics — REPORT (server behavior, from client findings)
- `attack_position_blocked` emitted for a melee (`unitType:0`) card in `Front`,
  which contradicts the rule that Front is the melee slot. Either allow it or add
  `reason`/`slot`/`unitType` diagnostics to the event.
- `skill_begin` is emitted for passive abilities (Taunt, Stun) that did not actively
  trigger; prefer a `passive_enabled` event or expose passives as card state only.
- Contract guarantee to keep: every battle event has a stable `eventId` and a
  strictly increasing per-match `sequence`; snapshots may repeat events but never
  reuse a sequence. Client dedupes by `sequence`.
- Animation event taxonomy the client relies on: `card_attack` = intent/windup
  (no health mutation); `card_damage` / `card_counterattack` = actual mutation;
  `death` = removal. Consume `battleEvents` in ascending `sequence`.
