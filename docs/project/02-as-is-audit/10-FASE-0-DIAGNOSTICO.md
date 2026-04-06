# Fase 0 — Diagnóstico Ejecutivo

**Fecha:** 2026-04-01
**Rama:** feature/semana6-vuelo-manual

## Resumen ejecutivo

Fase 0 concluye con un diagnóstico técnico del código fuente y del pipeline de eventos del backend. Se validó la presencia de Event Sourcing, Outbox Pattern, Sagas (MassTransit), proyecciones y tests de integración que prueban atomicidad de persistencia. Identificamos riesgos operativos y de diseño que impactan la latencia, trazabilidad y consistencia; además proponemos mitigaciones inmediatas (POC) y una hoja de ruta por fases para evolucionar hacia VitalSync.

## Alcance

- Inspección de agregados y eventos del dominio.
- Revisión del Event Store y Outbox (persistencia, publicador, worker).
- Revisión de sagas, consumidores (MassTransit) y SignalR notifications.
- Revisión de proyecciones/read models y tests de integración relevantes.

## Hallazgos clave

- OutboxProcessor (worker en proceso) usa polling cada ~5s → provoca latencia perceptible en la UI y en sincronización de read models.
- Outbox marca tipos de evento desconocidos como "procesados" (warning) → riesgo de pérdida silenciosa de mensajes.
- EventStore no tiene `SequenceNumber`/expectedVersion → falta control de concurrencia optimista para writes de agregados.
- No existe un agregado central de `TrayectoriaPaciente` (trayectoria única por paciente) → la lógica está fragmentada en `WaitingQueue` y sagas, dificultando invariantes transaccionales y trazabilidad.
- Sagas correlacionan por `PatientId` y generan nuevos GUIDs para sagaId en algunos flujos → riesgo de desalineación entre trayectoria y saga.
- Proyecciones muestran inconsistencias en claves (TurnId vs PatientId) y algunos upserts son simplistas.
- Tests de integración confirman que la persistencia es atómica para el flujo de check-in (buena cobertura en rollback/atomicidad).

## Riesgos más urgentes

- Alta: latencia de propagación de eventos (Outbox polling), posible pérdida silenciosa de eventos desconocidos.
- Media: falta de número de secuencia en EventStore (concurrency), correlación inadecuada en Sagas.
- Baja: desacoples locales en proyecciones y naming mismatches.

## Recomendaciones inmediatas (POC) — Prioridad Alta

1. POC-A: Hacer configurable el intervalo del `OutboxProcessor` y reducirlo temporalmente a 200–500ms; añadir métricas y logs (latencia end-to-end, publish duration, attempt counts). Archivos objetivo: `apps/backend/src/RLApp.Infrastructure/BackgroundServices/OutboxProcessor.cs` y configuración DI/`appsettings`.
2. POC-B (paralela): Probar LISTEN/NOTIFY en PostgreSQL para eliminar polling; diseño con fallback a polling configurable. Esto valida la reducción de latencia sin cambios disruptivos en la lógica de publicación.
3. Cambiar comportamiento frente a tipos desconocidos: mover a tabla DLQ/dead-letter y emitir alerta/telemetría en vez de marcar como procesado.

## Recomendaciones estructurales (Hoja de Ruta)

- Fase 1 — Estabilizar propagación: implementar POC-A → medir y decidir LISTEN/NOTIFY.
- Fase 2 — Trayectoria paciente: diseñar y scaffold del agregado `TrayectoriaPaciente` (comandos, eventos, invariantes); migración de eventos históricos para poblar read-models de trayectoria.
- Fase 3 — Concurrencia: añadir `SequenceNumber`/expectedVersion a `EventRecord` y aplicar checks en `EventStoreRepository` (optimistic concurrency); actualizar pruebas.
- Fase 4 — Sagas & Correlación: cambiar correlación de sagas a `TrajectoryId`/CorrelationId y adaptar `ConsultationSaga` y demás state machines.
- Fase 5 — Proyecciones y QA: alinear keying en proyecciones, rehacer rebuilds y añadir dashboards de observabilidad.

## Pruebas y validación

- Mantener TDD: cada PR debe incluir tests unitarios que cubran invariantes del agregado y tests de integración que validen atomicidad (EventStore + Outbox + Audit).
- Añadir pruebas de integración para POC de LISTEN/NOTIFY si se opta por esa vía.

## Artefactos y evidencia

- Informe: este archivo: docs/project/02-as-is-audit/10-FASE-0-DIAGNOSTICO.md
- Referencias clave (lectura recomendada):
  - apps/backend/src/RLApp.Domain/Aggregates/WaitingQueue.cs
  - apps/backend/src/RLApp.Adapters.Persistence/Repositories/EventStoreRepository.cs
  - apps/backend/src/RLApp.Infrastructure/BackgroundServices/OutboxProcessor.cs
  - apps/backend/src/RLApp.Adapters.Messaging/Sagas/ConsultationSaga.cs

## Siguientes pasos propuestos (acciones concretas)

1. Implementar POC-A (Outbox polling configurable + métricas) y abrir PR en la rama actual. Tiempo estimado: 2–4 horas de trabajo para patch + pruebas básicas.
2. Si POC-A mejora latencia: planear POC-B (LISTEN/NOTIFY) como segunda PR.
3. Diseñar `TrayectoriaPaciente` (spec + TDD) y preparar migración de eventos (pruebas de replay/rebuild de proyecciones).

---
_Estado:_ Fase 0 finalizada; diagnóstico y prioridades documentadas. Esperando orden para comenzar PR‑POC(s).
