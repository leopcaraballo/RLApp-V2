# S-007 Reporting And Audit

## Purpose

Definir dashboard operativo, timeline por turno, uso de `correlationId` y auditoria inmutable para diagnostico y supervision.

## Traceability

- User stories: `US-011`, `US-012`
- Use cases: `UC-015`
- Tests: `BDD-007`, `TDD-S-007`

## Scope

- dashboard de tiempos, ocupacion, ausencias y salud de proyecciones
- reconstruccion de timeline por `correlationId`
- almacenamiento inmutable de auditoria operativa

## Required behavior

- Todo comando mutante debe dejar rastro con actor, accion, entidad, timestamp, resultado y `correlationId`.
- El dashboard se construye desde proyecciones persistentes, no desde event replay en cada consulta.
- La reconstruccion de timeline debe poder correlacionar request, evento, publicacion y proyeccion.
- La auditoria debe permanecer inmutable.

## Contract dependencies

- Contratos canonicos: `/docs/project/07-interfaces-and-contracts/12-REPORTING-AND-AUDIT-CONTRACTS.md`.
- Queries canonicas: `GET /api/v1/operations/dashboard`, `GET /api/v1/audit/timeline/{correlationId}`.
- Los modelos de salida deben incluir al menos `correlationId`, actor, accion, timestamp, entidad y resultado.

## State and event impact

- Consume eventos operativos de `EV-001` a `EV-014`.
- No introduce nuevas transiciones del catalogo `ST-001` a `ST-009`; observa y reconstruye transiciones existentes.

## Validation criteria

- Debe ser posible reconstruir el timeline de un turno a partir de `correlationId`.
- El dashboard debe mostrar salud de proyecciones y metricas operativas clave.
- Ningun registro de auditoria puede modificarse o sobrescribirse.
