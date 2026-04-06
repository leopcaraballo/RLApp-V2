# Fase 4 — Cierre

**Fecha:** 2026-04-06
**Rama de trabajo original:** feature/fase4-sagas-correlacion-kickoff
**Estado:** completada

## Objetivo cumplido

Fase 4 cierra la correlacion longitudinal principal de `ConsultationSaga` por `trajectoryId`, mantiene `correlationId` como rastro operativo, formaliza el contrato async machine-readable para eventos de consulta y deja la persistencia durable de saga como baseline ejecutable.

## Trazabilidad

- US: `US-020`
- UC: `UC-020`
- S: `S-012`, `S-007`, `S-009`, `S-011`
- BDD/TDD: `BDD-011`, `TDD-S-012`, `TDD-S-009`

## Decision tecnica

- `trajectoryId` queda como clave longitudinal primaria para la correlacion de la saga de consulta.
- `correlationId` permanece obligatorio en request, comando, evento, auditoria y diagnostico operativo.
- La persistencia ejecutable de la saga deja de depender de `InMemoryRepository` y usa almacenamiento durable sobre PostgreSQL.
- Los eventos longitudinales ejecutables de consulta quedan descritos tambien en `apps/backend/docs/api/asyncapi.yaml`, alineados con el payload runtime.

## Entregables cerrados

1. `ConsultationSaga` correlaciona por `trajectoryId` cuando el payload lo trae o cuando el puente legacy puede resolverlo deterministicamente.
2. El estado durable de la saga persiste `trajectoryId`, `correlationId` operativo y metadatos minimos de diagnostico.
3. El baseline ejecutable elimina `InMemoryRepository` como opcion por defecto fuera de pruebas y probes aislados.
4. El contrato async de `EV-011`, `EV-012` y `EV-013` queda formalizado como artefacto machine-readable.

## Validacion

- Evidencia automatizada de correlacion y persistencia durable en pruebas de infraestructura e integracion del backend.
- Artefacto `apps/backend/docs/api/asyncapi.yaml` alineado con la emision runtime observada para eventos longitudinales de consulta.
- La matriz final de trazabilidad ya refleja Fase 4 como materializada y deja explicitado el siguiente hueco funcional.

## Riesgos remanentes

- El discovery operacional de trayectorias sigue pendiente cuando el operador no conoce `trajectoryId` de antemano.
- La observabilidad end-to-end mas amplia sobre servicios ejecutables todavia requiere automatizacion de evidencia y thresholds transversales.
- La consola de trayectoria sigue dependiendo de un `trajectoryId` conocido para consultar la proyeccion completa.

## Siguiente corte propuesto

- Abrir el siguiente slice sobre discovery operacional de trayectorias desde proyecciones persistidas.
- Mantener `S-009` como ancla secundaria para logging estructurado y diagnostico del lookup sin ampliar el alcance a una iniciativa completa de observabilidad.
