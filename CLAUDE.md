# CLAUDE.md — CardDuel.ServerApi

Server-authoritative backend for **CardDuel**, a turn-based card-battle game.
The Unity client lives in `../cardsGame` (repo `flippy27/cardgame`). This repo is
`flippy27/cardgameapi`.

## Stack

- **.NET 10** ASP.NET Core (`Microsoft.NET.Sdk.Web`), C# nullable + implicit usings.
- **PostgreSQL** via EF Core 8 (Npgsql). Migrations in `Migrations/`.
- **SignalR** for live match play (`/hubs/match`), optional Redis backplane.
- **JWT** bearer auth (`BCrypt` password hashing, SHA256 legacy fallback).
- **Serilog** structured logging → console + `logs/`.
- **Swagger** at `/swagger` with a custom auth helper panel.
- **xunit** tests (same csproj, under `Tests/`).

## Run / build

```bash
# full local dev (Docker postgres+redis, migrate, then run)
./start-dev.sh
dotnet watch run            # or: ./run-api.sh  (sets ASPNETCORE_ENVIRONMENT=Development)

# full stack (nginx + api + postgres + redis behind :80)
docker-compose up -d

dotnet test                 # run tests
dotnet ef migrations add X  # new migration
```

API: `http://localhost:5000` · Swagger: `/swagger` · Health: `/api/v1/health` ·
Metrics: `/metrics` (Prometheus, no auth).

> Migrations run automatically on startup (`Program.cs`). **Seeding is disabled by
> default** — run `AuthoringSeeder` / `CardCatalogSeeder` / `GameRulesetSeeder`
> manually, or apply `seed-data.sql`.

## Layout

- `Program.cs` — DI, auth, SignalR, middleware pipeline, startup migrate.
- `Controllers/` — REST under `api/v1/*` (auth, matches, matchmaking, decks, cards,
  players/cards, inventory, crafting, game-rulesets, users, replays, admin…).
- `Hubs/MatchHub.cs` — SignalR: `ConnectToMatch`/`SetReady`/`PlayCard`/`EndTurn`/
  `DestroyCard`/`Forfeit` → return `MatchSnapshot`; broadcasts `"MatchSnapshot"` to
  the `{matchId}` group.
- `Game/MatchEngine.cs` — **the authoritative game engine**: all enums
  (`CardType`, `CardRarity`, `CardFaction`, `EffectKind`, `TriggerKind`,
  `TargetSelectorKind`, `BoardSlot`, `MatchPhase`…), runtime state, snapshot records.
  Combat is **automatic** (no manual attack), no mulligan.
- `Game/GameRules.cs` — ruleset model (health/mana/draw/seat overrides).
- `Contracts/` — request/response DTOs (records): `ApiDtos.cs`, `CardDtos.cs`,
  `InventoryDtos.cs`, `ErrorResponse.cs`.
- `Services/` — `IMatchService` (in-memory, **singleton** — match state is in RAM),
  catalog/deck/ruleset/rating(ELO)/reconnection/replay/crafting/inventory services.
- `Infrastructure/` — DbContext, middleware (correlation id, metrics, rate limit,
  global exception, request logging, audit), Swagger filters.

## Contract with the Unity client — READ BEFORE CHANGING ANY DTO

- **JSON is camelCase** (System.Text.Json ASP.NET defaults; no custom JsonOptions).
- **Enums serialize as integers** (no `JsonStringEnumConverter`). The client reads
  them as `int`. If you add a global string-enum converter, the client breaks.
- DTO field names/types are mirrored by hand in the client's `JsonUtility` classes
  (`Assets/Runtime/Networking/ApiClients/*`). Renaming or retyping a field is a
  breaking change — update both repos together.
- Battle events: every event needs a stable `eventId` + strictly increasing
  per-match `sequence`; the client dedupes and animates in ascending `sequence`.
- Canonical integration spec: **`GAME_INTEGRATION_GUIDE.md`** (includes folded
  appendices: player-ownership/inventory/crafting, card-destruction, skill-event
  contracts). Latest cross-repo review + known mismatches: **`CONTRACT_REVIEW.md`**.

## Conventions

- DTOs are immutable `record`s in `Contracts/`. Validation via FluentValidation
  (`*Validator.cs`) + data annotations.
- Errors return `ErrorResponse` with `code`/`message`/`correlationId`; hub errors
  throw `HubException` carrying `GameActionErrorDto`.
- Don't commit secrets — `JWT_SIGNING_KEY` comes from env/config.
- Match state is per-process singleton; horizontal scaling needs the Redis backplane
  (`SignalR:UseRedisBackplane`).
