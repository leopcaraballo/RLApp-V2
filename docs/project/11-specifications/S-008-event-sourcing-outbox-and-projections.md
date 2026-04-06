# S-008 Event Sourcing Outbox And Projections

## Purpose

Definir persistencia append-only, versionado por agregado, outbox, publicacion asincrona, proyecciones persistentes y rebuild controlado.

## Traceability

- User stories: `US-011`, `US-016`, `US-018`, `US-019`
- Use cases: `UC-016`, `UC-018`, `UC-019`
- Tests: `BDD-008`, `TDD-S-008`, `RES-TEST-001`, `RES-TEST-002`, `RES-TEST-003`

## Scope

- event store append-only
- outbox transaccional
- topologia RabbitMQ para consumidores internos
- proyecciones persistentes de monitor, queue-state, next-turn, history, dashboard y trayectoria paciente
- rebuild y replay controlados

## Required behavior

- Todo cambio de estado de dominio se persiste como evento append-only con metadata versionada.
- La version del agregado debe proteger concurrencia optimista.
- Cada evento persistido debe recibir `SequenceNumber` monotono y sin huecos dentro de su `AggregateId`.
- Todo append write-side debe validar `expectedVersion` contra la ultima version persistida del agregado antes de asignar nuevos `SequenceNumber`.
- Si `expectedVersion` ya no coincide, el append debe rechazarse como `CONCURRENCY_CONFLICT` sin persistir eventos parciales, outbox parcial ni auditoria de exito.
- Outbox y persistencia de eventos deben compartir la misma transaccion logica.
- Los consumidores deben procesar mensajes de forma idempotente.
- Los mensajes de outbox con tipo desconocido o payload invalido deben moverse a almacenamiento dead-letter con causa y correlationId en vez de perderse silenciosamente.
- El rebuild debe rehacer proyecciones desde el event store usando checkpoints persistidos.
- El rebuild de trayectoria debe poblar o reconciliar read-models longitudinales sin reemitir side effects operativos.
- La falla temporal de RabbitMQ no puede causar perdida de eventos.

## Contracts

- Internal event contract minimo: `eventId`, `eventName`, `aggregateId`, `aggregateVersion`, `occurredAt`, `correlationId`, `schemaVersion`
- Rebuild endpoint documentado: `POST /api/v1/waiting-room/{queueId}/rebuild`

## State and event impact

- Soporta todos los estados `ST-001` a `ST-012` y eventos `EV-001` a `EV-019`.
- No define reglas de negocio nuevas; garantiza persistencia, replay y proyeccion confiable de las ya existentes.

## Validation criteria

- Append y load del event store deben respetar versionado por agregado.
- El event store debe rechazar writes stale cuando el `expectedVersion` ya no coincide con la version persistida.
- La migracion de `SequenceNumber` debe preservar el orden cronologico determinista del stream legacy por `AggregateId`.
- Outbox debe publicar de forma confiable y sin perdida.
- Los mensajes no recuperables del outbox deben quedar en cuarentena observable para analisis operativo.
- Rebuild debe ser idempotente y recuperar proyecciones consistentes.
- Rebuild de trayectoria debe recuperar trayectorias longitudinales consistentes a partir del historial legacy.
