# Fase 3 — Kickoff

**Fecha:** 2026-04-05
**Rama de trabajo:** feature/fase3-concurrencia-kickoff
**Origen:** develop sincronizado con origin/develop

## Objetivo

Iniciar la Fase 3 para introducir concurrencia optimista real en el EventStore mediante `SequenceNumber` monotono por aggregate, validacion de `expectedVersion` en write-side y traduccion consistente de conflictos a `CONCURRENCY_CONFLICT` sin romper outbox, auditoria, replay ni compatibilidad con los eventos legacy ya persistidos.

## Decision de arranque

- La Fase 2 queda cerrada y validada.
- La Fase 3 se limita al write-side event-sourced y a su contrato de error observable; no introduce nuevos estados ni eventos de negocio.
- `S-008` se amplia como spec primaria del slice, porque la concurrencia optimista forma parte del contrato canonico del event store, del outbox y del replay.
- El cambio de correlacion de sagas a `TrajectoryId` y el discovery de trayectorias permanecen fuera de esta fase.

## Trazabilidad

- US: `US-016`, `US-018`, `US-019`
- UC: `UC-016`, `UC-018`, `UC-019`
- S: `S-008`, `S-009`
- BDD/TDD: `BDD-008`, `TDD-S-008`, `TDD-S-009`, `RES-TEST-001`, `RES-TEST-002`, `RES-TEST-003`

## Alcance de Fase 3

- agregar `SequenceNumber` monotono por `AggregateId` en el event store
- validar `expectedVersion` en appends write-side antes de persistir eventos nuevos
- rechazar writes stale como `CONCURRENCY_CONFLICT` con semantica HTTP `409`
- preservar comportamiento append-only, atomicidad con outbox y orden determinista para replay de eventos legacy
- cubrir conflicto de version con pruebas unitarias e integracion sobre PostgreSQL real

## Entregables esperados

1. EventStore versionado por aggregate con `SequenceNumber` e indice unico por stream.
2. Repositorios write-side usando `expectedVersion` desde aggregates rehidratados.
3. Traduccion consistente de conflictos stale a `CONCURRENCY_CONFLICT` y `409`.
4. Migracion y replay legacy compatibles con streams ya persistidos.
5. Evidencia automatizada del conflicto de concurrencia y de la no persistencia parcial.

## Restricciones

- Mantener Git Flow: trabajo solo en `feature/*`.
- Mantener docs-first: no implementar sin cerrar `US/UC/S/BDD/TDD` del slice.
- No introducir nuevos estados ni eventos de negocio para resolver concurrencia tecnica.
- No romper el baseline Docker-first ni la atomicidad ya validada entre EventStore, Outbox y Audit.

## Referencias de entrada

- `docs/project/02-as-is-audit/10-FASE-0-DIAGNOSTICO.md`
- `docs/project/02-as-is-audit/16-FASE-2-CIERRE.md`
- `docs/project/05-domain/12-IDEMPOTENCY-AND-CONCURRENCY.md`
- `docs/project/09-data-and-messaging/02-EVENT-STORE-DESIGN.md`
- `docs/project/11-specifications/S-008-event-sourcing-outbox-and-projections.md`
- `docs/project/12-testing/tdd/TDD-S-008-event-sourcing-outbox-and-projections.md`

## Validacion inicial

- `develop` sincronizado con `origin/develop` antes de crear la rama de trabajo.
- Rama `feature/fase3-concurrencia-kickoff` creada desde base sincronizada.
- La cadena minima del slice queda abierta con `US-019`, `UC-019`, `S-008`, `BDD-008` y `TDD-S-008`.

## Riesgos remanentes

- Los streams legacy no contienen `SequenceNumber`; la migracion debe poblarlos con orden determinista por aggregate.
- El conflicto de version debe seguir preservando atomicidad entre EventStore, Outbox y Audit aun cuando el rechazo ocurra en el borde de persistencia.
- La observabilidad end-to-end de `S-009` y el cambio de correlacion de sagas siguen pendientes para slices posteriores.
