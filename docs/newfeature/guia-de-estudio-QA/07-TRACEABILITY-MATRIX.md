# Traceability Matrix

> **Actualizada**: 2026-04-09 — Basada en evidencia real del repositorio (PRs #51-#54)

## Purpose

Vincular historias, reglas, escenarios, casos de prueba y archivos de test reales para demostrar cobertura funcional y tecnica de la feature.

## Convenciones

- **Automated**: test automatizado existe en el repositorio y pasa en CI
- **Enforced by code**: la regla se cumple por diseno del dominio/infraestructura, no por un test dedicado
- **Backlog**: test identificado como necesario pero no implementado aun

## Matrix

| HU | Criterio de Aceptacion | Reglas | Archivo de Test Real | Metodo(s) de Test | Estado |
|---|---|---|---|---|---|
| HU-01 | CA-01 Unica trayectoria activa | RN-01 | `PatientTrajectoryOrchestratorTests.cs` | `TrackCheckIn_ExistingActiveTrajectory_ThrowsDomainException` | **Automated** |
| HU-01 | CA-02 Impedir duplicados | RN-02 RN-14 | `PatientTrajectoryTests.cs`, `PatientTrajectoryExtendedTests.cs` | `RecordStage_DuplicateStage_ReturnsFalse`, `RecordStage_ExactDuplicate_ReturnsFalse`, `RecordRebuild_DuplicateCorrelation_ReturnsFalse`, `Replay_DuplicateStageEvents_Deduplicates` | **Automated** |
| HU-01 | CA-03 Inicio con etapa valida | RN-05 | `PatientTrajectoryTests.cs`, `PatientTrajectoryExtendedTests.cs` | `Start_ValidInput_RaisesOpenedAndInitialStageEvents`, `Replay_WithoutOpenedEvent_ThrowsDomainException`, `Replay_EmptyEvents_ThrowsDomainException` | **Automated** |
| HU-01 | CA-04 Estado consistente | RN-04 RN-15 | `PatientTrajectoryTests.cs`, `PatientTrajectoryProjectionWriterTests.cs` | `CurrentStage_AfterStart_ReturnsInitialStage`, `Map_ActiveTrajectory_MapsAllProperties`, `RefreshAsync_LoadsAndUpserts` | **Automated** |
| HU-01 | CA-05 Simultaneidad segura | RN-21 RN-22 | — | — | **Enforced by code** (version + unique index); test directo en **Backlog** |
| HU-02 | CA-01 Conservar informacion | RN-12 RN-13 | `PatientTrajectoryOrchestratorTests.cs` | `TrackPaymentValidated_ExistingTrajectory_RecordsStageAndPublishes` (trajectory carries forward all previous data) | **Automated** |
| HU-02 | CA-02 No reprocesar datos | RN-13 | `PatientTrajectoryOrchestratorTests.cs` | `TrackPaymentValidated_DuplicateEvent_DoesNotPublish` | **Automated** |
| HU-02 | CA-03 Una sola accion | RN-09 RN-11 | `PatientTrajectoryTests.cs`, `PatientTrajectoryExtendedTests.cs` | `RecordStage_ReceptionToCashier_Succeeds`, `RecordStage_CashierToConsultation_Succeeds` | **Automated** |
| HU-02 | CA-04 Sin duplicados ante retry | RN-14 | `PatientTrajectoryTests.cs`, `PatientTrajectoryOrchestratorTests.cs` | `RecordStage_DuplicateStage_ReturnsFalse`, `TrackPaymentValidated_DuplicateEvent_DoesNotPublish` | **Automated** |
| HU-02 | CA-05 Flujo permitido | RN-08 RN-09 RN-10 | `PatientTrajectoryTests.cs`, `PatientTrajectoryExtendedTests.cs` | `RecordStage_InvalidTransition_ReceptionToConsultation_Throws`, `RecordStage_ReceptionToConsultation_Throws` | **Automated** |
| HU-03 | CA-01 Mostrar etapa actual | RN-04 RN-24 | `PatientTrajectoryProjectionWriterTests.cs`, integration | `Map_MultipleStages_OrdersChronologically`, `Rebuild_ShouldMaterialize...` | **Automated** |
| HU-03 | CA-02 Actualizacion < 1s | RN-24 RN-25 | `PatientTrajectoryOrchestratorTests.cs` | `PublishBatchAsync` assertions (propagacion) | **Partial automated** (latencia SLA no medida) |
| HU-03 | CA-03 Consistencia visible | RN-23 RN-25 | `PatientTrajectoryOrchestratorTests.cs` | All Track methods verify `UpdateAsync` + `PublishBatchAsync` called | **Automated** |
| HU-03 | CA-04 Multiples pacientes | RN-24 | `PatientTrajectoryIntegrationTests.cs` | `Discover_WithQueueFilter_ReturnsFilteredResults` | **Automated** |
| HU-03 | CA-05 Bajo interrupcion | RN-26 | — | — | **Enforced by code** (outbox + replay); test en **Backlog** |
| HU-04 | CA-01 Historial completo | RN-16 RN-17 | `PatientTrajectoryIntegrationTests.cs` | `Rebuild_ShouldMaterializeTrajectoryProjection...` (verifica 3 stages cronologicos) | **Automated** |
| HU-04 | CA-02 Etapas con timestamps | RN-17 RN-19 | `PatientTrajectoryProjectionWriterTests.cs`, integration | `Map_MultipleStages_OrdersChronologically` (asserts `OccurredAt` order) | **Automated** |
| HU-04 | CA-03 Historial inmutable | RN-18 RN-20 | `PatientTrajectoryExtendedTests.cs` | `RecordStage_OlderTimestamp_ThrowsDomainException`, `RecordStage_OnCompletedTrajectory_Throws` | **Automated** |
| HU-04 | CA-04 Filtros y auditoria | RN-28 RN-30 | `PatientTrajectoryIntegrationTests.cs` | `PatientTrajectoryEndpoints_ShouldEnforceAuthorizationPolicies` (RBAC) | **Partial automated** (audit assertions en **Backlog**) |
| HU-04 | CA-05 Sin degradar rendimiento | RN-24 | — | — | **Backlog** (performance test) |

## Evidencia real de automatizacion por capa

| Capa | Archivos de Test | Tests | Estado |
|---|---|---|---|
| **Domain (Aggregate)** | `PatientTrajectoryTests.cs`, `PatientTrajectoryExtendedTests.cs` | ~30 tests: transiciones, duplicados, cronologia, estados terminales, replay | **Activo en CI** |
| **Application (Orchestrator)** | `PatientTrajectoryOrchestratorTests.cs` | 11 tests: 7 Track methods + idempotencia + late-start | **Activo en CI** |
| **Application (Projection)** | `PatientTrajectoryProjectionWriterTests.cs` | 5 tests: Map, Refresh, Upsert, ordering | **Activo en CI** |
| **Application (Handlers)** | `CommandHandlerTests.cs` | 5 tests: ActivateRoom, CallNextAtCashier | **Activo en CI** |
| **Domain (Queue/Room)** | `WaitingQueueExtendedTests.cs`, `ConsultingRoomExtendedTests.cs` | ~26 tests | **Activo en CI** |
| **Infrastructure** | `ConsultationSagaTests.cs`, `OutboxProcessorTests.cs`, etc. | ~20 tests: saga, outbox, health checks | **Activo en CI** |
| **Integration** | `PatientTrajectoryIntegrationTests.cs`, etc. | 25 tests con Testcontainers (PostgreSQL + RabbitMQ) | **Activo en CI** |
| **Frontend (Unit)** | `authorization.test.ts`, `display-text.test.ts`, `http-client.test.ts`, `env.test.ts` | 38 tests: auth, display, HTTP client, env | **Activo en CI** |
| **Frontend (E2E)** | `role-smoke.mjs` | Smoke de roles con login | Manual trigger |

## Regla de lectura

- **Automated**: existe y pasa en CI (GitHub Actions)
- **Partial automated**: parte del criterio esta cubierta; el espectro completo no
- **Enforced by code**: la regla se cumple por invariantes del dominio o infraestructura
- **Backlog**: test identificado como necesario, no implementado
