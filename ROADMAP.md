# CardDuel API - Roadmap de Desarrollo

**Status:** 80% Infraestructura ✅ | 10% Gameplay ❌ | 10% Improvement ⚠️

---

## 🔴 CRÍTICO (Bloqueador para Gameplay)

### 1. **Sistema de Maná** [PRIORIDAD 1]
- [ ] Incremento per-turno (1→2→3→10)
- [ ] Validación de maná antes de jugar carta
- [ ] Deducción de maná al jugar
- [ ] Reset de maná en EndTurn
- [ ] Tracking en MatchSnapshot

**Impacto:** Sin esto, los jugadores pueden jugar cartas sin límite.

---

### 2. **Sistema de Combate** [PRIORIDAD 1]
- [ ] Método Attack() en MatchHub
- [ ] Cálculo de daño: Attacker.Attack - Defender.Armor
- [ ] Validación: HasAttacked, turnsUntilCanAttack
- [ ] Contraataque (retaliation)
- [ ] Derrota de unidad si Health <= 0
- [ ] Logging de combate

**Impacto:** Sin esto, no hay forma de atacar.

---

### 3. **Resolución de Habilidades** [PRIORIDAD 1]
- [ ] Procesar triggers (OnPlay, OnBattlePhase, OnTurnStart, OnTurnEnd)
- [ ] Resolver targets (Self, AllEnemies, LowestHealthAlly, etc)
- [ ] Ejecutar efectos (GainArmor, BuffAttack, Heal, Damage)
- [ ] Order of operations

**Impacto:** Las cartas tienen habilidades pero no hacen nada.

---

### 4. **Sistema de Mantenimiento** [PRIORIDAD 2]
- [ ] Validación de colocación de cartas (slot, row)
- [ ] Validación de condición de victoria (Health <= 0)
- [ ] Procesamiento completo de EndTurn
- [ ] Fases de turno explícitas (Main, Combat, End)

**Impacto:** El juego puede entrar en estados inválidos.

---

## 🟠 ALTA PRIORIDAD (Necesario para jugar de verdad)

### 5. **Sistema de Robo y Mulligan** [PRIORIDAD 2]
- [ ] Deal 3 cartas al inicio (mulligan)
- [ ] Permitir redraw
- [ ] Robar 1 carta en DrawPhase
- [ ] Validación de máximo de cartas en mano

**Impact:** Sin esto, los jugadores no tienen cartas de verdad.

---

### 6. **Sistema de Ranking ELO** [PRIORIDAD 2]
- [ ] Cálculo ELO formula
- [ ] Update rating en match complete
- [ ] Leaderboard GET endpoint
- [ ] Matchmaking by rating (opcional)

**Impact:** Ranked mode no funciona.

---

## 🟡 MEDIA PRIORIDAD (Polish)

### 7. **Validación de Decks** [PRIORIDAD 3]
- [ ] Mínimo 20 cartas
- [ ] Máximo 60 cartas
- [ ] Enforce isLimited (1 copia)
- [ ] Validar existencia de cardIds

---

### 8. **Historial y Replays** [PRIORIDAD 3]
- [ ] Logging de acciones (match_actions)
- [ ] Endpoint para obtener acciones
- [ ] Playback logic
- [ ] Replay viewer (frontend)

---

### 9. **Tests** [PRIORIDAD 3]
- [ ] GameEngineTests
- [ ] CombatTests
- [ ] ManaTests
- [ ] AbilityTests

---

## 📊 Progress Checklist

### ✅ Completado
```
✅ Autenticación (JWT)
✅ Catálogo de cartas (200+)
✅ Gestión de decks
✅ Creación de matches
✅ SignalR connectivity
✅ Base de datos schema
✅ Docker deployment
✅ Health checks
```

### 🔄 En Progreso
```
🔄 Game engine core
```

### ❌ No Iniciado
```
❌ Maná (Crítico)
❌ Combate (Crítico)
❌ Habilidades (Crítico)
❌ Condiciones de victoria
❌ Draw/Mulligan
❌ ELO ranking
❌ Validación decks
❌ Historial detallado
❌ Tests
```

---

## 📝 Estimaciones

| Tarea | Complejidad | Horas | Bloqueador |
|-------|------------|-------|-----------|
| Mana System | 🔴 CRÍTICA | 4-6h | Sí |
| Combat | 🔴 CRÍTICA | 8-10h | Sí |
| Abilities | 🔴 CRÍTICA | 6-8h | Sí |
| Win Validation | 🟠 ALTA | 2-3h | Sí |
| Deck Validation | 🟠 ALTA | 2h | No |
| Draw/Mulligan | 🟠 ALTA | 4-5h | Sí |
| ELO | 🟡 MEDIA | 3-4h | No |
| History/Replays | 🟡 MEDIA | 5-6h | No |
| Tests | 🟡 MEDIA | 6-8h | No |

**Total:** ~40-50 horas de desarrollo

---

## 🚀 Sugerencia de Orden

1. **Semana 1: Core Engine**
   - Mana System
   - Win Validation
   - Card Placement Validation

2. **Semana 2: Combat**
   - Combat System
   - Ability Resolution
   - Draw/Mulligan

3. **Semana 3: Polish**
   - ELO Ranking
   - History Logging
   - Tests

---

## 🐛 Known Issues

1. **Hash de password en seed-data.sql**
   - ❌ `F6Qy4SHIl43C0v7BvDiaMF8PvQqLGHV6dFyYU9GxlXE=` NO es 123456
   - ✅ Correcto: `jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=`
   - Actualizar en seed-data.sql para ambos usuarios

2. **Mana no se regenera**
   - El servidor tiene campos pero no lógica
   - EndTurn() no resetea mana

3. **No hay win condition**
   - Partidas pueden no terminar nunca
   - Incluso con health <= 0

4. **Habilidades se ignoran**
   - Datos están en DB
   - Pero nunca se ejecutan

---

## 📚 Archivos a Crear/Modificar

### Crear:
```
- Services/AbilityResolver.cs (resolver de habilidades)
- Services/CombatCalculator.cs (cálculos de daño)
- Contracts/AttackRequest.cs
- Tests/GameEngineTests.cs
- Tests/CombatTests.cs
```

### Modificar:
```
- MatchService.cs (agregar lógica de juego)
- MatchHub.cs (agregar método Attack)
- Hubs/MatchHub.cs (validaciones)
- Services/EloRatingService.cs (completar)
- Controllers/DecksController.cs (validación)
```

---

## 🎯 Objetivo Final

Una vez completados los 9 items anteriores:

✅ Los jugadores pueden crear partidas  
✅ Jugar cartas con validación de maná  
✅ Atacar unidades enemigas  
✅ Procesar habilidades  
✅ Ganar/perder partidas  
✅ Ver historial  
✅ Subir en ranking ELO  

**Resultado:** Juego 1v1 totalmente funcional y jugable.

---

**Última actualización:** 2026-04-20  
**Status:** Listos para implementación
