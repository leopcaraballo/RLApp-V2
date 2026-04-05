# Fase 2 — Kickoff

**Fecha:** 2026-04-01
**Rama de trabajo:** feature/fase2-trayectoria-paciente-kickoff
**Origen:** develop actualizado desde origin/develop

## Objetivo

Iniciar la Fase 2 para definir el agregado `TrayectoriaPaciente`, su identificador canonico `TrajectoryId` y el replay controlado que permita reconstruir una trayectoria unica por paciente entre recepcion, caja y consulta.

## Decision de arranque

- La Fase 1 queda cerrada y validada.
- La Fase 2 comienza con foco limitado en trazabilidad, modelo de dominio y replay longitudinal del paciente.
- `TrayectoriaPaciente` queda dentro del bounded context `Waiting Room`; este kickoff no introduce un modulo nuevo fuera de la arquitectura objetivo aprobada.
- La migracion de correlacion de sagas a `TrajectoryId` permanece fuera de esta fase y sigue reservada para Fase 4.

## Trazabilidad

- US: `US-012`, `US-016`, `US-018`
- UC: `UC-016`, `UC-018`
- S: `S-008`, `S-009`, `S-011`
- BDD/TDD: `BDD-008`, `BDD-010`, `TDD-S-008`, `TDD-S-009`, `TDD-S-011`, `SEC-TEST-001`, `SEC-TEST-003`, `RES-TEST-001`, `RES-TEST-002`, `RES-TEST-003`

## Alcance de Fase 2

- definir `TrayectoriaPaciente`, `TrajectoryId`, comandos, eventos, invariantes y estados longitudinales
- aclarar ownership del agregado dentro de `Waiting Room`
- documentar consulta protegida y rebuild controlado de trayectoria
- fijar reglas de replay idempotente desde eventos historicos sin reescribir el legado

## Entregables esperados

1. User story, use case, spec, BDD y TDD canonicos de trayectoria paciente.
2. Catálogos de dominio, contratos y matrices de trazabilidad actualizados.
3. Regla de replay longitudinal alineada con Event Sourcing, Outbox y proyecciones persistentes.
4. Riesgos remanentes y criterios de implementacion posterior documentados.

## Restricciones

- Mantener Git Flow: trabajo solo en `feature/*`.
- Mantener docs-first: no abrir implementacion ejecutable hasta cerrar la cadena canonica de Fase 2.
- No reemplazar `correlationId` ni reescribir eventos legacy en este slice.
- No introducir un modulo adicional fuera de `Waiting Room`, `Reporting` y `Audit` ya aprobados.

## Referencias de entrada

- `docs/project/02-as-is-audit/10-FASE-0-DIAGNOSTICO.md`
- `docs/project/02-as-is-audit/14-FASE-1-CIERRE.md`
- `docs/project/05-domain/01-DOMAIN-OVERVIEW.md`
- `docs/project/09-data-and-messaging/09-REBUILD-AND-REPLAY-STRATEGY.md`
- `docs/project/15-traceability/07-FINAL-TRACEABILITY-MATRIX.md`

## Validacion inicial

- `develop` actualizado antes de crear la rama de trabajo.
- Rama `feature/fase2-trayectoria-paciente-kickoff` creada desde base sincronizada.
- No existia previamente una cadena `US/UC/S/BDD/TDD` canonica para `TrayectoriaPaciente`.

## Riesgos remanentes

- El historial legacy no contiene `trajectoryId`; la reconstruccion depende de inferencia determinista por `PatientId`, `QueueId` y orden temporal.
- La implementacion ejecutable del agregado y la migracion real de proyecciones siguen pendientes.
- La correlacion de sagas continua anclada al modelo actual hasta la Fase 4.
