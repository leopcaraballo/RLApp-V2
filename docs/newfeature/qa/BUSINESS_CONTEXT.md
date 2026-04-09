# BUSINESS_CONTEXT

Este artefacto existe para alinearse con la Fase 2 del workspace QA.

## Source of truth inside this delivery

- contexto de negocio completo: `01-PRD-QA-PERSPECTIVE.md`
- alcance funcional y riesgos: `04-TEST-PLAN.md`

## Minimal business context

- problema: RLApp tenia flujo fragmentado, lag operativo, reprocesos y baja trazabilidad longitudinal
- solucion: trayectoria unica por paciente con Event Sourcing, CQRS, outbox, event bus y vistas sincronizadas
- actores: paciente, recepcionista, cajero, doctor, administrador, support, sistema
- restricciones: unicidad, idempotencia, control optimista, historial inmutable, RBAC, cumplimiento normativo
