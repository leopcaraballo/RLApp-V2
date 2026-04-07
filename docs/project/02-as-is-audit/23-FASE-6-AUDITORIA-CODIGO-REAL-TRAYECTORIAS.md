# Fase 6 - Auditoria de Estado Actual Basada en Codigo Real

**Fecha:** 2026-04-07
**Rama de trabajo:** feature/code-only-audit-20260406
**Proposito:** registrar el estado real del codigo para planear la iniciativa externa de trayectorias clinicas sin duplicar agregados, eventos, contratos, read models ni canales realtime ya existentes.
**Metodo:** inspeccion estatica de codigo en `apps/backend` y `apps/frontend`. Los hallazgos tecnicos de este informe salen solo del codigo fuente real. La documentacion canonica se usa unicamente para ubicar trazabilidad y gobernanza.

## Traceability

- Cadena canonica mas cercana para trayectoria persistida y reconstruccion: `US-018` / `UC-018` / `S-011` / `BDD-010` / `TDD-S-011`.
- Specs y soporte transversal que ya condicionan el slice actual: `S-008` Event Sourcing, Outbox and Projections; `S-009` Platform NFR.
- Cadena canonica mas cercana para visibilidad operacional sincronizada y realtime mediado por BFF: `US-021` / `UC-021` / `S-013` / `BDD-012` / `TDD-S-013`.
- `newfeature.md` no vive en `/docs/project`; por lo tanto no es un artefacto canonico `US/UC/S`. Este informe documenta evidencia AS-IS y no autoriza implementar el PRD externo sin cerrar primero la trazabilidad faltante.

## Artifacts Reviewed

- `apps/backend/src/RLApp.Domain/Aggregates/PatientTrajectory.cs`
- `apps/backend/src/RLApp.Domain/Common/PatientTrajectoryIdFactory.cs`
- `apps/backend/src/RLApp.Domain/Aggregates/WaitingQueue.cs`
- `apps/backend/src/RLApp.Domain/Common/DomainEntity.cs`
- `apps/backend/src/RLApp.Domain/Common/DomainEvent.cs`
- `apps/backend/src/RLApp.Domain/Common/DomainException.cs`
- `apps/backend/src/RLApp.Domain/Events/PatientTrajectoryEvents.cs`
- `apps/backend/src/RLApp.Domain/Events/QueueEvents.cs`
- `apps/backend/src/RLApp.Domain/Events/ConsultationEvents.cs`
- `apps/backend/src/RLApp.Ports/Inbound/IPatientTrajectoryRepository.cs`
- `apps/backend/src/RLApp.Ports/Outbound/IEventStore.cs`
- `apps/backend/src/RLApp.Ports/Outbound/IProjectionStore.cs`
- `apps/backend/src/RLApp.Adapters.Persistence/Repositories/PatientTrajectoryRepository.cs`
- `apps/backend/src/RLApp.Adapters.Persistence/Repositories/EventStoreRepository.cs`
- `apps/backend/src/RLApp.Adapters.Persistence/Data/Models/EventRecord.cs`
- `apps/backend/src/RLApp.Adapters.Persistence/Persistence/EfPersistenceSession.cs`
- `apps/backend/src/RLApp.Application/Commands/ApplicationCommands.cs`
- `apps/backend/src/RLApp.Application/Queries/ApplicationQueries.cs`
- `apps/backend/src/RLApp.Application/Handlers/RegisterPatientArrivalHandler.cs`
- `apps/backend/src/RLApp.Application/Handlers/PatientTrajectoryHandlers.cs`
- `apps/backend/src/RLApp.Application/Services/PatientTrajectoryOrchestrator.cs`
- `apps/backend/src/RLApp.Application/Services/PatientTrajectoryProjectionWriter.cs`
- `apps/backend/src/RLApp.Adapters.Http/Controllers/ReceptionController.cs`
- `apps/backend/src/RLApp.Adapters.Http/Controllers/PatientTrajectoriesController.cs`
- `apps/backend/src/RLApp.Adapters.Http/Middleware/GlobalExceptionMiddleware.cs`
- `apps/backend/src/RLApp.Adapters.Messaging/Consumers/PatientTrajectoryConsumer.cs`
- `apps/backend/src/RLApp.Api/Consumers/SignalRNotificationConsumer.cs`
- `apps/backend/src/RLApp.Api/Hubs/NotificationHub.cs`
- `apps/backend/src/RLApp.Api/Program.cs`
- `apps/frontend/src/features/trajectory/patient-trajectory-console.tsx`
- `apps/frontend/src/hooks/use-operational-realtime.ts`
- `apps/frontend/src/app/api/realtime/operations/route.ts`
- `apps/frontend/src/app/api/proxy/[...path]/route.ts`
- `apps/frontend/src/services/rlapp-api.ts`
- `apps/frontend/src/services/http-client.ts`
- `apps/frontend/src/types/api.ts`

## Hallazgos de Codigo Real

### 1. Aggregate root principal

- Ya existe un aggregate root longitudinal: `PatientTrajectory`.
- Nombre real del archivo y clase: `apps/backend/src/RLApp.Domain/Aggregates/PatientTrajectory.cs`.
- Propiedades reales del aggregate:
  - `Id`
  - `PatientId`
  - `QueueId`
  - `CurrentState`
  - `OpenedAt`
  - `ClosedAt`
  - `Stages`
  - `CorrelationIds`
  - `Version` heredada de `DomainEntity`
- Metodos reales del aggregate:
  - `Start(...)`
  - `RecordStage(...)`
  - `Complete(...)`
  - `Cancel(...)`
  - `RecordRebuild(...)`
- El aggregate no usa un enum para su estado; usa strings constantes.
- El identificador real de trayectoria no es GUID aleatorio. `PatientTrajectoryIdFactory.Create(queueId, patientId, occurredAt)` genera un ID deterministico con formato `TRJ-{queue}-{patient}-{timestamp}`.
- Existe un value object embebido para hitos longitudinales: `TrajectoryStage` con `OccurredAt`, `Stage`, `SourceEvent`, `SourceState` y `CorrelationId`.

### 2. Estados y nombres reales del flujo

- No se encontro un enum `JourneyState`, `TrajectoryState` o `Status` para la trayectoria en `apps/backend/src`.
- El estado actual de la trayectoria usa estas constantes string exactas:
  - `TrayectoriaActiva`
  - `TrayectoriaFinalizada`
  - `TrayectoriaCancelada`
- Las etapas longitudinales canonizadas en codigo son:
  - `Recepcion`
  - `Caja`
  - `Consulta`
- El aggregate tambien persiste `SourceState` con nombres reales que hoy sirven como contexto operacional:
  - `EnEsperaTaquilla`
  - `EnEsperaConsulta`
  - `Finalizado`
  - `CanceladoPorPago`
  - `CanceladoPorAusencia`
- En el canal realtime de staff aparecen otros labels de estado, pero son payloads de invalidacion y no el estado persistido del aggregate:
  - `Waiting`
  - `Called`
  - `InConsultation`
  - `Completed`
- Conclusion operativa: hoy no existe un `InProgress` explicito en el aggregate. El equivalente funcional es `TrayectoriaActiva`.

### 3. Eventos reales encontrados

- No se encontraron clases `INotification` en `apps/backend/src`.
- El modelo de eventos del backend se basa en clases que heredan de `DomainEvent`.
- Campos base reales de `DomainEvent`:
  - `EventType`
  - `OccurredAt`
  - `CorrelationId`
  - `AggregateId`
  - `TrajectoryId` opcional

Eventos reales de trayectoria:

- `PatientTrajectoryOpened`
- `PatientTrajectoryStageRecorded`
- `PatientTrajectoryCompleted`
- `PatientTrajectoryCancelled`
- `PatientTrajectoryRebuilt`

Eventos reales de cola y consulta que alimentan la trayectoria:

- `WaitingQueueCreated`
- `PatientCheckedIn`
- `PatientCalledAtCashier`
- `PatientPaymentValidated`
- `PatientPaymentPending`
- `PatientAbsentAtCashier`
- `PatientCancelledByPayment`
- `ConsultingRoomActivated`
- `ConsultingRoomDeactivated`
- `PatientClaimedForAttention`
- `PatientCalled`
- `PatientAttentionCompleted`
- `PatientAbsentAtConsultation`
- `PatientCancelledByAbsence`

Observaciones relevantes:

- La trayectoria no nace por un evento generico `StageAdvanced`; nace y evoluciona a partir de eventos operacionales concretos.
- `PatientTrajectoryOrchestrator` traduce eventos de `WaitingQueue` a mutaciones de `PatientTrajectory`.
- `SignalRNotificationConsumer` solo publica a realtime cuatro eventos operacionales: `PatientCheckedIn`, `PatientCalled`, `PatientClaimedForAttention` y `PatientAttentionCompleted`.

### 4. Repositorios, puertos y persistencia

Puertos reales para trayectoria:

- `IPatientTrajectoryRepository`
  - `GetByIdAsync(...)`
  - `FindActiveAsync(patientId, queueId, ...)`
  - `AddAsync(...)`
  - `UpdateAsync(...)`
- `IEventStore`
  - `SaveAsync(...)`
  - `SaveBatchAsync(...)`
  - `GetEventsByAggregateIdAsync(...)`
  - `GetEventsByDateRangeAsync(...)`
  - `GetAllAsync(...)`
- `IProjectionStore`
  - `UpsertAsync(...)`
  - `FindPatientTrajectoriesAsync(...)`
  - `GetPatientTrajectoryAsync(...)`

Implementaciones reales:

- `PatientTrajectoryRepository` rehidrata el aggregate por replay de eventos.
- `EventStoreRepository` serializa eventos a la tabla `EventStore`.
- `PatientTrajectoryConsumer` refresca la proyeccion `PatientTrajectory` desde el aggregate rehidratado.
- `PatientTrajectoryProjectionWriter` mapea el aggregate a un read model persistido.

Hallazgo importante:

- El repositorio de trayectoria ya existe. No hace falta crear `ITrajectoryRepository`, `IPatientFlowRepository` ni otra abstraccion paralela para empezar una feature longitudinal.

### 5. Event Sourcing, Outbox y CQRS reales

- Si existe un event store explicito. No es un snapshot-only model.
- Tabla real de eventos: `EventStore`.
- Registro real por evento: `EventRecord` con `AggregateId`, `SequenceNumber`, `EventType`, `CorrelationId`, `Payload`, `OccurredAt`.
- `EventStoreRepository` mantiene un mapa explicito de tipos (`EventTypeMap`) para deserializar eventos conocidos.
- El outbox existe y sigue activo en la arquitectura; la trayectoria publica sus domain events via `IEventPublisher` y `PublishBatchAsync(...)`.
- El backend usa `MediatR` con `IRequest` y `IRequestHandler`; no se encontro una jerarquia propia `ICommandHandler` / `IQueryHandler`.

Handlers y queries reales del slice de trayectoria:

- `DiscoverPatientTrajectoriesHandler`
- `GetPatientTrajectoryHandler`
- `RebuildPatientTrajectoriesHandler`
- `DiscoverPatientTrajectoriesQuery`
- `GetPatientTrajectoryQuery`
- `RebuildPatientTrajectoriesCommand`

Endpoints reales ya disponibles:

- `GET /api/patient-trajectories`
- `GET /api/patient-trajectories/{trajectoryId}`
- `POST /api/patient-trajectories/rebuild`

### 6. Arquitectura actual observable en codigo

- El backend esta separado en estas capas y modulos reales:
  - `RLApp.Domain`
  - `RLApp.Application`
  - `RLApp.Ports`
  - `RLApp.Adapters.Http`
  - `RLApp.Adapters.Persistence`
  - `RLApp.Adapters.Messaging`
  - `RLApp.Api`
  - `RLApp.Infrastructure`
- El frontend tambien ya tiene un BFF real:
  - proxy same-origin hacia backend en `apps/frontend/src/app/api/proxy/[...path]/route.ts`
  - stream same-origin en `apps/frontend/src/app/api/realtime/operations/route.ts`
  - consola de trayectoria en `apps/frontend/src/features/trajectory/patient-trajectory-console.tsx`
- Conclusion: el sistema ya implementa una arquitectura hexagonal-ish con trayectoria event-sourced y visibilidad projection-first. No seria correcto modelar la futura iniciativa como si el repo aun no tuviera aggregate longitudinal, event store ni consulta protegida de trayectoria.

### 7. Concurrencia, duplicados e idempotencia

Concurrencia optimista real:

- `DomainEntity` ya expone `Version`.
- `PatientTrajectoryRepository` persiste con `SaveBatchAsync(events, trajectory.Version, ...)`.
- `EventStoreRepository` compara `expectedVersion` contra la version actual del stream.
- `EventRecord` define un indice unico real por `(AggregateId, SequenceNumber)`.
- `EfPersistenceSession` detecta colisiones de secuencia y las traduce a `DomainException.ConcurrencyConflict(...)`.
- `GlobalExceptionMiddleware` devuelve `409 Conflict` para conflictos optimistas.

Protecciones contra duplicados reales:

- `PatientTrajectory.HasDuplicateStage(...)` evita volver a grabar la misma etapa con la misma tupla `(Stage, SourceEvent, SourceState, OccurredAt, CorrelationId)`.
- `CorrelationIds` evita duplicados con comparacion ordinal.
- `PatientTrajectory.Complete(...)` y `Cancel(...)` retornan `false` si el estado final ya fue aplicado.

Idempotencia real:

- Se confirmo soporte explicito de `IdempotencyKey` en `RebuildPatientTrajectoriesCommand`.
- `RebuildPatientTrajectoriesHandler` usa `ConcurrentDictionary` para bloquear rebuilds concurrentes con la misma combinacion `scope|idempotencyKey`.
- Varios controllers reciben el header `X-Idempotency-Key`, pero en `ApplicationCommands.cs` solo se encontro modelado explicitamente para `RebuildPatientTrajectoriesCommand`.
- Hallazgo verificable: `ReceptionController` recibe `X-Idempotency-Key`, pero `RegisterPatientArrivalCommand` no tiene propiedad `IdempotencyKey`; por tanto el check-in no implementa idempotencia de comando de la misma forma que el rebuild.

### 8. Frontend, visibilidad y realtime reales

- Ya existe una UI dedicada para trayectoria: `PatientTrajectoryConsole`.
- La UI ya soporta:
  - discovery por `patientId` y `queueId` opcional
  - consulta directa por `trajectoryId`
  - visualizacion de `currentState`, `correlationIds` y `stages`
  - rebuild controlado
- El browser no se conecta directo al backend con JWT expuesto. El frontend usa un BFF same-origin.
- El flujo realtime real es:
  1. el navegador abre `GET /api/realtime/operations` como SSE (`EventSource`)
  2. la route handler del frontend crea una `HubConnection` hacia `/hubs/notifications`
  3. esa conexion se suscribe a `JoinDashboardGroup`, `JoinQueueGroup` y `JoinTrajectoryGroup`
  4. los mensajes SignalR se traducen a invalidaciones SSE para el browser
- `useOperationalRealtime` trata el canal como invalidacion y resync, no como fuente de verdad persistida.

### 9. Resumen de nombres reales que no conviene duplicar

Reutilizar estos nombres existentes antes de introducir equivalentes nuevos:

- Aggregate: `PatientTrajectory`
- Aggregate auxiliar existente: `WaitingQueue`
- ID factory: `PatientTrajectoryIdFactory`
- Repository port: `IPatientTrajectoryRepository`
- Event store port: `IEventStore`
- Projection store port: `IProjectionStore`
- Orquestador: `PatientTrajectoryOrchestrator`
- Projection writer: `PatientTrajectoryProjectionWriter`
- Query handlers: `DiscoverPatientTrajectoriesHandler`, `GetPatientTrajectoryHandler`
- Rebuild handler: `RebuildPatientTrajectoriesHandler`
- UI principal: `PatientTrajectoryConsole`
- Hub backend: `NotificationHub`

No se encontro en codigo real:

- `Trajectory`
- `PatientJourney`
- `ClinicalTrajectory`
- `JourneyState` o `TrajectoryState` como enum
- `INotification` como modelo de eventos del backend

## Blocking Gaps

- `newfeature.md` todavia no tiene una cadena canonica propia dentro de `/docs/project`; hoy solo puede anclarse parcialmente a `US-018` / `S-011` y a `US-021` / `S-013`.
- El sistema ya usa estados string distintos segun capa (`TrayectoriaActiva`, `Finalizado`, `Completed`, `InConsultation`). Cualquier feature nueva que introduzca estados clinicos o labels operacionales debe normalizar primero ese vocabulario en docs canonicos para no romper contratos ni dashboards.
- La idempotencia esta implementada de forma fuerte en rebuild, pero no se pudo confirmar el mismo nivel de enforcement para los comandos operacionales normales; al menos en admision el header existe y el comando no lo modela.
- `IPatientTrajectoryRepository` vive en `RLApp.Ports/Inbound`, lo que puede inducir drift conceptual al extender el modulo aunque el comportamiento real sea de persistencia.

## Decision

- La nueva iniciativa no debe arrancar creando un aggregate paralelo llamado `Trajectory`, `PatientJourney` o similar.
- El punto de extension correcto del backend actual es `PatientTrajectory` junto con `WaitingQueue`, `IEventStore`, `IPatientTrajectoryRepository`, `PatientTrajectoryOrchestrator` y las proyecciones existentes.
- Para visibilidad de staff y tiempo real, el punto de extension correcto ya es el BFF same-origin del frontend (`/api/proxy` y `/api/realtime/operations`) con SignalR encapsulado del lado servidor.
- Antes de implementar el PRD externo, hay que traducir su alcance a una cadena canonica nueva o ampliar la existente en `/docs/project`. Este informe solo confirma que el codigo real ya cubre buena parte del terreno base y donde estan los nombres exactos.

## Validation

- Se valido el estado actual por lectura directa de archivos fuente reales en backend y frontend.
- Se usaron busquedas exactas para confirmar ausencia de `INotification` y de enums de estado para trayectoria en `apps/backend/src`.
- No se ejecutaron builds, pruebas ni Docker Compose porque el objetivo de este corte fue auditoria documental basada en codigo real, no cambio de runtime.
- No se usaron documentos Markdown como fuente de hallazgos tecnicos del sistema actual; solo como capa de trazabilidad y gobernanza para ubicar este artefacto.

## Residual Risks

- El nombre del feature externo sugiere una plataforma mas amplia que la base actual; si se intenta implementar directamente contra `newfeature.md` sin convertirlo a `US/UC/S/BDD/TDD`, se mezclara TO-BE con AS-IS y se perdera trazabilidad.
- Hay drift de naming entre estados persistidos, `SourceState` y payloads realtime. Eso puede generar integraciones inconsistentes si la nueva feature asume una taxonomia unica que hoy no existe en codigo.
- El canal realtime del frontend depende de una mediacion BFF que convierte SignalR a SSE. Cualquier cambio que conecte el browser directo al hub puede romper el boundary de seguridad y la sesion same-origin ya implementada.
- La presencia de `X-Idempotency-Key` en varios controllers puede inducir una falsa sensacion de cobertura uniforme; el uso fuerte comprobado en este audit es el rebuild de trayectorias, no todo el write-side operacional.
