---
name: ef-migration
description: Create and apply an EF Core migration in CardDuel.ServerApi (PostgreSQL/Npgsql). Use when changing the EF model / AppDbContext / entities, or when the schema needs to evolve.
---

# EF Core migration

Migrations live in `Migrations/` and run automatically on app startup
(`Program.cs` calls `db.Database.Migrate()`).

## Steps

1. Edit the entity/`AppDbContext` (`Infrastructure/`).
2. Create the migration:
   ```bash
   dotnet ef migrations add <DescriptiveName>
   ```
3. Review the generated `Up`/`Down` — confirm it does only what you intend
   (no accidental drops). Postgres column types come from Npgsql conventions.
4. Apply locally:
   ```bash
   dotnet ef database update
   ```
   (or just run the app — startup migrates).

## Guardrails

- Always keep a working `Down`. Don't hand-delete migration files that have shipped.
- Data-shape changes that surface in API responses can ripple to the Unity client —
  if the change affects a DTO, see CLAUDE.md "Contract" and the `contract-guardian`
  agent.
- Seeding is manual and separate (`AuthoringSeeder`/`CardCatalogSeeder`/
  `GameRulesetSeeder` or `seed-data.sql`) — don't seed inside a migration.
