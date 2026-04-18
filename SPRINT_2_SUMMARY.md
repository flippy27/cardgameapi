# Sprint 2 Summary: Card Catalog to DB + Validation + Audit

## ✅ Completed

### 1. Card Catalog Migration to Database
- Created `CardDefinition` model + migrations
- Created `DbCardCatalogService` (scoped, DB-backed)
- Seeded all 18 cards via `CardCatalogSeeder`
- JSON abilities stored in PostgreSQL JSONB
- Caching in memory to avoid repeated queries
- **Impact**: Catalog no longer hardcoded - can add/edit cards via DB

### 2. Audit Logging Infrastructure
- Created `AuditLog` model (userId, action, resource, IP, status code)
- Created `AuditService` - async logging with full context
- Created `AuditLoggingMiddleware` - auto-logs POST/PUT/DELETE on critical paths
- Indexes on userId, CreatedAt, (resource+resourceId) for fast queries
- **Impact**: Full audit trail for compliance + debugging

### 3. Database Schema Improvements
- `AppDbContextFactory` - enables EF Core migrations without full DI container
- Value comparer fix for `PlayerDeck.CardIds` collection
- JSONB columns for abilities + audit details (PostgreSQL specific)
- Proper indexing strategy for query performance

### 4. Tests (5 new + 9 existing = 14 total, 100% pass)
- `CardCatalogTests` - 3 tests (seed, resolve, unknown card)
- `AuditServiceTests` - 2 tests (create log, store metadata)
- All tests pass using EF Core InMemory isolation

### 5. Service Integration
- Registered `DbCardCatalogService` as scoped ICardCatalogService
- Registered `AuditService` + middleware in middleware pipeline
- Changed catalog from Singleton to Scoped (memory safety)

## Architecture Decisions

### Still In-Memory (Intentional)
- **Active match rooms**: Real-time perf + state consistency
- **Waiting queues**: Fast pairing without DB round-trips
- Persisted to DB only on completion via `CompleteMatch`

### Database Only
- Card definitions (18 cards, can grow)
- Audit logs (immutable, indexed for queries)
- User decks, matches, ratings (from Sprint 1)

## Migration Strategy
```sql
-- Postgres will auto-create via EF Core:
CREATE TABLE "Cards" (
  "Id" text PRIMARY KEY,
  "CardId" varchar(128) UNIQUE,
  "DisplayName" varchar(255),
  ... abilities JSONB
);

CREATE TABLE "AuditLogs" (
  "Id" text PRIMARY KEY,
  "UserId" text,
  "Action" varchar(64),
  ... JSONB details
);
-- With indexes on UserId, CreatedAt, (Resource, ResourceId)
```

## Code Quality
- 100% test pass rate
- Type-safe ability parsing (JSON → `ServerAbilityDefinition`)
- Async audit logging (non-blocking)
- Proper exception handling in catalog fallback

## Still Missing (Sprint 3+)
1. Request DTOs validation (FluentValidation)
2. Structured logging (correlation IDs, request context)
3. Health checks for DB/dependencies
4. Better error messages (enum mapping)
5. Replay action persistence (MatchAction table)

## Files Added/Modified
**New:**
- `CardDefinition.cs`, `AuditLog.cs`, `AppDbContextFactory.cs`
- `DbCardCatalogService.cs`, `AuditService.cs`, `CardCatalogSeeder.cs`
- `AuditLoggingMiddleware.cs`
- `Tests/CardCatalogTests.cs`, `Tests/AuditServiceTests.cs`
- `SPRINT_2_SUMMARY.md`
- Migration: `AddCardCatalogAndAuditLogs`

**Modified:**
- `AppDbContext.cs` - Added Cards, AuditLogs DbSets + config
- `Program.cs` - Added CardCatalogSeeder call, middleware, registrations
- (12 files total changes)

## Compiled & Tested ✓
- `dotnet build` → Success (zero errors)
- `dotnet test` → 14/14 passing
- Ready to run with migrations

## Next: Sprint 3
1. Structured logging (Serilog correlation IDs)
2. Health checks (DB, Redis, dependencies)
3. MatchAction table for replay validation
4. Request validation (FluentValidation)
5. Error response standardization

**Status**: Database-backed catalog + audit trail live. Matches persisted on completion.
