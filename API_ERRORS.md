# CardDuel API Error Codes

Todas las respuestas de error incluyen:
```json
{
  "code": 400,
  "message": "User-friendly error message",
  "timestamp": "2026-04-18T10:30:00Z",
  "correlationId": "abc123def456",
  "details": "Stack trace (development only)"
}
```

## Códigos de Error

### 400 - Bad Request
- **PlayCardRequest con slot inválido**: SlotIndex must be 0-2
- **DeckUpsertRequest con <20 o >30 cartas**: Deck must have 20-30 cards
- **Demasiadas copias de una carta**: Max 3 copies per card
- **MatchCompletionRequest con duración negativa**: DurationSeconds must be positive

### 401 - Unauthorized
- **Token JWT expirado**: Authentication required
- **Token JWT inválido**: Unauthorized
- **Usuario no autenticado en match**: Player mismatch

### 404 - Not Found
- **Carta no existe**: Unknown card id 'xxx'
- **Deck no existe**: Deck not found
- **Match no existe**: Match not found
- **Usuario no existe**: Player not found

### 409 - Conflict
- **Match ya completado**: Match already completed
- **Jugador no está en la partida**: Player not in this match
- **Slot ocupado**: Slot is occupied
- **Mana insuficiente**: Not enough mana
- **Turno incorrecto**: It is not this player's turn

### 500 - Internal Server Error
- **Error inesperado del servidor**: An unexpected error occurred

## Ejemplo: PlayCard Fallido

**Request:**
```bash
POST /api/v1/matches/match123/play
Authorization: Bearer eyJ0eXAi...
X-Correlation-Id: req-456789

{
  "matchId": "match123",
  "playerId": "player1",
  "runtimeHandKey": "hand-key-1",
  "slotIndex": 5
}
```

**Response (400):**
```json
{
  "code": 400,
  "message": "Invalid operation",
  "timestamp": "2026-04-18T10:30:00Z",
  "correlationId": "req-456789",
  "details": "SlotIndex must be 0-2 (Front, BackLeft, BackRight)"
}
```

## Tracking de Errores

Todos los errores incluyen `correlationId` que puedes usar para:
1. Encontrar el error en logs: `grep req-456789 logs/cardduel-*.txt`
2. Asociar múltiples requests relacionados
3. Debugging de sesiones completas

## Health Checks

```bash
GET /api/v1/health
```

Retorna:
- ✓ Healthy: DB + Redis OK
- ⚠ Degraded: DB OK pero Redis down
- ✗ Unhealthy: DB down
