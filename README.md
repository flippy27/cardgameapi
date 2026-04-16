# CardDuel Server API

Backend autoritativo en C# para el juego de cartas 1v1 multijugador.

## ✅ Características Implementadas

### Autenticación & Usuarios
- ✅ POST `/api/auth/register` - Registro de nuevo usuario
- ✅ POST `/api/auth/login` - Login con email/password → JWT
- ✅ GET `/api/users/{playerId}/profile` - Perfil de jugador
- ✅ GET `/api/users/{playerId}/stats` - Estadísticas (rating, W/L, win rate)
- ✅ GET `/api/users/leaderboard` - Leaderboard global (paginado)

### Mazos
- ✅ GET `/api/decks/catalog` - Catálogo de cartas
- ✅ GET `/api/decks/{playerId}` - Mazos del jugador
- ✅ PUT `/api/decks` - Crear/actualizar mazo con validación
  - Min 20 cartas, Max 30 cartas
  - Max 3 copias por carta
  - Validación de IDs de carta

### Matchmaking
- ✅ POST `/api/matchmaking/private` - Crear partida privada
- ✅ POST `/api/matchmaking/private/join` - Unirse a partida privada
- ✅ POST `/api/matchmaking/queue` - Encolar para casual/ranked
  - Casual: Quick match any opponent
  - Ranked: Rating-based pairing (ELO 1600+)

### Matches
- ✅ GET `/api/matches` - Listar partidas activas
- ✅ GET `/api/matches/{matchId}/summary` - Resumen de partida
- ✅ GET `/api/matches/{matchId}/snapshot/{playerId}` - Estado actual
- ✅ GET `/api/matches/history/{playerId}` - Historial (paginado)
- ✅ POST `/api/matches/{matchId}/ready` - Marcar ready
- ✅ POST `/api/matches/{matchId}/play` - Jugar carta
- ✅ POST `/api/matches/{matchId}/end-turn` - Terminar turno
- ✅ POST `/api/matches/{matchId}/forfeit` - Abandonar

### Sistema de Rating
- ✅ ELO rating (K=32)
- ✅ Actualización automática después de match
- ✅ Rating delta en historial
- ✅ Leaderboard ordenado por rating

### Real-Time (SignalR)
- ✅ Hub `/hubs/match` para snapshots en tiempo real
- ✅ Broadcast a ambos jugadores + spectators
- ✅ Reconnect handling con tokens

### Infraestructura
- ✅ PostgreSQL con Entity Framework Core
- ✅ Error handling global con response estándar
- ✅ Serilog para structured logging
- ✅ JWT authentication
- ✅ BCrypt para password hashing
- ✅ Docker + Docker Compose para dev local

## 🏗️ Arquitectura

```
┌─────────────────┐
│  Unity Client   │ (render, input, audio, VFX)
└────────┬────────┘
         │ REST + SignalR (JWT auth)
         ▼
┌─────────────────────────────┐
│  CardDuel.ServerApi (C#)    │ (authoritative game logic)
├─────────────────────────────┤
│ - Controllers               │ (API endpoints)
│ - MatchEngine               │ (game rules)
│ - RatingService             │ (ELO calculation)
│ - DeckValidationService     │ (deck rules)
└────────┬────────────────────┘
         │ EF Core
         ▼
    ┌─────────────┐
    │ PostgreSQL  │ (users, decks, matches, ratings)
    └─────────────┘
```

## 🚀 Desarrollo Local

### Prerequisites
- .NET 8+ SDK
- Docker + Docker Compose (o PostgreSQL local)

### Setup

1. **Clone & restore**
```bash
dotnet restore
```

2. **Start DB + Redis**
```bash
docker-compose up -d
```

3. **Run migrations**
```bash
dotnet ef database update
```

4. **Run API** (hot reload)
```bash
dotnet watch run
```

API estará en: `http://localhost:5000`
Swagger UI: `http://localhost:5000/swagger`

### Default Credentials (Dev)
- DB: `postgres:postgres@localhost:5432/cardduel`
- Redis: `localhost:6379`
- JWT Key: `super-secret-key-change-in-production-...`

## 📚 API Quick Start

### 1. Registrar usuario
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "player1@test.com",
    "username": "player1",
    "password": "secure_password_123"
  }'
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "guid",
  "username": "player1",
  "email": "player1@test.com"
}
```

### 2. Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "player1@test.com",
    "password": "secure_password_123"
  }'
```

### 3. Crear mazo
```bash
curl -X PUT http://localhost:5000/api/decks \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "playerId": "<userId>",
    "deckId": "fire_deck_1",
    "displayName": "Inferno Deck",
    "cardIds": [
      "ember_vanguard", "ember_vanguard", "ember_archer",
      "ember_burnseer", "tidal_priest", "tidal_lancer",
      "tidal_sniper", "grove_guardian", "grove_shaper",
      "grove_raincaller", "alloy_bulwark", "alloy_ballista",
      "alloy_hoplite", "void_stalker", "void_caller",
      "void_magus", "ember_colossus", "tidal_waveblade",
      "grove_myr", "ember_archer", "tidal_lancer"
    ]
  }'
```

### 4. Queue para partida casual
```bash
curl -X POST http://localhost:5000/api/matchmaking/queue \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "playerId": "<userId>",
    "deckId": "fire_deck_1",
    "mode": 0,
    "rating": 1000
  }'
```

**Response:**
```json
{
  "matchId": "match_uuid",
  "roomCode": "ABC123",
  "reconnectToken": "token_xyz",
  "seatIndex": 0,
  "mode": "Casual",
  "waitingForOpponent": true,
  "status": "WaitingForPlayers"
}
```

### 5. Conectar SignalR
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/match?access_token=" + token)
  .withAutomaticReconnect()
  .build();

connection.on("MatchSnapshot", (snapshot) => {
  console.log("Snapshot actualizado:", snapshot);
});

await connection.start();

// Conectar al match
await connection.invoke("ConnectToMatch", {
  playerId: userId,
  matchId: matchId,
  reconnectToken: reconnectToken
});
```

## 🔒 Seguridad

- ✅ JWT con RS256 (change signing key in production)
- ✅ BCrypt password hashing (cost 11)
- ✅ Validación de autorización por `sub` claim
- ✅ Deck validation previene cartas inválidas
- ✅ Error handling evita information leaks

**TODO:**
- [ ] Rate limiting por IP + user
- [ ] CSRF tokens
- [ ] Signed deck manifests
- [ ] Replay logs para disputes
- [ ] CORS configuration

## 📊 Database Schema

### Users
- `Id` (PK)
- `Email` (unique)
- `Username` (unique)
- `PasswordHash` (bcrypt)
- `IsActive`
- `CreatedAt`, `LastLoginAt`

### PlayerRatings
- `Id` (PK)
- `UserId` (FK)
- `RatingValue` (int, default 1000)
- `Wins`, `Losses`
- `Region` (global)
- `CreatedAt`, `UpdatedAt`

### PlayerDecks
- `Id` (PK)
- `UserId` (FK)
- `DeckId`
- `DisplayName`
- `CardIds` (json array)
- `CreatedAt`, `UpdatedAt`

### MatchRecords
- `Id` (PK)
- `MatchId` (unique)
- `Player1Id`, `Player2Id` (FKs)
- `WinnerId`
- `Mode` (Casual/Ranked/Private)
- `DurationSeconds`
- `RatingBefore/After` (both players)
- `CreatedAt`, `CompletedAt`

## 🧪 Testing

```bash
# Unit tests
dotnet test

# Integration tests
dotnet test -- --filter "Integration"
```

## 📦 Deployment

### Docker Build
```bash
docker build -t cardduel-api:latest .
docker run -p 8080:8080 \
  -e "ConnectionStrings:DefaultConnection=..." \
  -e "Jwt:SigningKey=..." \
  cardduel-api:latest
```

### Environment Variables (Production)
```bash
ConnectionStrings:DefaultConnection=Server=...;Port=5432;Database=...;
Jwt:SigningKey=<32+ char secret>
Jwt:Issuer=cardduel-server
Jwt:Audience=cardduel-clients
SignalR:UseRedisBackplane=true
SignalR:RedisConnectionString=redis-host:6379
ASPNETCORE_ENVIRONMENT=Production
```

## 📋 Roadmap

### Phase 1 (✅ Done)
- [x] User authentication (register/login)
- [x] Basic matchmaking (casual/ranked)
- [x] Game engine with match flow
- [x] Real-time updates (SignalR)
- [x] ELO rating system
- [x] Deck validation
- [x] Error handling + logging

### Phase 2 (In Progress)
- [ ] Spectator mode (redacted snapshots)
- [ ] Tournament system (bracket logic)
- [ ] Advanced matchmaking (region-based, skill brackets)
- [ ] Admin dashboard
- [ ] Player ban system
- [ ] Replay logs

### Phase 3 (Planned)
- [ ] Seasonal ranking reset
- [ ] Cosmetics/rewards system
- [ ] Social features (friends, chat)
- [ ] Mobile optimization
- [ ] Analytics dashboard
- [ ] Fraud detection

## 🐛 Known Issues

- Npgsql 8.0.0 has CVE (upgrade to 8.0.1+)
- In-memory stores still used for cards/tournaments (migrate to DB)
- No replay logs yet
- SignalR backplane not tested under heavy load

## 📞 Support

- **Issues**: GitHub Issues
- **Questions**: Discord #support
- **Security**: security@cardduel.dev (responsibly disclose)

---

**Last Updated:** 2026-04-14
**Status:** Alpha (feature complete, testing phase)
