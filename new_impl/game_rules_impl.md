# Implementacion de Reglas de Juego en Servidor

## Objetivo de esta fase

Se agrego una primera capa formal y persistente de `game rules` al servidor para que:

- cada match use una ruleset explicita;
- el cliente Unity pueda recibir esas reglas por match;
- el historial guarde referencia estable de que reglas se usaron;
- futuras variantes de juego o handicaps no obliguen a hardcodear valores en el cliente.

Esta fase cubre principalmente reglas configurables de:

- vida inicial del player;
- vida maxima del player;
- mana inicial;
- mana maximo;
- mana ganado por turno;
- momento en que se gana el mana;
- cantidad de cartas iniciales;
- cantidad de cartas robadas por turno;
- jugador/asiento que empieza;
- handicaps por asiento.

## Lo que se agrego

### 1. Modelo persistente

Se agregaron nuevas entidades/tablas:

- `game_rulesets`
- `game_ruleset_seat_overrides`
- `matchmaking_mode_ruleset_assignments`

Se extendio `matches` con:

- `game_ruleset_id`
- `game_ruleset_name`
- `game_rules_snapshot_json`

### 2. Regla historica por match

Cada `MatchRecord` ahora guarda:

- el id de la ruleset usada;
- el nombre visible de la ruleset usada;
- un snapshot JSON completo de las reglas efectivas del match.

Esto es importante porque una ruleset puede cambiar en el futuro. El match historico sigue apuntando a la configuracion exacta que uso realmente.

### 3. API de rulesets

Se agrego `GameRulesetsController` con Swagger:

- `GET /api/v1/game-rulesets`
- `GET /api/v1/game-rulesets/default`
- `GET /api/v1/game-rulesets/{rulesetId}`
- `POST /api/v1/game-rulesets`
- `PUT /api/v1/game-rulesets/{rulesetId}`
- `POST /api/v1/game-rulesets/{rulesetId}/activate`

Estas endpoints permiten listar, consultar, crear, editar y activar rulesets.

Tambien se agregaron endpoints para administrar que ruleset usa cada modo de matchmaking:

- `GET /api/v1/game-rulesets/matchmaking-modes`
- `PUT /api/v1/game-rulesets/matchmaking-modes/{mode}`

### 4. Reglas entregadas por match

Ahora las reglas no solo existen en DB. Tambien viajan al juego:

- `POST /api/v1/matchmaking/queue`
- `POST /api/v1/matchmaking/private`

Ya no aceptan `rulesetId`.

La ruleset ahora la decide el servidor segun el `mode` del matchmaking, usando la asignacion persistida en `matchmaking_mode_ruleset_assignments`.

Las respuestas de reserva (`MatchReservationDto`) ahora incluyen:

- `rulesetId`
- `rules`

Ademas, `MatchSnapshot` ahora incluye:

- `rulesetId`
- `rules`
- `activePlayerId`
- `isLocalPlayersTurn`

Tambien se agrego una endpoint explicita:

- `GET /api/v1/matches/{matchId}/rules/{playerId}`

Esta lee las reglas persistidas del match desde DB, no desde estado volatile en memoria. Sirve para reconnect, debugging y consumo explicito desde Unity.

### 5. Matchmaking resuelto por modo del servidor

Cada modo de matchmaking tiene una ruleset asignada del lado servidor.

Ejemplo:

- `Casual -> Default Rules`
- `Ranked -> Ranked Rules`
- `Private -> Default Rules`

La cola casual/ranked ya no mezcla jugadores de rulesets distintas, y tampoco depende de que el cliente mande un `rulesetId`.

Dos players solo matchean juntos si:

- modo compatible;
- deck valido;
- ruleset compatible.

### 6. Semantica aplicada en MatchEngine

`MatchEngine` ahora usa `GameRules` para:

- asignar vida y mana inicial por asiento;
- definir el asiento inicial;
- definir el draw inicial;
- definir el draw por turno;
- definir cuando se otorga el mana por turno;
- aplicar handicaps por asiento.

## Semantica exacta de las reglas

### ManaGrantTiming

Se implemento con esta semantica:

- `StartOfTurn`: el jugador que pasa a ser activo recibe su mana al comenzar su turno, antes del robo y antes de las habilidades `OnTurnStart`.
- `EndOfTurn`: el jugador activo recibe su mana al terminar su turno, despues de `OnTurnEnd` y `BattlePhase`, antes de ceder el turno.

Si Unity refleja mana en UI, debe asumir exactamente esa semantica.

### Seat overrides / handicaps

Los handicaps son aditivos sobre la base de la ruleset.

Ejemplo:

- Ruleset base: `StartingHeroHealth = 20`
- Seat override asiento 1: `AdditionalHeroHealth = 5`

Resultado:

- seat 0 empieza con `20`
- seat 1 empieza con `25`

Lo mismo aplica a:

- `MaxHeroHealth`
- `StartingMana`
- `MaxMana`
- `ManaGrantedPerTurn`
- `CardsDrawnOnTurnStart`

## Validaciones agregadas

El servidor ahora valida de forma explicita:

- `RulesetKey` obligatorio;
- `DisplayName` obligatorio;
- una ruleset default debe estar activa;
- `MaxHeroHealth >= StartingHeroHealth`;
- `MaxMana >= StartingMana`;
- no puede haber overrides duplicados por asiento;
- el resultado efectivo por asiento no puede dejar valores invalidos;
- la ruleset asignada a un modo de matchmaking debe existir y estar activa.

## Contratos relevantes para Unity

### Queue / Private

Requests:

- `CreatePrivateMatchRequest`
- `QueueForMatchRequest`

Cambios:

- ya no se envia `rulesetId` desde Unity;
- el servidor decide la ruleset segun el modo.

Responses:

- `MatchReservationDto.rulesetId`
- `MatchReservationDto.rules`

### Snapshot

`MatchSnapshot` ahora expone:

- `rulesetId`
- `rules`
- `activePlayerId`
- `isLocalPlayersTurn`

### Summary / History

`MatchSummaryDto` y el historial ahora exponen:

- `rulesetId`
- `rulesetName`

## Recomendacion de integracion Unity

### Flujo recomendado

1. Login.
2. Guardar el `playerId` real que devuelve auth.
3. Antes de queue/private, solo elegir el `mode`.
4. El servidor resuelve la ruleset asociada a ese modo.
5. Al crear o entrar a match, consumir `MatchReservationDto.rules`.
6. Al conectar o reconectar, usar `MatchSnapshot.rules`.
7. Si hace falta recargar reglas de un match ya persistido, usar `GET /api/v1/matches/{matchId}/rules/{playerId}`.

### Cosas que Unity ya no deberia hardcodear

Idealmente estas reglas deben salir del payload del servidor y no de constantes locales:

- hp inicial;
- hp maximo;
- mana inicial;
- mana maximo;
- mana por turno;
- timing de entrega de mana;
- cartas iniciales;
- cartas robadas por turno;
- quien empieza;
- handicaps por asiento.

### UI/UX recomendada

Unity deberia:

- mostrar el nombre visible de la ruleset en lobby o pantalla de match;
- usar `snapshot.isLocalPlayersTurn` para habilitar acciones de turno;
- usar `snapshot.activePlayerId` para debugging o overlays;
- mantener una copia local de `snapshot.rules` como fuente de verdad del match actual.

## Estado de esta fase vs tus docs de gameplay

Tus docs dejan claras dos capas distintas:

### Capa ya soportada en esta fase

- configuracion de match;
- economia de mana;
- vida del heroe;
- turno inicial;
- draw;
- handicaps;
- persistencia historica de reglas.

### Capa aun no llevada completamente a motor autoritativo de servidor

- prioridad estricta `top -> left -> right`;
- reposicionamiento automatico al morir una carta;
- slots bloqueados/desbloqueados segun ocupacion;
- reglas completas de melee/ranged/magic por posicion;
- pipeline total de skills defensivas/ofensivas/equipables/utilitarias/modifier segun tus docs.

Esas reglas de tablero y combate son la siguiente fase natural para mover mas autoridad al backend.

## Decisiones de diseño tomadas

- una ruleset puede evolucionar en el tiempo, pero cada match guarda snapshot propio;
- el matchmaking resuelve la ruleset solo en servidor, en funcion del modo;
- la asignacion `mode -> ruleset` es persistente y administrable por API;
- el matchmaking separa por ruleset efectiva para evitar emparejamientos incoherentes;
- la lectura de reglas por match se hace desde persistencia para soportar reconnect e historial;
- los handicaps se modelan por asiento y de forma aditiva;
- la integracion se dejo modular para poder sumar futuras categorias sin romper contratos existentes.

## Archivos principales tocados

- `Contracts/ApiDtos.cs`
- `Controllers/GameRulesetsController.cs`
- `Controllers/MatchmakingController.cs`
- `Controllers/MatchesController.cs`
- `Controllers/MatchHistoryController.cs`
- `Game/GameRules.cs`
- `Game/MatchEngine.cs`
- `Infrastructure/AppDbContext.cs`
- `Infrastructure/Models/GameRuleset.cs`
- `Infrastructure/Models/GameRulesetSeatOverride.cs`
- `Infrastructure/Models/MatchmakingModeRulesetAssignment.cs`
- `Infrastructure/Models/MatchRecord.cs`
- `Infrastructure/GameRulesetSeeder.cs`
- `Services/GameRulesetService.cs`
- `Services/InMemoryServices.cs`
- `Migrations/20260421033000_AddGameRulesets.cs`
- `Migrations/20260421041000_AddMatchmakingModeRulesetAssignments.cs`

## Dudas abiertas para siguiente fase

Estas no las congele como logica nueva todavia porque conviene validarlas contigo antes:

- si la prioridad `top -> left -> right` debe gobernar tambien la resolucion final de target selection de todas las abilities;
- si el reposicionamiento de slots debe ocurrir inmediatamente al morir o al cierre de una subfase;
- si la entrega de mana `EndOfTurn` debe reflejarse visualmente al final del turno actual o solo quedar disponible al siguiente ciclo;
- si los handicaps deben poder afectar tambien deck, hand size maxima, turn timer o draw inicial por asiento;
- si quieres versionado formal de rulesets por `ruleset_key + version` ademas del `id`.

## Resultado actual

Con esta fase, el servidor ya puede actuar como fuente profesional y persistente de reglas configurables por match, con trazabilidad historica y contratos claros para Unity.
