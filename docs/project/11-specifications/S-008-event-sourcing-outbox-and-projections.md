# S-008 Event Sourcing Outbox And Projections

## Purpose

Definir persistencia append-only, versionado por agregado, outbox, publicacion asincrona, proyecciones persistentes y rebuild controlado.

## Traceability

- User stories: `US-011`, `US-016`
- Use cases: `UC-016`
- Tests: `BDD-008`, `TDD-S-008`, `RES-TEST-001`, `RES-TEST-002`, `RES-TEST-003`

## Scope

- event store append-only
- outbox transaccional
- topologia RabbitMQ para consumidores internos
- proyecciones persistentes de monitor, queue-state, next-turn, history y dashboard
- rebuild y replay controlados

## Required behavior

- Todo cambio de estado de dominio se persiste como evento append-only con metadata versionada.
- La version del agregado debe proteger concurrencia optimista.
- Outbox y persistencia de eventos deben compartir la misma transaccion logica.
- Los consumidores deben procesar mensajes de forma idempotente.
- Los mensajes de outbox con tipo desconocido o payload invalido deben moverse a almacenamiento dead-letter con causa y correlationId en vez de perderse silenciosamente.
- El rebuild debe rehacer proyecciones desde el event store usando checkpoints persistidos.
- La falla temporal de RabbitMQ no puede causar perdida de eventos.

## Contracts

- Internal event contract minimo: `eventId`, `eventName`, `aggregateId`, `aggregateVersion`, `occurredAt`, `correlationId`, `schemaVersion`
- Rebuild endpoint documentado: `POST /api/v1/waiting-room/{queueId}/rebuild`

## State and event impact

- Soporta todos los estados `ST-001` a `ST-009` y eventos `EV-001` a `EV-014`.
- No define reglas de negocio nuevas; garantiza persistencia, replay y proyeccion confiable de las ya existentes.

## Validation criteria

- Append y load del event store deben respetar versionado por agregado.
- Outbox debe publicar de forma confiable y sin perdida.
- Los mensajes no recuperables del outbox deben quedar en cuarentena observable para analisis operativo.
- Rebuild debe ser idempotente y recuperar proyecciones consistentes.
