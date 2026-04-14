# server-api

Backend autoritativo en C# para el juego de cartas 1v1.

## Qué resuelve

- matchmaking casual, privado y ranked
- estado ready de ambos jugadores
- inicio de match solo cuando ambos están listos
- desconexión, abandono y ventana de rejoin
- motor de duelo autoritativo en el servidor
- SignalR para snapshots en tiempo real
- REST para mazos, crear/unirse a partida, salud y administración básica
- base para observadores y torneos

## Arquitectura recomendada

- **Unity cliente**: render, UX, input, VFX, audio
- **server-api**: autoridad del duelo, validación, matchmaking, snapshots, resultados
- **DB**: PostgreSQL para cuentas, mazos, rating, historial
- **cache/backplane**: Redis para scale-out de SignalR y coordinación entre nodos
- **auth**: JWT/OIDC

## Flujo recomendado

1. el cliente autentica y obtiene JWT
2. crea o busca partida por REST
3. el backend responde `matchId`, `roomCode`, `reconnectToken`
4. el cliente abre SignalR y se asocia al match
5. ambos marcan ready
6. el servidor inicia el seed y baraja mazos
7. todas las jugadas se validan en el servidor
8. el servidor empuja snapshots a cada jugador y uno redacted a observadores

## Qué viene listo

- proyecto ASP.NET Core 8
- SignalR Hub
- servicios in-memory para prototipo serio
- diseño listo para reemplazar por Redis/Postgres sin cambiar el contrato público

## Para llevarlo a producción de verdad

- reemplazar stores in-memory por PostgreSQL + Redis
- guardar resultados/rating en DB
- agregar worker para colas ranked por MMR/rango/región
- agregar antifraude de mazos firmados y replay logs
- separar matchmaking y game-state en procesos distintos si sube mucho el volumen
