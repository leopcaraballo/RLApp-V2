# Fase 3 — Cierre

**Fecha:** 2026-04-05
**Rama de trabajo:** feature/fase3-concurrencia-kickoff
**Estado:** completada

## Objetivo cumplido

Fase 3 cierra la incorporacion de concurrencia optimista real en el EventStore con `SequenceNumber` monotono por `AggregateId`, validacion de `expectedVersion` en el write-side, traduccion de writes stale a `CONCURRENCY_CONFLICT` con semantica HTTP `409` y preservacion de atomicidad entre EventStore, Outbox y Audit para los aggregates ya existentes, incluida `TrayectoriaPaciente`.

## Trazabilidad

- US: `US-016`, `US-018`, `US-019`
- UC: `UC-016`, `UC-018`, `UC-019`
- S: `S-008`, `S-009`
- BDD/TDD: `BDD-008`, `TDD-S-008`, `TDD-S-009`, `RES-TEST-001`, `RES-TEST-002`, `RES-TEST-003`

## Decision tecnica

- El control de concurrencia se resuelve en la frontera de persistencia usando version del agregado y un indice unico por stream, en vez de dispersar checks ad hoc por handler.
- La migracion de `SequenceNumber` rellena streams legacy con orden determinista por `AggregateId`, `OccurredAt` e `Id`, preservando replay y rebuild.
- El contrato observable del slice se normaliza como `CONCURRENCY_CONFLICT` y `409`, manteniendo el error funcional independiente de la base de datos subyacente.
- Los repositorios event-sourced rehidratan y persisten `Version` en el agregado para reutilizar el mismo contrato en `WaitingQueue`, `ConsultingRoom` y `TrayectoriaPaciente`.

## Entregables cerrados

1. `EventRecord` y el schema de persistencia quedan versionados con `SequenceNumber` monotono e indice unico por stream.
2. `EventStoreRepository` valida `expectedVersion`, asigna secuencia sin huecos y rehidrata eventos en orden determinista.
3. La capa application/http traduce conflictos stale a `CONCURRENCY_CONFLICT` y `409` sin perder `correlationId` ni semantica de error.
4. La evidencia automatizada cubre conflicto real de concurrencia, rollback del writer perdedor y monotonia de la secuencia sobre PostgreSQL.
5. El baseline Docker-first queda revalidado con `db`, `backend` y `frontend` saludables despues del cambio de persistencia.

## Validacion

- Pruebas focalizadas de concurrencia (`PersistenceIntegrityIntegrationTests` y `CommandResultTests`): `5/5` en verde.
- `dotnet test RLApp.slnx`: `68/68` pruebas en verde.
- `docker compose --profile backend --profile frontend up --build -d`: `db`, `backend` y `frontend` saludables.
- `curl http://127.0.0.1:5094/health`: backend saludable con `npgsql`, `ProjectionLag` y `Self` en estado `Healthy`.
- `curl -I http://127.0.0.1:3000/api/health`: frontend responde `200 OK`.

## Riesgos remanentes

- La validacion legacy cubre orden determinista y conflicto stale sobre la base real, pero aun no modela volumen historico masivo ni restores operativos de larga ventana.
- `S-009` sigue necesitando automatizacion end-to-end de metricas, tracing y thresholds sobre el runtime ejecutable completo.
- El cambio de correlacion de sagas a `TrajectoryId` y el discovery operacional de trayectorias permanecen fuera de Fase 3.

## Siguiente corte propuesto

- Abrir Fase 4 sobre correlacion de sagas a `TrajectoryId`, contratos async observables y consolidacion de trazabilidad longitudinal.
- Mantener como baseline obligatorio el arranque por Docker Compose y el smoke de salud del stack antes de cualquier PR posterior que toque persistencia o contratos internos.
