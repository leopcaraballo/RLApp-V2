# Fase 1 — Kickoff

**Fecha:** 2026-04-01
**Rama de trabajo:** feature/fase1-outbox-stabilization
**Origen:** develop actualizado desde origin/develop

## Objetivo

Iniciar la Fase 1 para estabilizar la propagacion de eventos del write-side hacia outbox, bus y proyecciones, reduciendo latencia operativa y preparando la decision tecnica entre polling configurable y mecanismo tipo LISTEN/NOTIFY.

## Decision de arranque

- La Fase 0 queda cerrada y validada.
- La Fase 1 comienza con foco limitado en estabilizacion de propagacion y observabilidad.
- No se amplia alcance a rediseño de agregados en esta rama inicial.

## Trazabilidad

- US: `US-011`, `US-016`
- UC: `UC-016`
- S: `S-008`, `S-009`
- BDD/TDD: `BDD-008`, `TDD-S-008`, `TDD-S-009`, `RES-TEST-001`, `RES-TEST-002`, `RES-TEST-003`

## Alcance de Fase 1

- parametrizar el intervalo del `OutboxProcessor`
- agregar medicion de latencia end-to-end del flujo outbox -> bus -> proyeccion
- definir criterio de decision para evaluar reemplazo por `LISTEN/NOTIFY`
- conservar consistencia con Event Sourcing, Outbox y proyecciones persistentes

## Entregables esperados

1. Configuracion explicita del worker de outbox.
2. Telemetria minima para publish duration, backlog y tiempo de propagacion.
3. Evidencia de validacion tecnica de la estrategia elegida.
4. Riesgos remanentes y siguiente corte para Fase 2.

## Restricciones

- Mantener Git Flow: trabajo solo en `feature/*`.
- Mantener docs-first: si falta contrato o criterio de validacion, actualizar primero `/docs/project`.
- No mezclar en esta rama los cambios estructurales de `TrayectoriaPaciente` ni versionado de Event Store.

## Referencias de entrada

- `docs/project/02-as-is-audit/10-FASE-0-DIAGNOSTICO.md`
- `docs/project/02-as-is-audit/12-FASE-0-VERIFICATION.md`
- `docs/project/11-specifications/S-008-event-sourcing-outbox-and-projections.md`
- `docs/project/11-specifications/S-009-platform-nfr.md`
- `docs/project/15-traceability/07-FINAL-TRACEABILITY-MATRIX.md`

## Validacion inicial

- `develop` actualizado desde `origin/develop` antes de crear la rama.
- Rama de trabajo creada desde base actualizada.
- Fase 0 documentada, verificada y disponible como insumo.

## Riesgos remanentes

- La latencia observada en runtime real aun debe medirse sobre codigo ejecutable.
- La alternativa `LISTEN/NOTIFY` puede requerir cambios de infraestructura no validados aun.
- El control de concurrencia por versionado queda fuera de esta fase inicial.
