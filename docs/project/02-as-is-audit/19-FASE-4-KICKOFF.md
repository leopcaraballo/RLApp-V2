# Fase 4 — Kickoff

**Fecha:** 2026-04-05
**Rama de trabajo:** feature/fase4-sagas-correlacion-kickoff
**Origen:** develop sincronizado con origin/develop

## Objetivo

Iniciar la Fase 4 para migrar la correlacion de sagas y state machines de `Waiting Room` hacia `trajectoryId` como clave longitudinal estable y `correlationId` como rastro operativo, comenzando por `ConsultationSaga`, sin romper flujo de consulta, auditoria, contratos async ni el baseline Docker-first ya validado.

## Decision de arranque

- La Fase 3 queda cerrada y validada sobre el EventStore y su versionado optimista.
- La Fase 4 se limita a la capa de orquestacion, correlacion y observabilidad async; no introduce nuevos estados ni eventos de negocio.
- `S-012` se abre como spec primaria del slice, con `S-007`, `S-009` y `S-011` como anclas secundarias.
- `trajectoryId` pasa a ser la clave longitudinal primaria de las sagas de `Waiting Room`, mientras `correlationId` permanece obligatorio para request, comando, evento, auditoria y diagnostico.
- Los mensajes historicos que aun no contienen `trajectoryId` deben resolverse por puente determinista, sin mutar el payload legacy.

## Trazabilidad

- US: `US-020`
- UC: `UC-020`
- S: `S-012`, `S-007`, `S-009`, `S-011`
- BDD/TDD: `BDD-011`, `TDD-S-012`, `TDD-S-009`

## Alcance de Fase 4

- adaptar `ConsultationSaga` y las futuras state machines longitudinales para correlacionar por `trajectoryId`
- persistir `trajectoryId` y `correlationId` en saga state, auditoria, logs y contratos internos
- definir el puente determinista para mensajes legacy que aun no traen `trajectoryId`
- preservar las transiciones de negocio existentes en consulta, trayectoria y replay
- hacer observable la correlacion longitudinal en retries, timeouts y dead-letter

## Entregables esperados

1. Cadena documental completa del slice con `US-020`, `UC-020`, `S-012`, `BDD-011` y `TDD-S-012`.
2. Regla canonica de correlacion por `trajectoryId` y persistencia de `correlationId` operativo en la saga.
3. Contrato interno minimo para mensajes async longitudinales con ambos identificadores y estrategia de puente legacy.
4. Evidencia automatizada de reutilizacion de saga por trayectoria y de reconstruccion diagnostica por `trajectoryId` y `correlationId`.
5. Objetivos ejecutables identificados en `ConsultationSaga` y `ConsultationState` antes de tocar codigo.

## Restricciones

- Mantener Git Flow: trabajo solo en `feature/*`.
- Mantener docs-first: no tocar codigo sin cerrar `US/UC/S/BDD/TDD` del slice.
- No reemplazar `correlationId`; debe coexistir con `trajectoryId` como rastro operativo.
- No introducir nuevos estados o eventos de negocio para resolver una necesidad de correlacion tecnica.
- No romper el baseline Docker-first ni la observabilidad minima exigida por `S-009`.

## Referencias de entrada

- `docs/project/02-as-is-audit/10-FASE-0-DIAGNOSTICO.md`
- `docs/project/02-as-is-audit/18-FASE-3-CIERRE.md`
- `docs/project/06-application/08-AUDIT-AND-CORRELATION.md`
- `docs/project/07-interfaces-and-contracts/09-INTERNAL-EVENT-CONTRACTS.md`
- `docs/project/11-specifications/S-005-consultation-flow.md`
- `docs/project/11-specifications/S-011-patient-trajectory-aggregate.md`
- `apps/backend/src/RLApp.Adapters.Messaging/Sagas/ConsultationSaga.cs`
- `apps/backend/src/RLApp.Adapters.Messaging/Sagas/ConsultationState.cs`

## Validacion inicial

- `develop` sincronizado con `origin/develop` antes de crear la rama de trabajo.
- Rama `feature/fase4-sagas-correlacion-kickoff` creada desde base sincronizada.
- La cadena minima del slice queda abierta con `US-020`, `UC-020`, `S-012`, `BDD-011` y `TDD-S-012`.
- Se confirma que `ConsultationSaga` actual correlaciona por `PatientId` y genera un nuevo `Guid` por llamado, por lo que el slice tiene un objetivo tecnico verificable.

## Riesgos remanentes

- La saga actual correlaciona por `PatientId` y puede abrir instancias paralelas para una misma trayectoria longitudinal.
- Los contratos async siguen siendo documentados de forma minima y aun no quedan formalizados como artefacto machine-readable.
- La observabilidad operativa todavia no expone de forma uniforme `trajectoryId` en logs, tracing y diagnostico de incidentes.
