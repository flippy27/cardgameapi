# CardDuel — Session Handoff (2026-06-12)

Exhaustive context to resume exactly where we stopped. Read this top-to-bottom
before touching anything. Caveman mode is active in the session (terse style);
that's a style hook only, ignore for correctness.

---

## 0. Two repos + the wire contract (CRITICAL)

| Role | Path | Git remote | Branch |
|---|---|---|---|
| **Server** (this repo) | `C:\Users\Flippy\Desktop\projects\CardDuel.ServerApi` | `flippy27/cardgameapi` | `master` |
| **Client** (Unity 6) | `C:\Users\Flippy\Desktop\projects\cardsGame` | `flippy27/cardgame` | `master` |

- `flippy27/cardgame` is the LIVE client. There is a DEAD trap repo `cardgame` (old) — ignore; the Unity project is `cardsGame`.
- **Wire contract** (do NOT break): JSON is **camelCase**; enums serialize as **integers** (NO `JsonStringEnumConverter` — adding one breaks the client). Client mirrors DTO field names by hand in `Assets/Runtime/Networking/ApiClients/*` (`JsonUtility` classes). Renaming/retyping a server DTO field = breaking change, update both repos.
- Battle events need stable `eventId` + strictly increasing per-match `sequence`; client dedupes + animates ascending.
- Server `CardType` enum (`Game/MatchEngine.cs:17`): `Unit=0, Utility=1, Equipment=2, Spell=3`.

---

## 1. Pi server (deployment target)

- **Host**: `192.168.1.87:5000` (HTTP). Runs in Docker on a Raspberry Pi.
- **SSH**: `ssh -i ~/.ssh/notes_pi flippy@192.168.1.87`
- **Deploy script**: `deploy-cardduel.ps1` (repo root) — packs working tree, scp to `~/cardduel`, `docker compose up -d --build`. Migrations run automatically on startup (`Program.cs`).
- **Containers**: `cardduel-postgres`, api, nginx, redis.
- **DB**: postgres user `postgres`, db `cardduel`.
- **Deployed state**: server engine INCLUDING playable Spell/Equipment/Utility (#12) is deployed (exit 0 earlier this session).

### Production DB mutation = needs explicit user authorization
The auto-mode classifier BLOCKS direct writes to the Pi DB over SSH. When a reseed/UPDATE is needed, give the user the exact command to run themselves via `! <cmd>` in the prompt, OR they paste the command to authorize. Copying a temp file via scp is allowed; the `docker exec ... psql` write is what gets blocked until authorized.

### Current Pi DB state (verified this session)
`abilities=25, card_abilities=201, decks=6, cards=200`. Distribution by `cardType`: 160 units (0), 10 utility (1), 15 equipment (2), 15 spell (3).

### Seeding
- Canonical generator: `tools/generate_seed.py` → writes `seed-data.sql` (FULL, destructive: now clears ALL FK-dependent tables in correct order — player_cards, ratings, matches, etc. — so a full re-apply works on a populated DB). Use for FRESH deploys.
- `seed-incremental.sql` — ADD-ONLY (every INSERT `ON CONFLICT (id) DO NOTHING`): abilities 17-25, effects 12-21, the 40 non-unit `card_abilities` (ids `ca-nu-1..40`), decks `deck-5`/`deck-6` + their `deck_cards`. This was generated + APPLIED to the Pi this session (the new spell/equip/util skills + the 30-card test decks now exist). Re-runnable safely.
- Re-generate incremental: regenerate `seed-data.sql` then run the extractor (the Python in the session that splits on `"),\n"`, re-adds `)` to all-but-last row, filters by `ability-1[7-9]|2[0-5]` / `effect-1[2-9]|2[01]` / `deck-5`/`deck-6`, renames `ca-N`→`ca-nu-N`). If you regenerate, double-check paren balance (`( == )` count).
- Apply incremental to Pi (user-authorized):
  ```
  scp -i ~/.ssh/notes_pi seed-incremental.sql flippy@192.168.1.87:/tmp/seed-incremental.sql
  ssh -i ~/.ssh/notes_pi flippy@192.168.1.87 'docker exec -i cardduel-postgres psql -U postgres -d cardduel -v ON_ERROR_STOP=1 < /tmp/seed-incremental.sql'
  ```

### Decks of note
- `deck-5` / `deck-6` = "Test All Types": 30 cards = 15 units + 5 spell + 5 equipment + 5 utility. Built specifically to exercise the non-unit target picker. They belong to seed users (user-1 / user-2 → emails `playerone@flippy.com` / `playertwo@flippy.com`, pass `123456`). The SP AI account is `playertwo@flippy.com`.

---

## 2. What this session delivered

### Server (`CardDuel.ServerApi`) — 5 unpushed commits on `master`
- `7bcc741` 200-card production catalog + 5 new combat abilities (cleave/execute/reflection/last_stand + GetAbilityAmount in `Game/MatchEngine.cs`).
- `d585614` default ruleset = full 20 mana.
- `64e47f4` HTTP match actions broadcast snapshot to SignalR (`Controllers/MatchesController.cs` injects `IHubContext<MatchHub>`, async + `BroadcastMatchAsync`).
- `49f26a3` **playable Spell/Equipment/Utility** (server-authoritative): `MatchEngine.PlayCard` branches by `CardType`; `PlayNonUnitCard`, `ApplyEquipment`, `TickEquipmentModifiers`, `RuntimeStatModifier`. `Contracts/ApiDtos.cs PlayCardRequest` gained `string? TargetRuntimeId = null`. `GameActionException` adds TargetRequired/InvalidTarget. Plumbed through `Hubs/MatchHub.cs`, `Services/InMemoryServices.cs`.
- `02e168b` test deck.
- `dec2178` seed generator FK-safe deletes + `seed-incremental.sql`.

### Client (`cardsGame`) — unpushed commits on `master` (newest first, key ones)
- `86f97a1` drop leftover .meta of deleted dead code.
- `c667ccf` **stat-number rotation fix** (see §4) — JUST MADE, NEEDS VERIFICATION.
- `3a62db2` **dead-code cleanup**: deleted 10 zero-ref files (verified C# + scene/asset GUID refs = 0): `MainGameSetup`, `InputSystemBootstrapper`, `ApiErrorHandler`, `CardInventoryService`, `CardInventoryApiClient`, `MatchCompletionService`, `ReplayPlayerService`, `ReplayApiClient`, `UI/DeckSelectionPanel`, `Data/SkillType`. NGO path left fully intact.
- `99b71b0` **Kenney code-skin** for deck-builder (see §6 for the manual step).
- `1ee947d` overlay compile fixes.
- `c5aacd5` **deck-selection menu before SP battle** (see §3).
- `8dc91e9` **spell/equip/util target picker** (client side of #12).
- Earlier batch (visual/drag/anim fixes): `722754a`, `e997239`, `9c79da6`, `3921b77`, `04688d2`, `67c2903`, `dcf02c9`, `b4990c3`, `2261ae8`, `ce4517f`, `8ca29e6`, etc.

---

## 3. Single-player flow (server-authoritative) + deck menu

SP no longer uses the client `DuelRuntime` sim. It opens a REAL private server match:
1. Entry button → `UI/MatchmakingPanelController.HandleLocalMatch()` (≈line 160). It now FIRST shows `SinglePlayerDeckSelectOverlay.Show(...)` (code-built, no prefab wiring). On confirm: stores the pick via `GamePlayStateManager.Instance.SetSelectedDeck(deckId, cardIds)`, then `GameModeManager.SetLocalMode()` + `SceneBootstrap.LoadMainGame()`.
2. MainGame loads → `Battle/Presentation/GameplayPresenter3D.Start()` (≈line 216, `IsLocalMode`) → `ServerSinglePlayerCoordinator.Instance.StartMatchAsync()`.
3. `SinglePlayer/ServerSinglePlayerCoordinator.StartMatchAsync()`: human deck now = `GamePlayStateManager.GetSelectedDeck().deckId` (fallback first/active deck). Human takes seat 0 via `MatchSignalRCoordinator`. AI logs into `playertwo@flippy.com`, joins seat 1, driven over HTTP. AI turn gated on `GameplayPresenter3D.IsPlayingBattlePresentation` so it doesn't act "first".

New file: `Assets/Runtime/UI/SinglePlayerDeckSelectOverlay.cs` — programmatic full-screen Canvas, lists `DeckManagementService.GetPlayerDecksAsync()`, defaults to active deck, Battle/Back buttons. (Note: the older `UI/DeckSelectionScreen.cs` still exists, half-wired; we did NOT use it — the new overlay supersedes it for SP. `DeckSelectionScreen.SelectedDeckId` is still read by the matchmaking path.)

---

## 4. OUTSTANDING — verify after recompile (the reason we stopped)

User reported (still UNVERIFIED post-fix):
1. **"Cost number corrido on rotated hand cards"** + **"drag ghost has a different frame / se cambia la carta / won't let me place / acts like it became a non-unit card"**.

### Fix already applied for the VISUAL half (`c667ccf`) — NEEDS USER VERIFICATION
Root cause: `Card3DView.SetStatsOverlayRotation()` was counter-rotating the StatsOverlay to keep numbers axis-aligned, but the baked frame SOCKETS tilt with the card mesh → numbers slid off their corners on fanned hand cards (the "corrido" cost) and on the velocity-tilted drag ghost (looked like the card "changed"). Fix: overlay now INHERITS the card rotation (`Quaternion.identity` passed at all 3 call sites) so each number stays glued to its socket (tilts naturally with the small fan angle).
- Patched: `Battle/Presentation/Hand3DManager.cs:254` and `:272`; `UI/DragGhost3D.cs:84`. (`DragGhost3D.cs:61` already passed identity.)

### STILL OPEN: "won't let me place / treated as non-unit"
Investigated; data path appears CORRECT, so this needs a runtime repro:
- Classifier: `Battle/Presentation/DragHandler3D.cs:335 DraggedCardIsNonUnit()` → reads `_draggedCard.CardData.cardId`, looks up `Networking.GameService.Instance.CardCatalog.TryGetCard(cardId, out def)`, returns `def.cardType != 0`.
- **Gotcha found**: `Networking/ApiClients/CommonDtos.cs:137` — `public int cardType = -1;` (default **-1**, not 0). So if the catalog lookup FAILS or the field isn't populated, `-1 != 0` would be TRUE → card wrongly classified non-unit. BUT: the Pi `/api/v1/cards` response WAS verified to include `cardType` with correct values (units=0), and the hand `cardId` matches the catalog id (it successfully resolves mana/unitType in `SnapshotConverter`). So in theory units classify correctly.
- **Hypothesis**: the drifted ghost frame made the user *perceive* non-unit; the rotation fix may resolve the whole report. If NOT — and a confirmed UNIT still refuses to place — the next step is:
  - Add a `Debug.Log` in `DraggedCardIsNonUnit()` printing `cardId`, `catalog.IsLoaded`, `TryGetCard` result, and `def?.cardType`, then repro in SP and read the console.
  - Also consider hardening the classifier to `def.cardType > 0` (treat unknown/-1 AND 0 as Unit) as a defensive guard — but ONLY after confirming the catalog actually loads in SP, otherwise spells would stop triggering the picker.
- Secondary suspect (low confidence): `DragHandler3D.CreateDragGhostFromSourceCard()` (≈line 916) re-`Initialize`s the cloned ghost (redundant after `Instantiate`); could rebuild the composite differently. Only pursue if the frame is still wrong after `c667ccf`.

### Ask the user, after they recompile:
1. Is the cost number now glued to the socket on tilted/fanned cards?
2. Does the drag ghost look like the correct card?
3. Does placing a UNIT on a board slot work? (If no → capture console logs / which card / which deck.)

---

## 5. The "unverified batch" (visual/drag/gameplay fixes awaiting recompile + screenshot)

All committed; user has been recompiling incrementally. Confirm end-to-end:
- Board card stat numbers visible; layout: **cost top-LEFT**, **armor just above health**, **attack bottom-left**, **hp bottom-right**; numbers white+bold+black-outline (TMP `ShaderUtilities` outline).
- Skill icons: dynamic socket circles drawn by code (frames stay plain); hand icons big, board icons small floating above.
- Drag: grab from CENTER (pivot offset), no oscillation at slot boundaries (nearest-slot + hysteresis, NOT physics raycast), no ghost flash, no go-and-return displacement bounce, no hand-card flash on play, cards already in play NOT draggable.
- Non-unit play: color outline target picker (green=friendly, red=enemy) — currently a TINT stand-in (`DragHandler3D.SetTargetCard`), a real outline shader is a TODO.
- Attacks paced slower; enemy doesn't attack "first"; faction projectiles + melee/magic impact VFX.

Mechanics note: armor is a number shown above health; it's consumed before health (already server-side).

---

## 6. #10 Kenney UI skin — REMAINING MANUAL STEP (one-time, editor)

Code skin is committed + null-safe (UI renders unskinned if sprites absent — non-blocking). To actually show the Kenney art:
- Raw pack lives at `Assets/Art/KenneyUI/` (currently **untracked in git** — local only; not pushed). `WIRING-PLAN.md` is in that folder (committed) with full details (section 6).
- `KenneyUiSkin.cs` (`Assets/Runtime/UI/DeckBuilding/`) loads ~9 sprites via `Resources.Load<Sprite>` trying roots `Art/KenneyUI/` then `KenneyUI/`.
- **Manual step**: copy these 9 PNGs into `Assets/Resources/Art/KenneyUI/` preserving sub-paths, import each as **Sprite (2D and UI)**, set 9-slice borders on the panel/button/input ones:
  - `RPGExpansion/PNG/panel_brown`, `RPGExpansion/PNG/panelInset_beige`
  - `PNG/Blue/Default/button_rectangle_depth_gloss`
  - `PNG/Grey/Default/button_rectangle_depth_flat`
  - `PNG/Grey/Default/button_square_depth_flat`
  - `PNG/Extra/Default/input_rectangle`
  - (+ the pressed/highlight variants the skin references — see `KenneyUiSkin.cs` for exact Resources paths)
- Decide whether to commit the raw Kenney pack (large binaries) so other machines/sessions have it. Currently NOT committed.

---

## 7. #11 cleanup — reorg plan (NOT executed) + NGO note

Deletions done (see `3a62db2`). A folder REORG was proposed but deliberately NOT executed (moving files churns `.meta`/GUIDs; risky mid-test). Proposal summary: split the overloaded flat `Networking/` into `Transport/` + `Services/` + `Dto/` (keep `ApiClients/`); split `UI/` into `Match/` + `DeckBuilding/` + `Debug/`. Execute via Unity Project window or `git mv` (keep `.meta` with file, never hand-edit GUIDs), in one batch when no test is in flight.

**NGO retirement (separate future task)**: the Unity-Netcode multiplayer path is still LIVE and was left intact: `DuelRuntime.cs`, `CardDuelNetworkCoordinator.cs`, `CardDuelNetworkPlayer.cs`, `NetworkBootstrap.cs`, `MpsGameSessionService.cs`, `MultiplayerAutoLogin.cs` (+ `Unity.Netcode.*`/`Unity.Multiplayer.*` packages). `MultiplayerAutoLogin` + `CardDuelNetworkPlayer` look dead (0 C# refs) but are live via `[RuntimeInitializeOnLoadMethod]` / prefab GUID. The live MP flow still depends on `DuelRuntime`, so it CANNOT be deleted until NGO MP is formally retired. SP already moved off `DuelRuntime` (uses the server path).

---

## 8. Untracked clutter (local only, not pushed) — user may gitignore/clean
- `Assets/Art/KenneyUI/` (raw temp art pack)
- `Assets/_Recovery/` (old recovered .unity scenes)
- `battle_phases/` (runtime battle-log .txt dumps — output artifacts)

---

## 9. Build / run / test

Server:
```
./start-dev.sh                 # docker postgres+redis + migrate + run
dotnet watch run               # or ./run-api.sh
dotnet test
dotnet ef migrations add X
docker-compose up -d           # full stack behind :80
```
API `http://localhost:5000` · Swagger `/swagger` · Health `/api/v1/health`.
Migrations auto-run on startup. Seeding disabled by default (apply `seed-data.sql` or run seeders).

Client: open `cardsGame` in Unity 6, Play. Client points at the Pi (`ConfigManager.GetApiBaseUrl()`). NOTE: a unit test was found pointing at the Pi IP and reverted to localhost (tests must be hermetic) — keep tests on localhost.

---

## 10. Task ledger
- #5 Migrate SP to server-authoritative — **DONE** (+ deck menu).
- #6 board double-frame — DONE.
- #7 preview transparency — DONE.
- #8 drag duplicate/ghost — DONE.
- #9 board placement anim — DONE.
- #10 deck-builder UI + Kenney — **DONE in code**; manual sprite import remains (§6).
- #11 client cleanup + reorg — **deletions DONE**; reorg = written plan only (§7).
- #12 playable Spell/Equipment/Utility + seed — **DONE** (server deployed, Pi seeded, client picker).
- #13 deck-selection menu before SP — **DONE**.

## 11. IMMEDIATE NEXT STEP on resume
1. Get the user's recompile/screenshot result on §4 (cost glued? ghost correct? unit places?).
2. If a confirmed UNIT still won't place → add the `Debug.Log` to `DragHandler3D.cs:335 DraggedCardIsNonUnit()` and repro in SP.
3. Then real outline shader for the target picker (replace the tint stand-in).

## 12. Memory files
`C:\Users\Flippy\.claude\projects\C--Users-Flippy-Desktop-projects-CardDuel-ServerApi\memory\`: `cardduel-project-pairing.md`, `pi-server-deployment.md`, `card-catalog-seed.md` (+ `MEMORY.md` index).
