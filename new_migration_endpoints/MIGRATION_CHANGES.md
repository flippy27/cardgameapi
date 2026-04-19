# Sprint 7: Database & API Changes - Card System Expansion

## Date
2026-04-19

## Overview
Complete overhaul of card system to support full CRUD operations with abilities and effects instead of hardcoded JSON. Database now has relational models for CardDefinition, AbilityDefinition, and EffectDefinition.

---

## DATABASE CHANGES

### New Tables

#### `AbilityDefinition`
```sql
CREATE TABLE "AbilityDefinition" (
    "Id" text PRIMARY KEY,
    "AbilityId" text NOT NULL,
    "DisplayName" varchar(255) NOT NULL,
    "Description" varchar(512),
    "TriggerKind" integer NOT NULL,
    "TargetSelectorKind" integer NOT NULL,
    "CardDefinitionId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    FOREIGN KEY ("CardDefinitionId") REFERENCES "CardDefinition"("Id") ON DELETE CASCADE,
    UNIQUE ("CardDefinitionId", "AbilityId")
);
CREATE INDEX ON "AbilityDefinition" ("CardDefinitionId");
```

#### `EffectDefinition`
```sql
CREATE TABLE "EffectDefinition" (
    "Id" text PRIMARY KEY,
    "EffectKind" integer NOT NULL,
    "Amount" integer NOT NULL,
    "Sequence" integer NOT NULL,
    "AbilityDefinitionId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    FOREIGN KEY ("AbilityDefinitionId") REFERENCES "AbilityDefinition"("Id") ON DELETE CASCADE,
    UNIQUE ("AbilityDefinitionId", "Sequence")
);
CREATE INDEX ON "EffectDefinition" ("AbilityDefinitionId");
```

### Modified Tables

#### `CardDefinition` (ADD COLUMNS)
```sql
-- New columns
ALTER TABLE "CardDefinition" ADD COLUMN "Description" varchar(1024);
ALTER TABLE "CardDefinition" ADD COLUMN "CardType" integer NOT NULL DEFAULT 0;
ALTER TABLE "CardDefinition" ADD COLUMN "CardRarity" integer NOT NULL DEFAULT 0;
ALTER TABLE "CardDefinition" ADD COLUMN "CardFaction" integer NOT NULL DEFAULT 0;
ALTER TABLE "CardDefinition" ADD COLUMN "UnitType" integer;
ALTER TABLE "CardDefinition" ADD COLUMN "TurnsUntilCanAttack" integer NOT NULL DEFAULT 1;
ALTER TABLE "CardDefinition" ADD COLUMN "IsLimited" boolean NOT NULL DEFAULT false;

-- Legacy column (keep for backward compatibility)
-- "AbilitiesJson" stays as-is, marked as [Obsolete] in code
```

---

## C# MODEL CHANGES

### New Files

#### `Game/CardEnums.cs` (NEW)
**Location**: `/Infrastructure/Models/`

Shared enums:
- `CardType` (0: Unit, 1: Utility, 2: Equipment, 3: Spell)
- `CardRarity` (0: Common, 1: Rare, 2: Epic, 3: Legendary)
- `CardFaction` (0: Ember, 1: Tidal, 2: Grove, 3: Alloy, 4: Void)
- `UnitType` (0: Melee, 1: Ranged, 2: Magic)
- `EffectKind` (17 types: 0-17)

#### `Infrastructure/Models/AbilityDefinition.cs` (NEW)
- `Id`: PK (Guid string)
- `AbilityId`: Unique identifier (snake_case)
- `DisplayName`: User-facing name
- `Description`: Ability explanation
- `TriggerKind`: int (0-3)
- `TargetSelectorKind`: int (0-4)
- `CardDefinitionId`: FK to Card
- `Effects`: ICollection<EffectDefinition>

#### `Infrastructure/Models/EffectDefinition.cs` (NEW)
- `Id`: PK (Guid string)
- `EffectKind`: int (0-17)
- `Amount`: int (1-100)
- `Sequence`: int (order within ability)
- `AbilityDefinitionId`: FK to Ability

### Modified Files

#### `Infrastructure/Models/CardDefinition.cs`
**Changes**:
- Added: `Description` (string)
- Added: `CardType` (int)
- Added: `CardRarity` (int)
- Added: `CardFaction` (int)
- Added: `UnitType` (int?)
- Added: `TurnsUntilCanAttack` (int, default 1)
- Added: `IsLimited` (bool)
- Added: `Abilities` navigation property (ICollection<AbilityDefinition>)
- Marked `AbilitiesJson` as [Obsolete]

#### `Infrastructure/AppDbContext.cs`
**Changes**:
- Added: `DbSet<AbilityDefinition> Abilities`
- Added: `DbSet<EffectDefinition> Effects`
- Added: Fluent API config for new tables with relationships and constraints

---

## API ENDPOINTS

### New Contracts (DTOs)

#### `Contracts/CardDtos.cs` (NEW)
- `CardDefinitionDto`: Full card with abilities expanded
- `CreateCardRequest`: Admin create
- `UpdateCardRequest`: Admin update (partial)
- `AbilityDto`: Ability with effects
- `CreateAbilityRequest`: Create ability with effects
- `UpdateAbilityRequest`: Update ability fields
- `EffectDto`: Single effect
- `CreateEffectRequest`: Create effect
- `UpdateEffectRequest`: Update effect
- Response DTOs: `CardOperationResponse`, `AbilityOperationResponse`, `EffectOperationResponse`

#### `Contracts/CardValidators.cs` (NEW)
- `CreateCardRequestValidator`
- `UpdateCardRequestValidator`
- `CreateAbilityRequestValidator`
- `CreateEffectRequestValidator`

### New Service

#### `Services/CardManagementService.cs` (NEW)
**Interface**: `ICardManagementService`

**Methods**:
- `CreateCardAsync(request)` → CardDefinitionDto
- `UpdateCardAsync(cardId, request)` → CardDefinitionDto
- `DeleteCardAsync(cardId)` → bool
- `AddAbilityAsync(cardId, request)` → AbilityDto
- `UpdateAbilityAsync(cardId, abilityId, request)` → AbilityDto
- `DeleteAbilityAsync(cardId, abilityId)` → bool
- `AddEffectAsync(cardId, abilityId, request)` → EffectDto
- `UpdateEffectAsync(cardId, abilityId, effectId, request)` → EffectDto
- `DeleteEffectAsync(cardId, abilityId, effectId)` → bool
- `GetCardWithAbilitiesAsync(cardId)` → CardDefinitionDto?

### New Endpoints (CardsController.cs)

#### Admin Endpoints (Require Authorization)

**POST** `/api/v1/cards`
- Create new card
- Body: `CreateCardRequest`
- Response: `CardOperationResponse`
- Returns: 201 Created

**PUT** `/api/v1/cards/{cardId}`
- Update card fields
- Body: `UpdateCardRequest`
- Response: `CardDefinitionDto`
- Returns: 200 OK

**DELETE** `/api/v1/cards/{cardId}`
- Delete card
- Returns: 204 NoContent or 404

**POST** `/api/v1/cards/{cardId}/abilities`
- Add ability to card
- Body: `CreateAbilityRequest`
- Response: `AbilityOperationResponse`
- Returns: 201 Created

**PUT** `/api/v1/cards/{cardId}/abilities/{abilityId}`
- Update ability
- Body: `UpdateAbilityRequest`
- Response: `AbilityDto`
- Returns: 200 OK

**DELETE** `/api/v1/cards/{cardId}/abilities/{abilityId}`
- Delete ability
- Returns: 204 NoContent or 404

**POST** `/api/v1/cards/{cardId}/abilities/{abilityId}/effects`
- Add effect to ability
- Body: `CreateEffectRequest`
- Response: `EffectOperationResponse`
- Returns: 201 Created

**PUT** `/api/v1/cards/{cardId}/abilities/{abilityId}/effects/{effectId}`
- Update effect
- Body: `UpdateEffectRequest`
- Response: `EffectDto`
- Returns: 200 OK

**DELETE** `/api/v1/cards/{cardId}/abilities/{abilityId}/effects/{effectId}`
- Delete effect
- Returns: 204 NoContent or 404

#### Public Endpoints (Expanded - 10 New)

**GET** `/api/v1/cards/{cardId}`
- Now returns: `CardDefinitionDto` (with abilities expanded)
- Includes: description, cardType, rarity, faction, unitType

**GET** `/api/v1/cards/by-faction?faction={0-4}`
- Filter cards by faction
- Returns: List of cards

**GET** `/api/v1/cards/by-type?cardType={0-3}`
- Filter cards by type (Unit/Utility/Equipment/Spell)
- Returns: List of cards

**GET** `/api/v1/cards/by-rarity?rarity={0-3}`
- Filter cards by rarity (Common/Rare/Epic/Legendary)
- Returns: List of cards

**GET** `/api/v1/cards/stats/by-faction`
- Stats grouped by faction (count, avg mana, attack, health, armor)
- Returns: Array of faction statistics

**GET** `/api/v1/cards/stats/by-type`
- Stats grouped by card type
- Returns: Array of type statistics

**GET** `/api/v1/cards/stats/by-rarity`
- Stats grouped by rarity
- Returns: Array of rarity statistics

**GET** `/api/v1/cards/effects`
- List all effect types (0-26 with names)
- Returns: Array of effect definitions

**GET** `/api/v1/cards/triggers`
- List all trigger types (0-3 with names)
- Returns: Array of trigger definitions

**GET** `/api/v1/cards/target-selectors`
- List all target selector types (0-4 with names)
- Returns: Array of selector definitions

**GET** `/api/v1/cards/skill-types`
- List all skill types (0-4 with names)
- Returns: Array of skill type definitions

**GET** `/api/v1/cards/skills`
- List all skills from all cards
- Returns: Array with skillId, displayName, cardId, triggerKind, targetSelectorKind, effectCount

---

## ENUMS MAPPING

### CardType (int)
| Value | Name | Description |
|-------|------|-------------|
| 0 | Unit | Creature card |
| 1 | Utility | Non-creature helper |
| 2 | Equipment | Equipment for units |
| 3 | Spell | One-time effect |

### CardRarity (int)
| Value | Name |
|-------|------|
| 0 | Common |
| 1 | Rare |
| 2 | Epic |
| 3 | Legendary |

### CardFaction (int)
| Value | Name |
|-------|------|
| 0 | Ember |
| 1 | Tidal |
| 2 | Grove |
| 3 | Alloy |
| 4 | Void |

### UnitType (int, nullable)
| Value | Name |
|-------|------|
| 0 | Melee |
| 1 | Ranged |
| 2 | Magic |
| null | N/A (non-unit card) |

### TriggerKind (int)
| Value | Name | Description |
|-------|------|-------------|
| 0 | OnPlay | When card enters board |
| 1 | OnTurnStart | At start of owner's turn |
| 2 | OnTurnEnd | At end of owner's turn |
| 3 | OnBattlePhase | During battle phase |

### TargetSelectorKind (int)
| Value | Name |
|-------|------|
| 0 | Self |
| 1 | FrontlineFirst |
| 2 | BacklineFirst |
| 3 | AllEnemies |
| 4 | LowestHealthAlly |

### SkillType (int, nullable in AbilityDefinition)
| Value | Name | Description |
|-------|------|-------------|
| 0 | Defensive | Armor, shield, evasion, reflection, dodge |
| 1 | Offensive | Poison, stun, leech, mana_burn, enrage |
| 2 | Equipable | Weapon/armor cards that grant abilities |
| 3 | Utility | Regenerate, charge, taunt |
| 4 | Modifier | Change attack behavior (cleave, range, etc) |

### EffectKind (int) - 27 Total Types
| Value | Name | Description |
|-------|------|-------------|
| 0 | Damage | Deal damage to target |
| 1 | Heal | Heal target |
| 2 | GainArmor | Add armor to target |
| 3 | BuffAttack | Increase attack |
| 4 | HitHero | Damage opponent hero |
| 5 | Stun | Disable target for turn |
| 6 | Poison | DoT damage |
| 7 | Leech | Damage + heal caster |
| 8 | Evasion | Dodge next hit |
| 9 | Shield | Block one hit |
| 10 | Reflection | Reflect damage |
| 11 | Dodge | Dodge random attacks |
| 12 | Enrage | Increase damage taken |
| 13 | ManaBurn | Drain mana |
| 14 | Regenerate | Heal each turn |
| 15 | Execute | Kill low health |
| 16 | DiagonalAttack | Hit diagonal positions |
| 17 | Fly | Bypass melee defense |
| 18 | Armor | Armor effect |
| 19 | Chain | Chain reaction |
| 20 | Charge | Charging attack |
| 21 | Cleave | AoE attack |
| 22 | LastStand | Defensive stance |
| 23 | MeleeRange | Melee range |
| 24 | Ricochet | Bounce damage |
| 25 | Taunt | Force target |
| 26 | Trample | Trample effect |

---

## MIGRATION SQL

Execute these in this order after deploying code:

```sql
-- Add columns to CardDefinition
ALTER TABLE "CardDefinition" ADD COLUMN "Description" varchar(1024);
ALTER TABLE "CardDefinition" ADD COLUMN "CardType" integer NOT NULL DEFAULT 0;
ALTER TABLE "CardDefinition" ADD COLUMN "CardRarity" integer NOT NULL DEFAULT 0;
ALTER TABLE "CardDefinition" ADD COLUMN "CardFaction" integer NOT NULL DEFAULT 0;
ALTER TABLE "CardDefinition" ADD COLUMN "UnitType" integer;
ALTER TABLE "CardDefinition" ADD COLUMN "TurnsUntilCanAttack" integer NOT NULL DEFAULT 1;
ALTER TABLE "CardDefinition" ADD COLUMN "IsLimited" boolean NOT NULL DEFAULT false;

-- Create AbilityDefinition table
CREATE TABLE "AbilityDefinition" (
    "Id" text PRIMARY KEY,
    "AbilityId" text NOT NULL,
    "DisplayName" varchar(255) NOT NULL,
    "Description" varchar(512),
    "TriggerKind" integer NOT NULL,
    "TargetSelectorKind" integer NOT NULL,
    "CardDefinitionId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    FOREIGN KEY ("CardDefinitionId") REFERENCES "CardDefinition"("Id") ON DELETE CASCADE,
    UNIQUE ("CardDefinitionId", "AbilityId")
);
CREATE INDEX "IX_AbilityDefinition_CardDefinitionId" ON "AbilityDefinition"("CardDefinitionId");

-- Create EffectDefinition table
CREATE TABLE "EffectDefinition" (
    "Id" text PRIMARY KEY,
    "EffectKind" integer NOT NULL,
    "Amount" integer NOT NULL,
    "Sequence" integer NOT NULL,
    "AbilityDefinitionId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    FOREIGN KEY ("AbilityDefinitionId") REFERENCES "AbilityDefinition"("Id") ON DELETE CASCADE,
    UNIQUE ("AbilityDefinitionId", "Sequence")
);
CREATE INDEX "IX_EffectDefinition_AbilityDefinitionId" ON "EffectDefinition"("AbilityDefinitionId");
```

---

## BACKWARD COMPATIBILITY

- Old `AbilitiesJson` column remains (marked [Obsolete] in code)
- Existing card data unaffected during migration
- SeederService updated to populate new tables
- Old JSON seeding still works but deprecated

---

## GAME CLIENT UPDATES REQUIRED

In `/Users/idhemax/proyects/_MINE/cardgame`, update:

1. **CardGameApiClient.cs**
   - `GetCardAsync()` now returns full abilities array (not JSON string)
   - Parse `Abilities` property (AbilityDto[])
   - Each ability has `Effects` array (EffectDto[])

2. **Data Models** (if consuming from API)
   - `CardDefinition` now has properties:
     - `CardType` (enum)
     - `CardRarity` (enum)
     - `CardFaction` (enum)
     - `UnitType` (enum?)
     - `Description`
     - `TurnsUntilCanAttack`
     - `IsLimited`
   - `Abilities` is now structured array, not JSON

3. **Card Registry**
   - Update to deserialize from new API format
   - Example:
     ```csharp
     var card = cardDto; // Direct mapping
     var ability = card.Abilities[0];
     var effect = ability.Effects[0]; // EffectKind, Amount
     ```

---

## TESTING NOTES

### New Test Endpoints

```bash
# Create card
curl -X POST http://localhost:5000/api/v1/cards \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "cardId": "test_card_1",
    "displayName": "Test Card",
    "description": "Test",
    "manaCost": 3,
    "attack": 2,
    "health": 3,
    "armor": 0,
    "cardType": 0,
    "cardRarity": 1,
    "cardFaction": 0,
    "unitType": 0,
    "allowedRow": 0,
    "defaultAttackSelector": 1,
    "turnsUntilCanAttack": 1,
    "isLimited": false
  }'

# Add ability to card
curl -X POST http://localhost:5000/api/v1/cards/test_card_1/abilities \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "abilityId": "test_ability",
    "displayName": "Test Ability",
    "description": "Test",
    "triggerKind": 0,
    "targetSelectorKind": 0,
    "effects": [
      {
        "effectKind": 0,
        "amount": 3,
        "sequence": 0
      }
    ]
  }'

# Get card with abilities
curl -X GET http://localhost:5000/api/v1/cards/test_card_1
```

---

## FILES CREATED

1. `/Game/CardEnums.cs` - Shared enums
2. `/Infrastructure/Models/AbilityDefinition.cs` - New model
3. `/Infrastructure/Models/EffectDefinition.cs` - New model
4. `/Contracts/CardDtos.cs` - New DTOs
5. `/Contracts/CardValidators.cs` - New validators
6. `/Services/CardManagementService.cs` - New service

## FILES MODIFIED

1. `/Infrastructure/Models/CardDefinition.cs` - Expanded with new fields + navigation
2. `/Infrastructure/AppDbContext.cs` - Added DbSets and Fluent API config
3. `/Controllers/CardsController.cs` - New admin endpoints (next step)
4. `/Program.cs` - Register service + validators (next step)

---

---

# Sprint 7: COMPLETE ✅ (WITH SKILLS)

**Total Files Created:** 6
**Total Files Modified:** 8  
**Total Endpoints Added:** 22 (10 admin + 12 public)
**Total Test Methods:** 11
**Effects Supported:** 27 (all from game)
**Skills System:** ✅ Full support with 5 skill types

---

## STATUS: FULL IMPLEMENTATION COMPLETE ✅ (+ SKILLS)

### Completed ✅

**Models & Database:**
- [x] Create AbilityDefinition model (FK to CardDefinition, +SkillType)
- [x] Create EffectDefinition model (FK to AbilityDefinition)
- [x] Expand CardDefinition model (+8 properties)
- [x] Update AppDbContext with new DbSets and Fluent API
- [x] Create EF Core migration (20260419170327_AddAbilitiesAndEffectsModels)

**Enums:**
- [x] Consolidate enums into MatchEngine.cs (CardType, CardRarity, CardFaction, UnitType, **SkillType**)
- [x] Expand EffectKind enum from 5 → 27 types (all Unity effects covered)
- [x] Update ServerCardDefinition and ServerAbilityDefinition records
- [x] SkillType enum (0-4): Defensive, Offensive, Equipable, Utility, Modifier

**API:**
- [x] Create CardDtos.cs (8 DTOs + responses)
- [x] Create CardValidators.cs (4 validators)
- [x] Create CardManagementService (11 CRUD methods)
- [x] Expand CardsController (10 admin + 10 public endpoints)
- [x] Register service & validators in Program.cs
- [x] Add Swagger tags to controller

**Testing:**
- [x] Create CardManagementServiceTests (11 test methods)
- [x] Fix ServerCardDefinition calls in InMemoryServices & MatchEngineTests

**Documentation:**
- [x] MIGRATION_CHANGES.md (complete technical guide)
- [x] README.md (game client integration guide)
- [x] example-create-card.json (testing reference)
- [x] example-create-ability.json (testing reference)

### Pending (for deployment)

- [ ] Run migration on database: `dotnet ef database update`
- [ ] Test all endpoints
- [ ] Update game client to consume new API format
- [ ] Add Swagger/OpenAPI documentation to endpoints
- [ ] Update seeder to populate AbilityDefinition/EffectDefinition (optional, backward compat maintained)

## NEXT STEPS FOR USER

1. **Migration runs automatically on API startup**
   - Program.cs already includes auto-migration code
   - Just restart docker-compose or API
   ```bash
   docker-compose restart cardduel-api
   # OR
   docker-compose down && docker-compose up --build
   ```

2. **Verify migration applied**
   ```bash
   docker exec cardduel-postgres psql -U postgres -d cardduel -c "\dt"
   # Should show: Abilities, Effects tables
   ```

3. **Test endpoints** (use provided curl examples in TESTING NOTES section)

3. **Update game client** (`/Users/idhemax/proyects/_MINE/cardgame`)
   - See GAME CLIENT UPDATES section for required changes

4. **Update seeder** (optional)
   - Current seeder still works with AbilitiesJson
   - Eventually migrate to new model structure

## QUICK REFERENCE

**Admin Authorization Required For:**
- POST/PUT/DELETE cards
- POST/PUT/DELETE abilities
- POST/PUT/DELETE effects

**Example Create Card:**
```bash
curl -X POST http://localhost:5000/api/v1/cards \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d @create-card.json
```

**Example Get Card With Abilities:**
```bash
curl http://localhost:5000/api/v1/cards/ember_vanguard
```

Returns full card with nested abilities and effects.
