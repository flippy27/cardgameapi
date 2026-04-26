# Delete Contract

## Objetivo

El servidor ahora soporta destruccion manual de una carta en juego y combate base con intercambio de dano entre cartas. El cliente debe tratar ambos flujos como server-authoritative.

## Nuevo endpoint REST

- `POST /api/v1/matches/{matchId}/destroy-card`

Body:

```json
{
  "matchId": "{{matchId}}",
  "playerId": "{{playerId}}",
  "runtimeCardId": "{{runtimeCardId}}"
}
```

Reglas:

- `playerId` debe coincidir con el JWT.
- `runtimeCardId` debe venir del snapshot vivo del match.
- Solo se puede destruir una carta propia actualmente en board.
- La accion esta habilitada aunque no sea el turno local.

## Nuevo metodo de SignalR

- `DestroyCard(DestroyCardRequest request)`

Payload igual al endpoint REST.

## Battle events nuevos/relevantes

- `card_destroyed`
  - intencion explicita de destruir una carta.
  - llega antes de `death`.
- `death`
  - remocion final de la carta del board.
- `card_counterattack`
  - indica el golpe de respuesta del defensor dentro de un duelo carta-vs-carta.

Orden esperado para animar:

1. Consumir `battleEvents` en `sequence` ascendente.
2. Aplicar animacion del evento.
3. Al terminar la cola, reconciliar con el snapshot final.

## Cambio de reglas de combate

En ataques normales carta-vs-carta ahora hay intercambio de dano.

Ejemplo:

- atacante `1/3`
- defensor `1/1`

Resultado correcto:

- defensor muere
- atacante queda `1/2`

Esto no aplica cuando:

- el ataque va directo al heroe
- `fly` hace bypass del defensor y pega al heroe

## Cambio de cliente recomendado

- Agregar accion UI de destruir/sacrificar carta usando `runtimeCardId` del slot ocupado.
- No inventar el resultado localmente.
- Mostrar `card_destroyed`, luego `death`, y finalmente refrescar board desde snapshot.
- Para combate, no asumir dano unilateral.
- Si llega `card_attack` y luego `card_counterattack`, ambas animaciones deben reproducirse antes de reconciliar HP finales.
- El board puede compactarse despues de una destruccion o muerte, asi que el cliente debe confiar en los slots del snapshot nuevo.

## Swagger helper

El helper ahora soporta estas variables persistentes para este flujo:

- `{{runtimeCardId}}`
- `{{playerCardId}}`
- `{{itemTypeKey}}`
- `{{upgradeId}}`
- `{{requirementId}}`

Tambien se agregaron pickers desde API para poblarlas mas rapido.
