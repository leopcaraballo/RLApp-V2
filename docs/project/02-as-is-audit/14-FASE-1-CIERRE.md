# Fase 1 — Cierre

**Fecha:** 2026-04-01
**Rama de trabajo:** feature/fase1-outbox-config-telemetry
**Estado:** completada

## Objetivo cumplido

Fase 1 cierra la estabilizacion de propagacion del write-side hacia outbox, bus y proyecciones para entornos locales y Docker. El corte deja configuracion explicita del worker, telemetria minima, fallback local sin broker externo, cuarentena dead-letter para mensajes no recuperables y evidencia automatizada de validacion.

## Trazabilidad

- US: `US-011`, `US-016`
- UC: `UC-016`
- S: `S-008`, `S-009`
- BDD/TDD: `BDD-008`, `TDD-S-008`, `TDD-S-009`, `RES-TEST-001`, `RES-TEST-002`, `RES-TEST-003`

## Decision tecnica

- Se mantiene como estrategia base de Fase 1 el worker de outbox con `polling interval` configurable, `batch size` configurable y señal post-commit con polling como fallback.
- Para entorno local y Docker sin broker externo, el outbox puede despachar en proceso para sostener proyecciones y readiness funcional.
- Los mensajes de outbox con tipo desconocido o payload invalido ya no se marcan como procesados de forma silenciosa: se mueven a dead-letter storage con `correlationId`, causa y payload original.
- `LISTEN/NOTIFY` no se adopta en Fase 1. Queda como opcion de Fase 1.5 o decision de arranque de Fase 2 solo si se cumple alguno de estos criterios:
  - `outbox propagation delay p95 > 3000 ms` en operacion nominal.
  - `outbox backlog` sostenido por encima de la capacidad del batch bajo carga nominal.
  - el costo de polling en PostgreSQL deja de ser aceptable para la ventana operativa.
  - se requiere wake-up cross-process y la señal en proceso deja de ser suficiente.

## Entregables cerrados

1. Worker de outbox parametrizado y validado.
2. Telemetria minima para backlog, publish duration, propagation delay y dead-letter count.
3. Validacion automatizada de fallback local del outbox y readiness sin broker externo.
4. Riesgos remanentes y criterio de siguiente corte documentados.

## Validacion

- `RLApp.Tests.Unit`: `51/51` pruebas en verde.
- `RLApp.Tests.Integration`: `7/7` pruebas en verde.
- El slice previo ya habia validado despliegue funcional en Docker con `db`, `backend` y `frontend` saludables.

## Riesgos remanentes

- El event store sigue sin `SequenceNumber` ni `expectedVersion`; eso queda fuera de Fase 1.
- La correlacion de sagas sigue anclada al modelo actual; no se ha iniciado aun el rediseño de `TrayectoriaPaciente`.
- Persisten warnings de nulabilidad en proyectos de dominio, aplicacion y mensajeria.
- `LISTEN/NOTIFY` sigue pendiente de medicion comparativa real antes de adoptarse.

## Siguiente corte propuesto

- Abrir Fase 2 solo despues de preparar spec, TDD y kickoff del agregado `TrayectoriaPaciente`.
- Mantener dentro de Fase 1 cualquier ajuste menor de observabilidad o endurecimiento operativo que no cambie el modelo de dominio.
