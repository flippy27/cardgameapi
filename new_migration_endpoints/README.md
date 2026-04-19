# Sprint 7: Card System Migration - Game Client Integration Guide

This folder contains all information needed to update your game client to work with the new Card API.

## What Changed?

### Old System (Hardcoded)
- Cards had abilities as JSON string in `AbilitiesJson`
- No structured card types, rarities, factions
- Admin couldn't create/modify cards via API

### New System (Relational)
- Cards have structured `CardDefinition`, `AbilityDefinition`, `EffectDefinition` tables
- Full CRUD admin endpoints for cards, abilities, effects
- Cards have type, rarity, faction metadata
- Backward compatible (old `AbilitiesJson` still exists but deprecated)

---

## Files in This Folder

- **MIGRATION_CHANGES.md** - Complete technical migration details
- **example-create-card.json** - Example POST body for creating a card
- **example-create-ability.json** - Example POST body for creating an ability
- **README.md** - This file

---

## For Game Client Developers

### Step 1: Deploy API Migration

```bash
cd /Users/idhemax/proyects/_MINE/cardgameapi

# Option 1: Restart docker (auto-runs migration on startup)
docker-compose restart cardduel-api

# Option 2: Full rebuild
docker-compose down
docker-compose up --build
```

Verify migration applied:
```bash
docker exec cardduel-postgres psql -U postgres -d cardduel -c "\dt"
# Look for "Abilities" and "Effects" tables
```

### Step 2: Update CardGameApiClient

Your `CardGameApiClient.cs` fetch calls need updating:

**Before (Old):**
```csharp
public async Task<CardDefinitionDto> GetCardAsync(string cardId)
{
    var response = await client.GetAsync($"/cards/{cardId}");
    var card = JsonSerializer.Deserialize<CardDefinition>(await response.Content.ReadAsStringAsync());
    // Parse card.AbilitiesJson manually
}
```

**After (New):**
```csharp
public async Task<CardDefinitionDto> GetCardAsync(string cardId)
{
    var response = await client.GetAsync($"/cards/{cardId}");
    var card = JsonSerializer.Deserialize<CardDefinitionDto>(await response.Content.ReadAsStringAsync());
    // Abilities are now: card.Abilities (List<AbilityDto>)
    // Each ability has: ability.Effects (List<EffectDto>)
}
```

### Step 3: Update Data Models in Game

Update `CardDefinition` in your game code:

**New Properties Available:**
```csharp
public string CardId { get; set; }
public string DisplayName { get; set; }
public string Description { get; set; }         // NEW
public int ManaCost { get; set; }
public int Attack { get; set; }
public int Health { get; set; }
public int Armor { get; set; }
public int CardType { get; set; }                // NEW: 0=Unit, 1=Utility, 2=Equipment, 3=Spell
public int CardRarity { get; set; }              // NEW: 0=Common, 1=Rare, 2=Epic, 3=Legendary
public int CardFaction { get; set; }             // NEW: 0=Ember, 1=Tidal, 2=Grove, 3=Alloy, 4=Void
public int? UnitType { get; set; }               // NEW: 0=Melee, 1=Ranged, 2=Magic
public int AllowedRow { get; set; }
public int DefaultAttackSelector { get; set; }
public int TurnsUntilCanAttack { get; set; }     // NEW
public bool IsLimited { get; set; }              // NEW
public List<AbilityDto> Abilities { get; set; } // CHANGED: Was JSON, now list
```

### Step 4: Parse Abilities Structure

**Old:**
```csharp
var abilitiesJson = card.AbilitiesJson; // "[{...}, {...}]"
// Deserialize manually, handle errors
```

**New:**
```csharp
foreach (var ability in card.Abilities)
{
    string abilityId = ability.AbilityId;
    string displayName = ability.DisplayName;
    int triggerKind = ability.TriggerKind; // 0=OnPlay, 1=OnTurnStart, etc.
    int targetSelectorKind = ability.TargetSelectorKind; // 0=Self, 1=FrontlineFirst, etc.
    
    foreach (var effect in ability.Effects)
    {
        int effectKind = effect.EffectKind; // 0=Damage, 1=Heal, etc.
        int amount = effect.Amount;
        int sequence = effect.Sequence; // Order of execution
    }
}
```

---

## DTO Structures (For Reference)

### CardDefinitionDto
```json
{
  "id": "guid",
  "cardId": "ember_vanguard",
  "displayName": "Ember Vanguard",
  "description": "A tough melee unit",
  "manaCost": 2,
  "attack": 3,
  "health": 3,
  "armor": 0,
  "cardType": 0,
  "cardRarity": 0,
  "cardFaction": 0,
  "unitType": 0,
  "allowedRow": 0,
  "defaultAttackSelector": 1,
  "turnsUntilCanAttack": 1,
  "isLimited": false,
  "abilities": [
    {
      "id": "guid",
      "abilityId": "hero_ping",
      "displayName": "Hero Ping",
      "description": "...",
      "triggerKind": 2,
      "targetSelectorKind": 0,
      "effects": [
        {
          "id": "guid",
          "effectKind": 4,
          "amount": 2,
          "sequence": 0
        }
      ]
    }
  ]
}
```

### Enum Values

**CardType:**
- 0 = Unit
- 1 = Utility
- 2 = Equipment
- 3 = Spell

**CardRarity:**
- 0 = Common
- 1 = Rare
- 2 = Epic
- 3 = Legendary

**CardFaction:**
- 0 = Ember
- 1 = Tidal
- 2 = Grove
- 3 = Alloy
- 4 = Void

**UnitType:**
- 0 = Melee
- 1 = Ranged
- 2 = Magic
- null = Non-unit card

**TriggerKind:**
- 0 = OnPlay
- 1 = OnTurnStart
- 2 = OnTurnEnd
- 3 = OnBattlePhase

**TargetSelectorKind:**
- 0 = Self
- 1 = FrontlineFirst
- 2 = BacklineFirst
- 3 = AllEnemies
- 4 = LowestHealthAlly

**EffectKind:**
- 0 = Damage
- 1 = Heal
- 2 = GainArmor
- 3 = BuffAttack
- 4 = HitHero
- 5 = Stun
- 6 = Poison
- 7 = Leech
- 8 = Evasion
- 9 = Shield
- 10 = Reflection
- 11 = Dodge
- 12 = Enrage
- 13 = ManaBurn
- 14 = Regenerate
- 15 = Execute
- 16 = DiagonalAttack
- 17 = Fly

---

## Testing the New Endpoints

### 1. Create a Card (Admin Only)
```bash
TOKEN="your_jwt_token_here"
curl -X POST http://localhost:5000/api/v1/cards \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @example-create-card.json
```

### 2. Add Ability to Card (Admin Only)
```bash
TOKEN="your_jwt_token_here"
CARD_ID="test_card_example"
curl -X POST http://localhost:5000/api/v1/cards/$CARD_ID/abilities \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @example-create-ability.json
```

### 3. Get Card with All Details (Public)
```bash
curl http://localhost:5000/api/v1/cards/test_card_example
```

### 4. Get All Cards (Public)
```bash
curl http://localhost:5000/api/v1/cards
```

### 5. Filter by Type (Public)
```bash
curl http://localhost:5000/api/v1/cards?cardType=0  # Units only
```

### 6. Filter by Rarity (Public)
```bash
curl http://localhost:5000/api/v1/cards?rarity=2    # Epic rarity
```

### 7. Filter by Faction (Public)
```bash
curl http://localhost:5000/api/v1/cards?faction=0   # Ember faction
```

---

## Backward Compatibility

**Old data still works:**
- Existing cards seeded with JSON abilities
- `AbilitiesJson` column preserved (marked [Obsolete])
- SeederService still uses old format
- API transparently serves both old and new format

**Migration Strategy:**
1. Run API with new code = auto-migration runs
2. Existing cards remain unchanged
3. New cards created via API use new relational structure
4. Gradually migrate seeded data when ready

---

## Troubleshooting

### "Table Abilities not found" Error
The migration hasn't run. Restart API:
```bash
docker-compose restart cardduel-api
```

### "password authentication failed" Error
Local postgres has different password than docker. Use docker container:
```bash
docker exec cardduel-postgres psql -U postgres -d cardduel -c "SELECT COUNT(*) FROM \"Abilities\";"
```

### Game Client Doesn't See Abilities
- Check CardDefinitionDto parsing (Abilities is now a list, not JSON string)
- Verify API returns 200 OK with full structure
- Check JSON deserialization settings (snake_case vs PascalCase)

---

## Summary

✅ **API is ready**
- All endpoints implemented
- Models created
- Migration applied automatically
- Admin CRUD fully functional

⚠️ **Game Client Action Required**
1. Restart/rebuild docker-compose
2. Update CardGameApiClient parsing
3. Update local card data models
4. Test new ability/effect structure
5. Verify seeded cards load correctly

For details, see MIGRATION_CHANGES.md
