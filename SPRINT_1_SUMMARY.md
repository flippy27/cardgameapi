# Sprint 1 Summary: Move Decks, Matches, Ratings to Database

## ✅ Completed

### 1. Secrets Management
- JWT key removed from appsettings.json - now reads from `JWT_SIGNING_KEY` env var
- DB password removed from config - now uses env var
- `.env.example` created with required env vars

### 2. CORS Security
- Wildcard `http://127.0.0.1:*` → Explicit ports (3000, 5173)
- Restrictive whitelist for localhost

### 3. Database Persistence

#### Deck Repository
- Created `DbDeckRepository` - replaces InMemoryDeckRepository
- Full EF Core integration with AppDbContext
- Upsert/Get operations backed by PostgreSQL
- Registered as scoped service in Program.cs

#### Rating Service
- Created `DbRatingService` - handles rating persistence
- Integrated with EloRatingService for calculations
- Stores PlayerRating records with Win/Loss counts
- Auto-creates rating records on first match

#### Match Completion
- Improved `CompleteMatch` in InMemoryMatchService
- Creates MatchRecord in DB if missing
- Persists ratings before/after
- Full transaction support with rollback

### 4. Public Properties on MatchEngine
- Added `RoomCode`, `Mode`, `MatchId` properties
- Enables better service integration

### 5. Testing (9 tests, 100% pass)
- `DeckRepositoryTests` - 3 tests for CRUD ops
- `RatingServiceTests` - 3 tests for Elo calculations
- `MatchEngineTests` - 3 tests for game state
- Using xunit + EF Core InMemory for isolation

### 6. NuGet Packages Added
- xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk
- Microsoft.EntityFrameworkCore.InMemory (for testing)

## Still Using In-Memory
- Card catalog (fine for now - 18 cards hardcoded)
- Active match rooms (needed for real-time perf)
- Tournament store
- These will move to DB in Sprint 3+

## Next: Sprint 2
1. Move card catalog to DB table
2. Add PlayCard/EndTurn validations
3. Improve disconnect/reconnect logic
4. Add audit logging middleware
5. Structured logging (correlation IDs)

## Compiled ✓
- `dotnet build` → Success
- `dotnet test` → 9/9 passing
- No errors, minor warnings (resolvable)

## Env Vars Required to Run
```bash
export JWT_SIGNING_KEY="your-secret-key-at-least-32-chars"
# optionally:
export ASPNETCORE_ENVIRONMENT=Development
```

Then: `dotnet run`
