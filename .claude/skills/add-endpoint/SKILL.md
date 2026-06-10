---
name: add-endpoint
description: Scaffold a new REST endpoint in CardDuel.ServerApi following repo conventions (controller action, record DTOs in Contracts/, FluentValidation validator, service method). Use when adding or extending an api/v1 endpoint.
---

# Add a REST endpoint

Follow the existing patterns exactly — the Unity client mirrors DTOs by hand.

## Steps

1. **DTOs** — add request/response as immutable `record`s in the right
   `Contracts/*.cs` file (`ApiDtos.cs` for match/deck/matchmaking, `CardDtos.cs`
   for cards/abilities, `InventoryDtos.cs` for items/crafting). camelCase is
   automatic; enums are emitted as `int`. Use `[Required]`/`[Range]` annotations.
2. **Validator** (if non-trivial input) — add a `*Validator : AbstractValidator<T>`
   and ensure its assembly is covered by `AddValidatorsFromAssemblyContaining<>` in
   `Program.cs`.
3. **Service** — put business logic in the relevant `Services/I*Service`
   implementation, not the controller. Match state lives in the singleton
   `IMatchService`.
4. **Controller** — add the action under the existing `[Route("api/v1/...")]`
   controller. Keep `[Authorize]` unless it's intentionally public. Return DTOs (not
   entities). On error, return `ErrorResponse` via the established helpers.
5. **Swagger** — it's auto-discovered; add request examples if the pattern in the
   controller uses them.
6. **Test** — add an xunit test under `Tests/`.

## Guardrails

- Don't return EF entities directly — always project to a DTO record.
- Don't change global JSON config or enum definitions (breaks the client — see
  CLAUDE.md "Contract" section and run the `contract-guardian` agent if you touch
  a client-facing DTO).
- New migration needed? Use the `ef-migration` skill.
