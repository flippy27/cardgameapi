# CardDuel API - Roadmap de Desarrollo

**Status:** 80% Infraestructura ✅ | 70% Gameplay ✅ | 20% Polish ⚠️

---

## ✅ COMPLETADO (Gameplay Core)

### ✅ 1. **Sistema de Maná** 
- [x] Incremento per-turno (1→2→3→10)
- [x] Validación de maná antes de jugar carta (EnsureLegalPlacement)
- [x] Deducción de maná al jugar (PlayCard)
- [x] Reset de maná en EndTurn (EndTurn: next.Mana = next.MaxMana)
- [x] Tracking en MatchSnapshot

**Implementación:** MatchEngine.cs (lineas 389, 421-422, 588-591)

---

### ✅ 2. **Sistema de Combate**
- [x] Automático: ExecuteBattlePhase()
- [x] Cálculo de daño: DealDamage() con Armor
- [x] Target selection por card.DefaultAttackSelector
- [x] Derrota de unidad si Health <= 0
- [x] Logging de combate

**Implementación:** MatchEngine.cs (lineas 604-743)

**NOTA:** Combate es automático en ExecuteBattlePhase. No hay Attack() manual en Hub.

---

### ✅ 3. **Resolución de Habilidades**
- [x] Procesar triggers (OnPlay, OnBattlePhase, OnTurnStart, OnTurnEnd)
- [x] Resolver targets (Self, AllEnemies, LowestHealthAlly, FrontlineFirst, BacklineFirst)
- [x] Ejecutar efectos (Damage, Heal, GainArmor, BuffAttack, HitHero)
- [x] Order of operations (ResolveTriggeredAbilities -> ApplyEffects)

**Implementación:** MatchEngine.cs (lineas 640-682)

---

### ✅ 4. **Card Placement & Win Condition**
- [x] Validación de colocación (slot, row restrictions)
- [x] Condición de victoria (HeroHealth <= 0 → WinnerSeatIndex)
- [x] Procesamiento completo de EndTurn
- [x] Fases de turno (OnPlay, OnTurnStart, OnTurnEnd, OnBattlePhase)

**Implementación:** MatchEngine.cs (EnsureLegalPlacement, DamageHero, EndTurn)

---

## ✅ COMPLETADO (Draw & ELO)

### ✅ 5. **Sistema de Robo y ELO**
- [x] Deal 4 cartas al inicio (StartMatch)
- [x] Robar 1 carta en DrawPhase (DrawCard llamado en EndTurn)
- [x] Cálculo ELO formula (K=32, RatingFloor=100, RatingCeiling=4000)
- [x] Update rating en match complete
- [x] Shuffle deck con seed determinístico

**Implementación:** 
- Draw: MatchEngine.cs (lineas 542-552, 522-526)
- ELO: EloRatingService.cs (lineas 14-29)
- DB Update: DbRatingService.cs (lineas 15-51)

**NOTA:** No hay mulligan (redraw). Deal 4 cartas y listo.

---

## 🟡 FALTA MEJORAR (Polish & Edge Cases)

### 6. **Validación de Decks** [PRIORIDAD MEDIA]
- [ ] Mínimo 20 cartas
- [ ] Máximo 60 cartas
- [ ] Enforce isLimited (1 copia)
- [ ] Validar existencia de cardIds

**Estado:** Endpoints POST/PUT /decks existen pero SIN validación.

**Impacto:** Usuarios pueden crear decks inválidos.

---

### 7. **Historial y Logging Detallado** [PRIORIDAD MEDIA]
- [x] Logging de acciones (_logs en MatchEngine)
- [ ] Guardar match_actions en DB
- [ ] Endpoint para obtener acciones
- [ ] Playback logic
- [ ] Replay viewer (frontend)

**Estado:** _logs existe en MatchEngine pero no se persiste en match_actions DB.

**Implementación:** LogReplayActionAsync() llamado en PlayCard(), EndTurn(), Forfeit()

---

### 8. **Tests** [PRIORIDAD BAJA]
- [ ] GameEngineTests
- [ ] CombatTests
- [ ] ManaTests
- [ ] AbilityTests
- [ ] RatingServiceTests (Existe: RatingServiceTests.cs)

---

## 📊 Progress Checklist

### ✅ COMPLETADO (Game Engine 90%)
```
✅ Autenticación (JWT)
✅ Catálogo de cartas (200+ en memoria + DB)
✅ Gestión de decks (CRUD endpoints)
✅ Creación de matches (Public/Private/Ranked)
✅ SignalR connectivity (Hub, snapshots)
✅ Base de datos schema (Completo)
✅ Docker deployment
✅ Health checks

GAME ENGINE:
✅ Mana system (deducción, regeneración, validación)
✅ Combat (automático, damage calc, armor)
✅ Ability resolution (4 triggers, 5 selectors, 8+ effects)
✅ Card placement validation (row/slot restrictions)
✅ Win condition (HeroHealth <= 0)
✅ Draw system (4 cartas inicio, +1 por turno)
✅ ELO ranking (K=32, persiste en DB)
✅ Turn phases (OnPlay, OnTurnStart, OnTurnEnd, OnBattlePhase)
```

### 🟡 EN PROGRESO / FALTA PULIR
```
🟡 Deck validation (min 20, max 60, limited)
🟡 Match action logging (LogReplayActionAsync existe pero no persiste)
🟡 Tests (RatingServiceTests existe, falta GameEngineTests)
```

### ❌ NO CRÍTICO
```
❌ Mulligan (redraw) - No es crítico, deal 4 y juega
❌ Playback/Replay UI - Data existe, falta visualización
❌ Leaderboard - ELO cálculo existe, falta endpoint
```

---

## 📝 Tareas Pendientes (Solo las que REALMENTE faltan)

| Tarea | Complejidad | Horas | Estado |
|-------|------------|-------|--------|
| Deck Validation | 🟡 MEDIA | 2-3h | ⚠️ Pendiente |
| Match Action Persistence | 🟡 MEDIA | 3-4h | ⚠️ Pendiente |
| GameEngineTests | 🟡 MEDIA | 4-6h | ❌ No iniciado |

**Total:** ~9-13 horas (muy manejable)

---

## 🎯 Plan de Implementación Inmediato

### AHORA (1-2 horas)
1. ✅ [DONE] Descubrir estado actual (HECHO)
2. ➡️ **SIGUIENTE:** Implementar Deck Validation (2h)
   - Mínimo 20 cartas
   - Máximo 60 cartas  
   - Enforce isLimited (1 copia limitada)
   - Validar que cardIds existan

### MAÑANA (3-4 horas)
3. Implementar Match Action Persistence (3-4h)
   - LogReplayActionAsync() actualmente no persiste
   - Guardar en match_actions table
   - Crear endpoint GET /matches/{id}/actions

### DESPUÉS (4-6 horas)
4. Escribir GameEngineTests (4-6h)
   - Test mana deduction
   - Test ability triggers
   - Test win condition
   - Test combat damage

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
