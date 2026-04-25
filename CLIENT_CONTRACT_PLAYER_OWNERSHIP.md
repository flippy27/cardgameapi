# CardDuel API — Player Ownership, Inventory & Crafting Contract

**Version:** 1.0  
**Base URL:** `http://<host>/api/v1`  
**Auth:** JWT Bearer (`Authorization: Bearer <token>`)

---

## Overview

This document covers the three new systems added to CardDuel:

1. **Player Card Collection** — players own specific card instances, each with its own UUID
2. **Player Inventory** — players hold items (earned from matches, events, etc.)
3. **Crafting** — players spend items to obtain new card instances

---

## Core Concepts

### Card Instances vs. Card Definitions

The game has two levels of card data:

| Layer | Table | What it is |
|---|---|---|
| **Card Definition** (`cards`) | Global catalog | The base template: stats, abilities, art. Read-only from player perspective. |
| **Player Card** (`player_cards`) | Player-owned instance | A copy owned by one player. Has its own UUID. Can have upgrades. |

When a player builds a deck, they will eventually select **player card instances**, not raw definitions. This allows:
- Multiple copies of the same card in a collection
- Each copy having different upgrade states
- Precise tracking of which upgraded copy is in which deck

### Items

Items are the in-game economy for crafting. The primary item is `card_dust`, earned by playing matches. Other item types exist for faction-specific or rarity-specific crafting.

Available item type keys (seeded at startup):

| Key | Name | Use |
|---|---|---|
| `card_dust` | Card Dust | Base material for all crafting |
| `arcane_shard` | Arcane Shard | Rare/Epic card crafting |
| `essence_of_void` | Essence of Void | Legendary cards + special upgrades |
| `faction_ember` | Ember Ember | Ember faction card crafting |
| `faction_tidal` | Tidal Droplet | Tidal faction card crafting |
| `faction_grove` | Grove Seed | Grove faction card crafting |
| `faction_alloy` | Alloy Scrap | Alloy faction card crafting |
| `faction_void` | Void Crystal | Void faction card crafting |
| `upgrade_stone` | Upgrade Stone | Applying stat upgrades to owned cards |
| `ability_tome` | Ability Tome | Adding a new ability to an owned card |

### Upgrades

Each player card can have N upgrade rows applied to it. Upgrades are flexible key-value rows:

| `upgrade_kind` | `int_value` | `string_value` | Meaning |
|---|---|---|---|
| `attack_bonus` | +N | — | Adds N to attack |
| `health_bonus` | +N | — | Adds N to max health |
| `armor_bonus` | +N | — | Adds N to armor |
| `level_up` | — | — | Increments the card level by 1 |
| `added_ability` | — | `ability_id` | Grants an extra ability to this card |
| `custom_tag` | — | `"tag_string"` | Arbitrary metadata tag |

Effective stats are computed by the server: `base_stat + sum(all matching upgrade int_values)`.

---

## 1. Item Type Catalog

### `GET /api/v1/items`

List all item types. Public, no auth required.

**Response `200 OK`:**
```json
[
  {
    "id": 0,
    "key": "card_dust",
    "displayName": "Card Dust",
    "description": "Basic crafting material earned by playing matches.",
    "category": "crafting",
    "maxStack": -1,
    "isActive": true,
    "iconAssetRef": "ui/items/card_dust",
    "metadataJson": "{}"
  }
]
```

### `GET /api/v1/items/{key}`

Get a single item type by key.

```
GET /api/v1/items/card_dust
```

**Response `200 OK`:** Same shape as array element above.  
**Response `404`:** Item type not found.

---

## 2. Player Inventory

All inventory endpoints require the authenticated user to be the player specified in the path.

### `GET /api/v1/players/{userId}/inventory`

Get the full inventory of a player. Only shows items with existing rows (i.e., items the player has received at least once).

**Response `200 OK`:**
```json
{
  "userId": "abc123",
  "items": [
    {
      "id": "uuid",
      "userId": "abc123",
      "itemTypeId": 0,
      "itemTypeKey": "card_dust",
      "itemTypeDisplayName": "Card Dust",
      "itemTypeCategory": "crafting",
      "quantity": 350,
      "createdAt": "2026-04-20T12:00:00Z",
      "updatedAt": "2026-04-25T10:30:00Z"
    }
  ]
}
```

### `GET /api/v1/players/{userId}/inventory/{itemTypeKey}`

Get balance of a single item. Returns `quantity: 0` if the player has never received this item.

```
GET /api/v1/players/abc123/inventory/card_dust
```

**Response `200 OK`:**
```json
{
  "id": "uuid-or-empty",
  "userId": "abc123",
  "itemTypeId": 0,
  "itemTypeKey": "card_dust",
  "itemTypeDisplayName": "Card Dust",
  "itemTypeCategory": "crafting",
  "quantity": 350,
  "createdAt": "...",
  "updatedAt": "..."
}
```

### `POST /api/v1/players/{userId}/inventory/grant`

Grant items to a player. Quantity is additive. Used for match rewards, event rewards, admin grants.

**Request body:**
```json
{
  "itemTypeKey": "card_dust",
  "quantity": 100,
  "reason": "match_reward"
}
```

**Response `200 OK`:**
```json
{
  "success": true,
  "message": "Granted 100x card_dust.",
  "updatedItem": { ...PlayerItemDto... }
}
```

**Response `404`:** Item type key not found.

### `POST /api/v1/players/{userId}/inventory/consume`

Consume (deduct) items. Fails if balance is insufficient. Use this for admin corrections. For crafting use the crafting endpoint.

**Request body:**
```json
{
  "itemTypeKey": "card_dust",
  "quantity": 50,
  "reason": "admin_correction"
}
```

**Response `200 OK`:** Updated item balance.  
**Response `409 Conflict`:** Insufficient balance — message tells you how many you have vs. how many are needed.

---

## 3. Player Card Collection

### `GET /api/v1/players/{userId}/cards`

Get the player's full card collection as a flat list.

**Response `200 OK`:**
```json
{
  "userId": "abc123",
  "totalCards": 5,
  "cards": [
    {
      "id": "player-card-uuid",
      "userId": "abc123",
      "cardDefinitionId": "card-definition-uuid",
      "cardId": "ember_vanguard",
      "displayName": "Ember Vanguard",
      "cardRarity": 0,
      "cardFaction": 0,
      "cardType": 0,
      "acquiredFrom": "crafted",
      "acquiredAt": "2026-04-25T14:00:00Z"
    }
  ]
}
```

### `GET /api/v1/players/{userId}/cards/summary`

Get a grouped summary — cards grouped by card type with copy count. Useful for the collection screen and deck building.

**Response `200 OK`:**
```json
{
  "userId": "abc123",
  "uniqueCardTypes": 3,
  "totalCopies": 5,
  "cards": [
    {
      "cardId": "ember_vanguard",
      "displayName": "Ember Vanguard",
      "ownedCopies": 2,
      "ownedInstances": [
        { "id": "uuid1", ...PlayerCardDto... },
        { "id": "uuid2", ...PlayerCardDto... }
      ]
    }
  ]
}
```

### `GET /api/v1/players/{userId}/cards/{playerCardId}`

Get a specific owned card instance with full detail including computed effective stats and upgrade history.

**Response `200 OK`:**
```json
{
  "id": "player-card-uuid",
  "userId": "abc123",
  "cardDefinitionId": "def-uuid",
  "cardId": "ember_vanguard",
  "displayName": "Ember Vanguard",
  "description": "...",
  "manaCost": 2,
  "baseAttack": 3,
  "baseHealth": 3,
  "baseArmor": 0,
  "cardRarity": 0,
  "cardFaction": 0,
  "cardType": 0,
  "unitType": 0,
  "acquiredFrom": "crafted",
  "acquiredAt": "2026-04-25T14:00:00Z",
  "effectiveAttack": 5,
  "effectiveHealth": 3,
  "effectiveArmor": 1,
  "level": 2,
  "upgrades": [
    {
      "id": "upgrade-uuid",
      "playerCardId": "player-card-uuid",
      "upgradeKind": "attack_bonus",
      "intValue": 2,
      "stringValue": null,
      "appliedAt": "2026-04-26T10:00:00Z",
      "appliedBy": "upgrade_system",
      "note": "Tier 1 attack upgrade"
    },
    {
      "id": "upgrade-uuid-2",
      "playerCardId": "player-card-uuid",
      "upgradeKind": "level_up",
      "intValue": null,
      "stringValue": null,
      "appliedAt": "2026-04-26T10:01:00Z",
      "appliedBy": "upgrade_system",
      "note": null
    }
  ]
}
```

**Fields:**
- `effectiveAttack/Health/Armor`: base stats + sum of all corresponding bonus upgrades
- `level`: 1 + count of `level_up` upgrade rows

### `GET /api/v1/players/{userId}/cards/by-card/{cardId}`

Get all owned copies of a specific card type by string card id (e.g. `"ember_vanguard"`).

```
GET /api/v1/players/abc123/cards/by-card/ember_vanguard
```

**Response `200 OK`:** Array of `PlayerCardDto` items.

### `POST /api/v1/players/{userId}/cards/grant`

**(Admin)** Grant a card instance to a player.

**Request body:**
```json
{
  "cardId": "ember_vanguard",
  "acquiredFrom": "admin_grant"
}
```

`acquiredFrom` values: `"admin_grant"`, `"match_reward"`, `"crafted"`, `"starter_pack"`, `"event_reward"`.

**Response `201 Created`:** The new `PlayerCardDto`.

### `DELETE /api/v1/players/{userId}/cards/{playerCardId}`

**(Admin)** Delete/revoke a player card instance. Permanent.

**Response `204 No Content`.**

---

## 4. Card Upgrades

### `GET /api/v1/players/{userId}/cards/{playerCardId}/upgrades`

Get all upgrades applied to a specific owned card.

**Response `200 OK`:** Array of `PlayerCardUpgradeDto`.

### `POST /api/v1/players/{userId}/cards/{playerCardId}/upgrades`

Apply an upgrade to an owned card.

**Request body:**
```json
{
  "upgradeKind": "attack_bonus",
  "intValue": 2,
  "stringValue": null,
  "appliedBy": "upgrade_system",
  "note": "Tier 1 attack upgrade applied via upgrade stone"
}
```

| Field | Type | Notes |
|---|---|---|
| `upgradeKind` | string (required) | See upgrade kind table above |
| `intValue` | int? | Numeric delta (positive or negative) |
| `stringValue` | string? | String reference (e.g. `ability_id` for `added_ability`) |
| `appliedBy` | string | Source identifier: `"upgrade_system"`, `"admin"`, `"event"` |
| `note` | string? | Human-readable description (optional) |

**Response `201 Created`:** The new `PlayerCardUpgradeDto`.

> **Note:** The server does NOT deduct any items when applying upgrades via this endpoint. Item deduction for upgrade costs is handled separately by the game client calling the inventory endpoints before applying the upgrade. If you want the server to handle upgrade costs atomically, request a dedicated upgrade crafting endpoint.

### `DELETE /api/v1/players/{userId}/cards/{playerCardId}/upgrades/{upgradeId}`

Remove a specific upgrade. Stats recompute automatically on next fetch.

**Response `204 No Content`.**

---

## 5. Crafting

### `GET /api/v1/crafting/cards`

List all cards that have crafting requirements (craftable cards).

**Response `200 OK`:**
```json
[
  {
    "cardId": "ember_vanguard",
    "displayName": "Ember Vanguard",
    "cardRarity": 0,
    "isCraftable": true,
    "requirements": [
      {
        "id": "req-uuid",
        "cardDefinitionId": "def-uuid",
        "itemTypeId": 0,
        "itemTypeKey": "card_dust",
        "itemTypeDisplayName": "Card Dust",
        "quantityRequired": 200
      }
    ]
  }
]
```

### `GET /api/v1/crafting/cards/{cardId}`

Get crafting info for a specific card.

```
GET /api/v1/crafting/cards/ember_vanguard
```

**Response `200 OK`:** Same shape as array element above.  
**Response `404`:** Card not found.

### `POST /api/v1/crafting/cards/{cardId}`

Craft a card. Requires authentication. The calling user is the recipient.

- Checks all requirements against the player's inventory
- Fails atomically if any requirement is not met (no partial deductions)
- On success: deducts all required items, creates a new `player_card` instance

```
POST /api/v1/crafting/cards/ember_vanguard
```
*(No request body needed — the userId comes from the JWT token)*

**Response `200 OK`:**
```json
{
  "success": true,
  "message": "Card 'Ember Vanguard' crafted successfully.",
  "playerCard": { ...PlayerCardDto... },
  "updatedInventory": [
    { "itemTypeKey": "card_dust", "quantity": 150, ... }
  ]
}
```

**Response `409 Conflict`:** Insufficient items or no requirements defined.
```json
{ "message": "Insufficient 'Card Dust': need 200, have 150." }
```

### `PUT /api/v1/crafting/cards/{cardId}/requirements`

**(Admin)** Replace all crafting requirements for a card. Pass empty array to make uncraftable.

**Request body:**
```json
{
  "requirements": [
    { "itemTypeKey": "card_dust", "quantityRequired": 200 },
    { "itemTypeKey": "faction_ember", "quantityRequired": 10 }
  ]
}
```

**Response `200 OK`:** Array of `CraftingRequirementDto`.

### `DELETE /api/v1/crafting/cards/{cardId}/requirements/{requirementId}`

**(Admin)** Remove one requirement row.

**Response `204 No Content`.**

---

## 6. Deck Building with Owned Cards

> **Current State:** The deck endpoints (`/api/v1/decks`) currently accept `cardIds` as string arrays (e.g. `["ember_vanguard"]`). Player card ownership is tracked but not yet enforced at the deck API level.

### Planned Migration Path

1. **Phase 1 (current):** Players can still build decks using card string IDs. Ownership tracked separately.
2. **Phase 2 (upcoming):** Deck endpoint updated to accept `playerCardIds` (UUIDs from `/players/{userId}/cards`). Ownership enforced — players can only add cards they own.
3. **Phase 3:** Each `DeckCard` row references the specific `player_card` instance. Upgrades of that instance apply during the match.

### Checking Ownership Before Deck Building (Client Guide)

Until Phase 2, the client should:

1. Call `GET /api/v1/players/{userId}/cards/summary` to get owned cards.
2. Filter the available card catalog to only show cards the player owns.
3. Allow deck building only from owned cards.
4. When submitting the deck, use the card string IDs (existing API).

In Phase 2, step 4 will change to submitting `playerCardIds` (the UUIDs).

---

## 7. Awarding Items from Match Completion

When a match ends, the game server should grant items to the winner (and optionally the loser). This is done via:

```
POST /api/v1/players/{userId}/inventory/grant
{
  "itemTypeKey": "card_dust",
  "quantity": 50,
  "reason": "match_win"
}
```

Typical reward schedule (configure per your game design):
- Win: 50 card_dust
- Loss: 20 card_dust
- Win (faction match): 50 card_dust + 5 faction_X material

The match completion endpoint (`POST /api/v1/matches/{matchId}/complete`) does **not** automatically grant rewards. The client is responsible for calling the inventory grant endpoint after a match concludes. If you want server-authoritative reward grants, let us know and we can add a rewards hook to the match completion flow.

---

## 8. Common Workflows

### Flow: New Player Setup
```
1. POST /api/v1/auth/register → get JWT
2. POST /api/v1/players/{userId}/inventory/grant { "itemTypeKey": "card_dust", "quantity": 500 }
3. GET  /api/v1/crafting/cards → show craftable cards
4. POST /api/v1/crafting/cards/ember_vanguard → craft a card
5. GET  /api/v1/players/{userId}/cards → show collection
6. POST /api/v1/decks + PUT /api/v1/decks → build deck from owned cards
```

### Flow: Upgrade a Card
```
1. POST /api/v1/players/{userId}/inventory/consume { "itemTypeKey": "upgrade_stone", "quantity": 1 }
2. POST /api/v1/players/{userId}/cards/{playerCardId}/upgrades
   { "upgradeKind": "attack_bonus", "intValue": 2, "appliedBy": "upgrade_system" }
3. GET  /api/v1/players/{userId}/cards/{playerCardId} → verify effectiveAttack updated
```

### Flow: Display Collection Screen
```
1. GET /api/v1/players/{userId}/cards/summary → grouped by card type with copy counts
2. GET /api/v1/cards → full card catalog (for unowned cards, shown as locked)
3. GET /api/v1/crafting/cards → which cards are craftable (and their costs)
4. GET /api/v1/players/{userId}/inventory → what materials the player has
```

---

## 9. Error Reference

| HTTP Status | Meaning |
|---|---|
| `200 OK` | Success |
| `201 Created` | Resource created (includes Location header) |
| `204 No Content` | Success, no body (DELETE) |
| `400 Bad Request` | Validation error (check message) |
| `401 Unauthorized` | JWT missing or expired |
| `403 Forbidden` | Accessing another player's data |
| `404 Not Found` | Resource not found |
| `409 Conflict` | Business logic failure (insufficient items, duplicate, etc.) |

All error responses follow:
```json
{ "message": "Human-readable error description." }
```

---

## 10. Table Schema Reference

```
player_cards
├── id                  UUID PK
├── user_id             → users.id (cascade delete)
├── card_definition_id  → cards.id (restrict delete)
├── acquired_from       varchar(64)   "crafted" | "admin_grant" | "match_reward" | "starter_pack" | "event_reward"
└── acquired_at         timestamptz

player_card_upgrades
├── id              UUID PK
├── player_card_id  → player_cards.id (cascade delete)
├── upgrade_kind    varchar(64)   free-form key, extensible
├── int_value       int?
├── string_value    varchar(255)?
├── applied_at      timestamptz
├── applied_by      varchar(64)
└── note            varchar(512)?

item_type_definitions
├── id            int PK (seeded, enum-like)
├── key           varchar(64) UNIQUE
├── display_name  varchar(128)
├── description   varchar(512)
├── category      varchar(64)  "crafting" | "faction" | "upgrade" | "currency"
├── max_stack     int  (-1 = unlimited)
├── is_active     bool
└── icon_asset_ref varchar(255)?

player_items
├── id            UUID PK
├── user_id       → users.id (cascade delete)
├── item_type_id  → item_type_definitions.id (restrict delete)
├── quantity      bigint
├── created_at    timestamptz
└── updated_at    timestamptz
UNIQUE (user_id, item_type_id)

card_crafting_requirements
├── id                  UUID PK
├── card_definition_id  → cards.id (cascade delete)
├── item_type_id        → item_type_definitions.id (restrict delete)
└── quantity_required   int
UNIQUE (card_definition_id, item_type_id)
```

---

## 11. Notes & Future Extensions

- **Upgrade costs:** Currently upgrades are applied server-side without item cost validation. A future endpoint can bundle "consume items + apply upgrade" atomically.
- **Deck building enforcement:** The `deck_cards` table has a nullable `player_card_id` column. Once Phase 2 is deployed, this will be required and decks will only accept cards the player owns.
- **Seasonal/event items:** New item types can be added to `item_type_definitions` via seeding or migration. Clients should always fetch `/api/v1/items` rather than hard-coding item IDs.
- **Crafting duplicates:** Nothing prevents crafting a second copy of a card you already own. If a "max copies" limit is desired, it can be added as a check in the crafting endpoint.
- **Upgrade kinds:** The `upgrade_kind` column is free-form — new upgrade types can be added without a migration. The client should handle unknown upgrade kinds gracefully (display as "Unknown Upgrade").
