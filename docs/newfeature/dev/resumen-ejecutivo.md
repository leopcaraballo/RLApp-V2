# Feature Architecture Analysis — Orquestador de Trayectorias Clínicas Sincronizadas

> **Documento técnico de análisis profundo**
> Fecha: 8 de abril de 2026
> Sistema: RLApp-V2
> Feature: Orquestador de Trayectorias Clínicas Sincronizadas

---

## 1. Resumen Ejecutivo

### Qué hace la feature

La feature implementa un **orquestador de trayectorias clínicas** que centraliza el recorrido completo de cada paciente a través de todas las etapas del proceso clínico — desde la admisión en recepción, pasando por caja, hasta la finalización de la consulta médica. Cada paciente genera una **trayectoria única e inmutable** que actúa como fuente de verdad del estado global del paciente, eliminando fragmentación, duplicidad de registros y falta de visibilidad.

La implementación abarca:

- **Dominio**: Aggregate Root `PatientTrajectory` con máquina de estados (Recepción → Caja → Consulta), invariantes de negocio y eventos de dominio.
- **Orquestación**: `PatientTrajectoryOrchestrator` que reacciona a eventos de otros aggregates (colas, consultorios) para mantener la trayectoria sincronizada.
- **CQRS completo**: Escrituras vía Event Store + lecturas vía Projection Store optimizado.
- **Tiempo real**: Propagación de eventos de trayectoria vía MassTransit → SignalR → SSE → UI.
- **Reconstrucción**: Capacidad de rebuild de trayectorias a partir del histórico completo de eventos.
- **Frontend**: Consola de trayectorias con búsqueda, detalle longitudinal, y reconstrucción (dry-run y real).

### Nivel de complejidad

**Alto**. La feature involucra Event Sourcing con concurrencia optimista, CQRS con proyecciones persistentes, una saga MassTransit, propagación de eventos en tiempo real a través de 4 capas (dominio → outbox → bus → SignalR → SSE → UI), y reconstrucción determinista de agregados desde streams de eventos históricos.

### Tipo de sistema

**Monolito modular event-driven con CQRS y Event Sourcing**. El backend es un único proceso .NET desplegable que internamente implementa una arquitectura hexagonal estricta con separación de módulos por assembly. La mensajería opera sobre RabbitMQ (o InMemory para desarrollo) mediante MassTransit.

### Nivel de madurez técnica

**Alto**. El sistema demuestra manejo sofisticado de Event Sourcing con concurrencia optimista, Outbox Pattern transaccional, Sagas persistentes (EF Core + PostgreSQL), idempotencia, telemetría OpenTelemetry, health checks granulares, y separación hexagonal de puertos y adaptadores.

---

## 2. Arquitectura Implementada

### Tipo de arquitectura

El sistema implementa simultáneamente múltiples paradigmas arquitectónicos:

| Paradigma | Implementación |
|-----------|---------------|
| **Arquitectura Hexagonal (Ports & Adapters)** | 6 assemblies separados por capa: `Domain`, `Ports`, `Application`, `Adapters.Http`, `Adapters.Persistence`, `Adapters.Messaging` |
| **CQRS** | Escrituras vía `IEventStore` + `IEventPublisher` (outbox); lecturas vía `IProjectionStore` con read models persistidos en PostgreSQL |
| **Event Sourcing** | Todos los agregados (`PatientTrajectory`, `WaitingQueue`, `ConsultingRoom`) persisten su estado como streams de eventos inmutables en `EventRecord` |
| **Event-Driven** | MassTransit (RabbitMQ/InMemory) como bus de eventos; consumers asíncronos para proyecciones, dashboard, waiting room y SignalR |
| **Monolito Modular** | Un único proceso desplegable (API + Background Services) con módulos desacoplados por assembly y contratos vía interfaces en `Ports` |

### Componentes principales

```
┌────────────────────────────────────────────────────────────────────┐
│                        FRONTEND (Next.js 16)                       │
│  ┌──────────┐ ┌─────────┐ ┌────────┐ ┌─────────┐ ┌────────────┐  │
│  │  Login   │ │Recepción│ │  Caja  │ │ Médico  │ │Trayectoria │  │
│  │  Form    │ │ Console │ │Console │ │ Console │ │  Console   │  │
│  └──────────┘ └─────────┘ └────────┘ └─────────┘ └────────────┘  │
│                      │          │          │            │          │
│             ┌────────┴──────────┴──────────┴────────────┘          │
│             ▼                                                      │
│  ┌─────────────────────┐  ┌──────────────────────────────────┐    │
│  │  API Proxy Layer    │  │  SSE EventSource (real-time)     │    │
│  │  /api/proxy/[...]   │  │  /api/realtime/operations        │    │
│  └─────────┬───────────┘  └──────────────┬───────────────────┘    │
│            │                              │                        │
└────────────┼──────────────────────────────┼────────────────────────┘
             │ HTTP (Bearer JWT)            │ SignalR → SSE bridge
             ▼                              ▼
┌────────────────────────────────────────────────────────────────────┐
│                       BACKEND (ASP.NET Core)                       │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────┐      │
│  │              API Layer (RLApp.Api)                       │      │
│  │  Program.cs · NotificationHub · SignalRNotificationConsumer    │
│  └──────────────────────────┬──────────────────────────────┘      │
│                              │                                     │
│  ┌──────────────────────────┴──────────────────────────────┐      │
│  │         HTTP Adapter (RLApp.Adapters.Http)              │      │
│  │  7 Controllers · GlobalExceptionMiddleware · RBAC       │      │
│  └──────────────────────────┬──────────────────────────────┘      │
│                              │ MediatR                             │
│  ┌──────────────────────────┴──────────────────────────────┐      │
│  │          Application Layer (RLApp.Application)          │      │
│  │  13 Commands · 6 Queries · Handlers · Orchestrator      │      │
│  │  IdempotencyGuard · CorrelationResolver · ProjectionWriter    │
│  └──────────────────────────┬──────────────────────────────┘      │
│                              │                                     │
│  ┌──────────────────────────┴──────────────────────────────┐      │
│  │             Domain Layer (RLApp.Domain)                  │      │
│  │  PatientTrajectory · WaitingQueue · ConsultingRoom      │      │
│  │  StaffUser · DomainEvent · Value Objects                │      │
│  └──────────────────────────┬──────────────────────────────┘      │
│                              │                                     │
│  ┌──────────────────────────┴──────────────────────────────┐      │
│  │             Ports Layer (RLApp.Ports)                    │      │
│  │  IEventStore · IProjectionStore · IEventPublisher       │      │
│  │  IPatientTrajectoryRepository · IAuditStore             │      │
│  └──────────────────────────┬──────────────────────────────┘      │
│                              │                                     │
│  ┌───────────────┬──────────┴──────────┬───────────────────┐      │
│  │  Persistence  │     Messaging       │  Infrastructure   │      │
│  │  EF Core +    │  MassTransit +      │  OutboxProcessor  │      │
│  │  PostgreSQL   │  RabbitMQ           │  Health Checks    │      │
│  │  Event Store  │  4 Consumers        │  OpenTelemetry    │      │
│  │  Outbox       │  ConsultationSaga   │  JWT + PBKDF2     │      │
│  │  Projections  │                     │  Polly Resilience │      │
│  └───────────────┴─────────────────────┴───────────────────┘      │
└────────────────────────────────────────────────────────────────────┘
             │                    │                   │
             ▼                    ▼                   ▼
      ┌──────────┐        ┌──────────┐        ┌──────────┐
      │PostgreSQL│        │ RabbitMQ │        │Prometheus│
      │  (DB)    │        │  (Bus)   │        │(Metrics) │
      └──────────┘        └──────────┘        └──────────┘
```

### Bases de datos y almacenamiento

| Componente | Tecnología | Propósito |
|-----------|-----------|----------|
| **Event Store** | PostgreSQL (`event_store` tabla) | Almacena todos los eventos de dominio como registros inmutables con `AggregateId`, `SequenceNumber`, `Payload` (JSONB) |
| **Outbox** | PostgreSQL (`outbox_messages` tabla) | Garantiza atomicidad entre persistencia de estado y publicación de eventos |
| **Projection Store** | PostgreSQL (`patient_trajectory_views`, `waiting_room_monitor_views`, `operations_dashboard_views`) | Read models desnormalizados para consultas rápidas |
| **Saga State** | PostgreSQL (`ConsultationState` tabla) | Persistencia del estado de la saga de consulta |
| **Audit Log** | PostgreSQL (`audit_logs` tabla) | Registro de auditoría de todas las operaciones |
| **Staff Users** | PostgreSQL (`staff_users` tabla) | Tabla CRUD tradicional para credenciales |

---

## 3. Flujo End-to-End de la Feature

El flujo completo de un paciente desde la admisión hasta la finalización genera una trayectoria con 3-5 etapas. A continuación se detalla paso a paso:

### Paso 1: Entrada — Registro del paciente en recepción

1. **UI**: El recepcionista completa el formulario en `reception-console.tsx` con los datos del paciente (`patientId`, `patientName`, `queueId`, `priority`).
2. **Frontend**: La petición viaja como `POST /api/proxy/reception/register` → el proxy de Next.js inyecta el Bearer token JWT y los headers `X-Correlation-Id` + `X-Idempotency-Key`.
3. **Controller**: `ReceptionController.Register()` construye un `RegisterPatientArrivalCommand` y lo envía vía MediatR.

### Paso 2: Validaciones

1. **IdempotencyGuard**: El handler verifica que el `IdempotencyKey` no esté activo en el `ConcurrentDictionary` en memoria. Si es duplicado, retorna `DUPLICATE_COMMAND`.
2. **Dominio (WaitingQueue)**: El aggregate `WaitingQueue` valida:
   - La cola está abierta (`IsOpen = true`)
   - El paciente no está ya en la cola (`PatientIds.Contains`)
3. Si la cola no existe, se crea automáticamente (auto-provisioning).

### Paso 3: Lógica de dominio

1. **WaitingQueue.CheckInPatient()**: Agrega al paciente a `PatientIds` y emite el evento `PatientCheckedIn`.
2. **PatientTrajectoryOrchestrator.TrackCheckInAsync()**: Reacciona al evento `PatientCheckedIn`:
   - Verifica que no exista una trayectoria activa para ese paciente en esa cola vía `FindActiveAsync()` (consulta el Projection Store).
   - Genera un `TrajectoryId` determinista: `TRJ-{QUEUE-NORMALIZED}-{PATIENT-NORMALIZED}-{yyyyMMddHHmmssfff}`.
   - Crea el aggregate `PatientTrajectory.Start()` con la primera etapa: `Recepcion` / `EnEsperaTaquilla`.
   - Emite dos eventos: `PatientTrajectoryOpened` + `PatientTrajectoryStageRecorded`.

### Paso 4: Persistencia (transaccional)

1. **WaitingQueueRepository**: Persiste los eventos del queue en el Event Store con concurrencia optimista (`SequenceNumber` único por aggregate).
2. **PatientTrajectoryRepository**: Persiste los eventos de la trayectoria en el Event Store con `expectedVersion` para control de concurrencia.
3. **OutboxEventPublisher**: Todos los eventos de ambos aggregates se insertan atómicamente en la tabla `outbox_messages` como parte de la misma transacción EF Core.
4. **EfPersistenceSession.CommitAsync()**: Un único `SaveChangesAsync()` persiste: event store records + outbox messages + audit log — todo atómico.

### Paso 5: Publicación de eventos

1. **OutboxProcessor** (background service): Cada 500ms (configurable), consulta `outbox_messages` sin `ProcessedAt`, deserializa el JSON al tipo concreto usando el assembly de dominio, y despacha cada mensaje a MassTransit.
2. **MassTransitOutboxMessageDispatcher**: Envía el evento al bus (RabbitMQ o InMemory) usando `IPublishEndpoint.Publish(payload, eventType)`.
3. El `OutboxMessage.ProcessedAt` se marca con la fecha de procesamiento. Si falla, incrementa `AttemptCount`; si el tipo es desconocido, mueve a `outbox_dead_letter_messages`.

### Paso 6: Consumo de eventos (asíncrono)

Los mensajes se distribuyen en paralelo a múltiples consumers registrados en MassTransit:

1. **PatientTrajectoryConsumer**: Recibe `PatientTrajectoryOpened`, recarga el aggregate desde el Event Store (replay completo), y actualiza la proyección en `IProjectionStore`.
2. **WaitingRoomMonitorConsumer**: Recibe `PatientCheckedIn`, actualiza la proyección `WaitingRoomMonitorView`.
3. **DashboardConsumer**: Recibe `PatientCheckedIn`, actualiza las métricas del `OperationsDashboardView`.
4. **QueueStateConsumer**: Recibe eventos de cola, reconstruye el estado operacional de la cola.
5. **SignalRNotificationConsumer**: Recibe todos los eventos (13 tipos), construye payloads normalizados y los publica a los grupos SignalR correspondientes (`dashboard`, `queue-{queueId}`, `trajectory-{trajectoryId}`).
6. **ConsultationSaga**: Escucha `PatientCalled`, `PatientAttentionCompleted`, `PatientAbsentAtConsultation` para coordinar la máquina de estados de la consulta.

### Paso 7: Actualización de vistas (read models)

1. Las proyecciones persistidas en PostgreSQL (`patient_trajectory_views`, `waiting_room_monitor_views`, `operations_dashboard_views`) se actualizan como JSON serializado. Cada consumer hace `IProjectionStore.UpsertAsync()`.
2. El frontend recibe las invalidaciones vía SSE y ejecuta `queryClient.invalidateQueries()` para refrescar las queries de React Query.

### Paso 8: Respuesta al usuario

1. El handler original retorna un `CommandResult<RegisterPatientResultDto>` con `QueueId`, `TurnId`, `PatientId`, `RegisteredAt`.
2. El controller retorna HTTP 200 con el resultado.
3. El frontend muestra un `response-card--success` con el resultado.
4. Simultáneamente, el SSE stream notifica a otros clientes (dashboard, sala de espera, display público) de la nueva llegada.

### Etapas subsiguientes

El mismo patrón se repite para cada etapa del flujo:

| Etapa | Trigger | Orchestrator Method | Stage | SourceState |
|-------|---------|-------------------|-------|-------------|
| **Recepción** | `PatientCheckedIn` | `TrackCheckInAsync` | `Recepcion` | `EnEsperaTaquilla` |
| **Caja** | `PatientPaymentValidated` | `TrackPaymentValidatedAsync` | `Caja` | `EnEsperaConsulta` |
| **Llamado consulta** | `PatientCalled` | `TrackConsultationCalledAsync` | `Consulta` | `LlamadoConsulta` |
| **En consulta** | `PatientClaimedForAttention` (started) | `TrackConsultationStartedAsync` | `Consulta` | `EnConsulta` |
| **Finalización** | `PatientAttentionCompleted` | `TrackCompletionAsync` | `Consulta` | `Finalizado` |
| **Cancelación caja** | `PatientAbsentAtCashier` | `TrackCashierAbsenceAsync` | `Caja` | `CanceladoPorAusencia` |
| **Cancelación consulta** | `PatientAbsentAtConsultation` | `TrackConsultationAbsenceAsync` | `Consulta` | `CanceladoPorAusencia` |

---

## 4. Diseño de Dominio (DDD)

### Aggregates

#### PatientTrajectory (Aggregate Root)

El aggregate central de la feature. Representa el flujo completo de un paciente a través de las etapas clínicas.

**Archivo**: `RLApp.Domain/Aggregates/PatientTrajectory.cs`

```
PatientTrajectory
├── Id: string (TRJ-{QUEUE}-{PATIENT}-{timestamp})
├── PatientId: string
├── QueueId: string
├── CurrentState: string [TrayectoriaActiva | TrayectoriaFinalizada | TrayectoriaCancelada]
├── OpenedAt: DateTime
├── ClosedAt: DateTime?
├── Stages: List<TrajectoryStage>
├── CorrelationIds: List<string>
├── Version: int (control de concurrencia optimista)
│
├── Start(trajectoryId, patientId, queueId, stage, sourceEvent, sourceState, occurredAt, correlationId)
├── RecordStage(stage, sourceEvent, sourceState, occurredAt, correlationId) → bool
├── Complete(stage, sourceEvent, sourceState, occurredAt, correlationId) → bool
├── Cancel(sourceEvent, sourceState, reason, occurredAt, correlationId) → bool
├── RecordRebuild(scope, occurredAt, correlationId) → bool
└── Replay(events) → PatientTrajectory [static, reconstrucción sin reflexión]
```

#### WaitingQueue (Aggregate Root)

Gestiona la cola de espera de pacientes, las asignaciones a consultorios y los estados de atención.

**Archivo**: `RLApp.Domain/Aggregates/WaitingQueue.cs`

```
WaitingQueue
├── Id: string
├── Name: string
├── PatientIds: List<string>
├── PatientRoomAssignments: Dictionary<string, string>
├── PatientAttentionStates: Dictionary<string, string>
├── IsOpen: bool
│
├── Create() / Open() / Close()
├── CheckInPatient() → emite PatientCheckedIn
├── CallPatientAtCashier() → emite PatientCalledAtCashier
├── MarkPaymentValidated() → emite PatientPaymentValidated
├── AssignPatientToRoom() → emite PatientClaimedForAttention
├── CallPatient() → emite PatientCalled
├── StartPatientAttention() → emite PatientClaimedForAttention (started)
├── CompletePatientAttention() → emite PatientAttentionCompleted
└── MarkPatientAbsent() → emite PatientAbsentAtConsultation
```

#### ConsultingRoom (Aggregate Root)

Gestiona el ciclo de vida de los consultorios médicos.

**Archivo**: `RLApp.Domain/Aggregates/ConsultingRoom.cs`

#### StaffUser (Aggregate Root)

Gestiona credenciales y roles del personal clínico.

**Archivo**: `RLApp.Domain/Aggregates/StaffUser.cs`

### Entidades

- **TrajectoryStage** (Value Object embebido): Representa una etapa dentro de la trayectoria con `OccurredAt`, `Stage`, `SourceEvent`, `SourceState`, `CorrelationId`.

### Value Objects

- **Email** (`RLApp.Domain/ValueObjects/Email.cs`): Validación de formato de email inmutable.
- **StaffRole** (`RLApp.Domain/ValueObjects/StaffRole.cs`): Enumeración de roles (Receptionist, Cashier, Doctor, Supervisor, Support).

### Eventos de dominio

| Evento | Aggregate origen | Descripción |
|--------|-----------------|-------------|
| `PatientTrajectoryOpened` | PatientTrajectory | Trayectoria creada al registrar paciente |
| `PatientTrajectoryStageRecorded` | PatientTrajectory | Nueva etapa registrada en la trayectoria |
| `PatientTrajectoryCompleted` | PatientTrajectory | Trayectoria marcada como finalizada |
| `PatientTrajectoryCancelled` | PatientTrajectory | Trayectoria cancelada (ausencia) |
| `PatientTrajectoryRebuilt` | PatientTrajectory | Trayectoria reconstruida desde histórico |
| `PatientCheckedIn` | WaitingQueue | Paciente registrado en cola |
| `PatientCalledAtCashier` | WaitingQueue | Paciente llamado a caja |
| `PatientPaymentValidated` | WaitingQueue | Pago validado |
| `PatientPaymentPending` | WaitingQueue | Pago pendiente |
| `PatientCalled` | WaitingQueue | Paciente llamado a consulta |
| `PatientClaimedForAttention` | WaitingQueue | Paciente asignado/en consulta |
| `PatientAttentionCompleted` | WaitingQueue | Atención finalizada |
| `PatientAbsentAtCashier` | WaitingQueue | Ausencia en caja |
| `PatientAbsentAtConsultation` | WaitingQueue | Ausencia en consulta |
| `WaitingQueueCreated` | WaitingQueue | Cola creada |
| `ConsultingRoomActivated` | ConsultingRoom | Consultorio activado |
| `ConsultingRoomDeactivated` | ConsultingRoom | Consultorio desactivado |

### Invariantes implementados

| Código | Invariante | Implementación |
|--------|-----------|---------------|
| RN-01 | Un paciente debe tener una única trayectoria activa | `PatientTrajectoryOrchestrator.TrackCheckInAsync()` valida vía `FindActiveAsync()` |
| RN-03 | Un paciente no puede estar en múltiples etapas simultáneamente | `AllowedTransitions` en `PatientTrajectory` + flujo secuencial de stages |
| RN-08 | No hay transición sin estado previo | `EnsureValidTransition()` verifica `CurrentStage` contra `AllowedTransitions` |
| RN-09/10 | Transiciones deben respetar el flujo | `AllowedTransitions`: `"" → Recepcion → Caja → Consulta` |
| RN-11 | Transiciones atómicas | Single aggregate boundary + EF Core transacción |
| RN-14 | Idempotencia | `HasDuplicateStage()` + `IdempotencyGuard` + `CorrelationIds` tracking |
| RN-18 | Historial inmutable | Event Store append-only, no UPDATE/DELETE |
| RN-19 | Orden cronológico | `EnsureChronologicalOrder()` valida `occurredAt >= lastStage.OccurredAt` |
| RN-22 | Control optimista | `SequenceNumber` unique constraint + `expectedVersion` en `EventStoreRepository.SaveBatchAsync()` |

### Máquina de estados de la trayectoria

```
                 ┌──────────────────┐
                 │   (sin estado)    │
                 └────────┬─────────┘
                          │ Start()
                          ▼
               ┌──────────────────────┐
               │  TrayectoriaActiva   │
               │                      │
               │  Stages:             │
               │  ┌──────────────┐    │
               │  │  Recepcion   │────┼──────────┐
               │  └──────────────┘    │          │
               │         │            │          │
               │         ▼            │          │ Cancel()
               │  ┌──────────────┐    │          │
               │  │    Caja      │────┼──────────┤
               │  └──────────────┘    │          │
               │         │            │          ▼
               │         ▼            │  ┌───────────────────┐
               │  ┌──────────────┐    │  │TrayectoriaCancelada│
               │  │  Consulta    │    │  │  (ClosedAt set)   │
               │  │ (LlamadoConsulta│ │  └───────────────────┘
               │  │  EnConsulta)  │   │
               │  └──────┬───────┘    │
               │         │            │
               └─────────┼────────────┘
                          │ Complete()
                          ▼
               ┌──────────────────────┐
               │TrayectoriaFinalizada │
               │   (ClosedAt set)     │
               └──────────────────────┘
```

---

## 5. Patrones de Diseño Utilizados

### 5.1 Event Sourcing

**Dónde aparece**: Todo el dominio. Los aggregates `PatientTrajectory`, `WaitingQueue` y `ConsultingRoom` persisten su estado como streams de eventos inmutables.

**Cómo se implementa**:

- `DomainEntity` base acumula eventos en `_unraisedEvents` vía `RaiseDomainEvent()`.
- `EventStoreRepository.SaveBatchAsync()` serializa cada `DomainEvent` como JSON y lo inserta en `EventRecord` con `AggregateId` y `SequenceNumber` auto-incrementante.
- `PatientTrajectory.Replay()` reconstruye el aggregate completo a partir de su stream de eventos sin usar reflexión — usa pattern matching explícito (`switch` sobre tipos de evento).
- El constraint `(AggregateId, SequenceNumber) UNIQUE` en PostgreSQL implementa la concurrencia optimista a nivel de base de datos.

**Qué problema resuelve**: Trazabilidad total (RN-16 a RN-20), inmutabilidad del historial, capacidad de reconstrucción (RNF-21), y auditoría completa.

### 5.2 CQRS (Command Query Responsibility Segregation)

**Dónde aparece**: Separación completa entre escrituras y lecturas.

**Cómo se implementa**:

- **Escrituras**: `Command<T>` → MediatR handler → `IEventStore.SaveBatchAsync()` + `IEventPublisher.PublishBatchAsync()` (outbox).
- **Lecturas**: `Query<T>` → MediatR handler → `IProjectionStore.FindPatientTrajectoriesAsync()` / `GetPatientTrajectoryAsync()`.
- Los consumers (PatientTrajectoryConsumer, DashboardConsumer, etc.) actualizan las proyecciones asíncronamente al consumir eventos del bus.
- Las proyecciones son JSON serializado en tablas PostgreSQL separadas (`PatientTrajectoryView`, `WaitingRoomMonitorView`, `OperationsDashboardView`).

**Qué problema resuelve**: Permite escalar lecturas independientemente de escrituras (RNF-07), optimiza queries sin afectar el modelo de dominio, y soporta múltiples vistas materializadas desde el mismo stream de eventos.

### 5.3 Outbox Pattern

**Dónde aparece**: `OutboxEventPublisher` + `OutboxProcessor` + `OutboxMessage`.

**Cómo se implementa**:

- `OutboxEventPublisher` implementa `IEventPublisher`. En lugar de publicar directamente al bus, inserta mensajes en la tabla `outbox_messages` dentro de la misma transacción EF Core que persiste los eventos y el audit log.
- `OutboxProcessor` (hosted BackgroundService) hace polling cada 500ms (configurable 100-5000ms), lee batches de hasta 50 mensajes (configurable 1-500), deserializa, y despacha a MassTransit.
- Si el tipo de evento es desconocido o el JSON es inválido, el mensaje se mueve a `outbox_dead_letter_messages`.

**Qué problema resuelve**: Garantiza atomicidad entre la persistencia del estado y la publicación de eventos. Elimina el problema de "dual write" donde la escritura en la DB podría ocurrir sin la publicación del evento o viceversa.

### 5.4 Repository Pattern

**Dónde aparece**: 5 repositorios registrados como `IPatientTrajectoryRepository`, `IWaitingQueueRepository`, `IConsultingRoomRepository`, `IStaffUserRepository`, `IAuditStore`.

**Cómo se implementa**:

- `PatientTrajectoryRepository` es un repositorio híbrido: usa el `IProjectionStore` para consultas rápidas (`FindActiveAsync` busca en la proyección) y el `IEventStore` para la reconstrucción completa del aggregate (`GetByIdAsync` hace replay de eventos).
- Los repositorios operan sobre la transacción EF Core compartida. El `EfPersistenceSession.CommitAsync()` ejecuta un único `SaveChangesAsync()`.

**Qué problema resuelve**: Abstracción del mecanismo de persistencia, permitiendo que el dominio no conozca la infraestructura.

### 5.5 Mediator Pattern (via MediatR)

**Dónde aparece**: Todos los controllers envían commands/queries vía `IMediator.Send()`.

**Cómo se implementa**: MediatR registra automáticamente todos los handlers del assembly `RLApp.Application`. Cada command tiene un handler dedicado (`RegisterPatientArrivalHandler`, `FinishConsultationHandler`, etc.).

**Qué problema resuelve**: Desacoplamiento completo entre la capa HTTP y la lógica de aplicación. Los controllers no conocen los handlers ni sus dependencias.

### 5.6 Saga Pattern (State Machine)

**Dónde aparece**: `ConsultationSaga` + `ConsultationState` (MassTransit Automatonymous).

**Cómo se implementa**:

- La saga es una máquina de estados con 3 estados: `WaitingForPatient` → `InConsultation` / `Expired`.
- La correlación se realiza vía SHA256 hash del `TrajectoryId` o `PatientId`.
- Los eventos `PatientCalled`, `PatientAttentionCompleted`, `PatientAbsentAtConsultation` disparan transiciones.
- El estado se persiste en PostgreSQL vía EF Core EntityFramework saga repository.

**Qué problema resuelve**: Coordinación de procesos de larga duración distribuidos, garantizando que la consulta siga un flujo válido incluso ante fallos parciales.

### 5.7 Observer / Pub-Sub

**Dónde aparece**: Todos los consumers de MassTransit son subscribers de eventos de dominio.

**Cómo se implementa**: MassTransit registra consumers (`PatientTrajectoryConsumer`, `WaitingRoomMonitorConsumer`, `DashboardConsumer`, `QueueStateConsumer`, `SignalRNotificationConsumer`) que se suscriben a tipos de eventos específicos. El bus distribuye cada evento en paralelo a todos los consumers registrados para ese tipo.

**Qué problema resuelve**: Desacoplamiento entre productores y consumidores de eventos. Un nuevo consumer puede agregarse sin modificar el productor.

### 5.8 Factory Pattern

**Dónde aparece**: `PatientTrajectoryIdFactory.Create()`.

**Cómo se implementa**: Genera IDs deterministas para trayectorias: `TRJ-{QUEUE-NORMALIZED}-{PATIENT-NORMALIZED}-{yyyyMMddHHmmssfff}`. La normalización convierte a mayúsculas, reemplaza caracteres no alfanuméricos con guiones, y colapsa guiones consecutivos.

**Qué problema resuelve**: IDs predecibles, legibles y deterministas que codifican contexto.

### 5.9 Projection Pattern (Read Model)

**Dónde aparece**: `PatientTrajectoryProjectionWriter`, `ProjectionStoreRepository`, consumers.

**Cómo se implementa**: Cada consumer reconstruye el aggregate completo desde el event store y persiste una versión desnormalizada (projección) optimizada para lectura. El `PatientTrajectoryProjection` contiene toda la información necesaria para las queries sin necesidad de replay.

**Qué problema resuelve**: Lecturas O(1) en lugar de O(n) por replay de eventos, donde n es la cantidad de eventos del aggregate.

---

## 6. Interacciones entre Componentes

### Comunicación síncrona (HTTP)

```
Browser ──HTTP──▶ Next.js Proxy ──HTTP──▶ ASP.NET Controllers
                                              │
                                              │ MediatR (in-process)
                                              ▼
                                         Handler
                                              │
                                              │ Inject (IoC)
                                              ▼
                                    Repository / Orchestrator
```

- Las operaciones del staff fluyen síncronamente desde el browser hasta el handler y de vuelta.
- El proxy de Next.js inyecta Bearer token + X-Correlation-Id antes del forward.
- Todas las escrituras retornan `CommandResult<T>` con `Success`, `Message`, `CorrelationId` y opcionalmente `Data`.

### Comunicación asíncrona (eventos)

```
Handler ──persist──▶ Event Store (PostgreSQL)
    │
    └──outbox──▶ OutboxMessage (PostgreSQL)
                      │
          ┌───────────┘ (polling, 500ms)
          ▼
   OutboxProcessor ──dispatch──▶ MassTransit Bus (RabbitMQ)
                                       │
                          ┌────────────┼────────────┬───────────────┐
                          ▼            ▼            ▼               ▼
              TrajectoryConsumer  DashboardConsumer  QueueConsumer  SignalRConsumer
                   │                    │               │               │
                   ▼                    ▼               ▼               ▼
             ProjectionStore     ProjectionStore   ProjectionStore  SignalR Hub
                                                                       │
                                                                       ▼
                                                              SSE → Browser
```

- La propagación es asíncrona con una latencia típica de `500ms` (polling del outbox) + tiempo de procesamiento del consumer + tiempo de entrega al browser vía SSE.
- Cada consumer es independiente y puede fallar sin afectar a los demás (MassTransit maneja retry per-consumer).

### Dependencias entre módulos

```
RLApp.Api ──────────▶ RLApp.Application
    │                      │
    ├──▶ RLApp.Adapters.Http  │
    │                      ▼
    │              RLApp.Domain
    │                      │
    │                      ▼
    │               RLApp.Ports ◀── RLApp.Adapters.Persistence
    │                              ◀── RLApp.Adapters.Messaging
    │                              ◀── RLApp.Infrastructure
    │
    └──▶ RLApp.Infrastructure (DI composition root)
```

- **Domain** no depende de nada externo (solo de .NET BCL).
- **Ports** define interfaces puras sin implementación.
- **Application** depende de Domain y Ports, nunca de adaptadores.
- **Adapters** dependen de Ports (implementan interfaces) y de Domain (usan tipos).
- **Infrastructure** es la composition root que conecta todo.
- **Api** es el entry point que configura y arranca el sistema.

### Nivel de acoplamiento

**Bajo**. La arquitectura hexagonal fuerza que las dependencias fluyan siempre hacia el centro (Domain). Los adaptadores implementan interfaces definidas en Ports. El cambio de RabbitMQ a InMemory, por ejemplo, se maneja con una sola línea de configuración sin tocar ningún handler ni dominio.

---

## 7. Decisiones Técnicas (El Por Qué)

### ¿Por qué Event Sourcing?

**Trade-off**: Mayor complejidad operacional a cambio de trazabilidad total y reconstrucción.

- El requisito de **trazabilidad completa** (RN-16 a RN-20) exige que cada cambio de estado sea registrado como un evento inmutable. Event Sourcing hace esto natural: el estado es la función de sus eventos.
- La capacidad de **reconstrucción** (`Rebuild` endpoint) permite regenerar trayectorias a partir de eventos históricos de otros aggregates — algo imposible con CRUD tradicional.
- La **auditoría** es gratuita: el event store es by design un log de auditoría completo.

### ¿Por qué CQRS con proyecciones persistentes?

**Trade-off**: Consistencia eventual en lecturas a cambio de performance y escalabilidad.

- Las queries de trayectoria (`Discover`, `GetById`, `Active`, `History`) deben ser rápidas (RNF-01/02). Sin proyecciones, cada query requiriría replay de todos los eventos del aggregate — O(n) por trayectoria.
- Las proyecciones en PostgreSQL (`PatientTrajectoryView`) permiten queries O(1) con filtros por `PatientId`, `QueueId`, `CurrentState`, y rangos de fecha.
- El `FindActiveAsync()` del `PatientTrajectoryRepository` consulta directamente la proyección en lugar de escanear el event store completo — eficiente incluso con miles de trayectorias.

### ¿Por qué Outbox Pattern?

**Trade-off**: Latencia adicional de 500ms a cambio de garantía de entrega.

- Sin outbox, la escritura en el event store y la publicación al bus serían dos operaciones no-atómicas. Un fallo entre ambas dejaría el sistema en estado inconsistente.
- El outbox todo lo persiste en una única transacción PostgreSQL. El `OutboxProcessor` despacha de forma independiente con reintento automático.
- La latencia del outbox (polling cada 500ms, batches de 50) es aceptable dado que las actualizaciones de UI se benefician del Outbox Processing Signal que puede acortar el ciclo.

### ¿Por qué MassTransit + RabbitMQ?

**Trade-off**: Dependencia adicional (RabbitMQ) a cambio de desacoplamiento y resilencia.

- MassTransit proporciona abstracciones robustas: consumers tipados, sagas persistentes, retry policies, circuit breakers, y soporte para múltiples transportes.
- RabbitMQ ofrece delivery guarantees, message routing, y es el estándar de facto para .NET message-based systems.
- El fallback a InMemory transport permite correr el sistema completo sin RabbitMQ para desarrollo local.

### ¿Por qué SignalR → SSE (puente)?

**Trade-off**: Complejidad de puente a cambio de compatibilidad universal.

- El backend expone un Hub de SignalR (`/hubs/notifications`) que requiere autenticación JWT.
- El frontend de Next.js no puede conectar directamente al Hub del backend (CORS + autenticación server-to-server).
- La solución: el servidor Next.js establece una conexión SignalR al backend y la expone como SSE stream al browser. Esto permite que el frontend use `EventSource` estándar sin dependencia de la librería SignalR en el cliente.

### ¿Por qué monolito modular y no microservicios?

**Trade-off**: Simplicidad operacional a cambio de deployabilidad unitaria.

- El equipo y el volumen actual no justifican la complejidad operacional de microservicios (orquestadores, service mesh, distributed tracing cross-service).
- La arquitectura hexagonal con assemblies separados permite una futura extracción a microservicios si fuera necesario: cada módulo (Reception, Cashier, Medical, Trajectory) podría convertirse en un servicio independiente reasignando los assemblies.

---

## 8. Modelo de Datos

### Event Store (`event_store`)

```
┌──────────────┬──────────────┬──────────────┬──────────────┬────────────┬──────────────┐
│     Id       │ AggregateId  │SequenceNumber│  EventType   │  Payload   │  OccurredAt  │
│   (Guid PK)  │ (string 128) │   (int)      │ (string 256) │  (JSONB)   │  (DateTime)  │
├──────────────┴──────────────┴──────────────┴──────────────┴────────────┴──────────────┤
│ UNIQUE INDEX: (AggregateId, SequenceNumber)  ← concurrencia optimista                │
│ INDEX: (AggregateId, OccurredAt)                                                      │
│ INDEX: CorrelationId                                                                  │
│ INDEX: (EventType, OccurredAt)                                                        │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

### Outbox Messages (`outbox_messages`)

```
┌────────┬─────────────┬───────────────┬─────────┬──────────┬────────────┬─────────────┬──────────────┬───────┐
│   Id   │ AggregateId │ CorrelationId │  Type   │ Payload  │ OccurredAt │ ProcessedAt │ AttemptCount │ Error │
├────────┴─────────────┴───────────────┴─────────┴──────────┴────────────┴─────────────┴──────────────┴───────┤
│ INDEX: ProcessedAt (null = pendiente)                                                                       │
│ INDEX: (OccurredAt, CorrelationId, AggregateId)                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Patient Trajectory View (`patient_trajectory_views`)

```
┌──────────────┬─────────────┬──────────────┬──────────┬──────────────┬────────────┬──────────┬──────────────┐
│ProjectionId  │ProjectionType│ProjectionData│          │              │            │          │              │
│(TrajectoryId)│"PatientTraj."│  (JSONB)     │          │              │            │          │              │
├──────────────┴──────────────┴──────────────┴──────────┴──────────────┴────────────┴──────────┴──────────────┤
│ ProjectionData contiene:                                                                                    │
│ {                                                                                                           │
│   "trajectoryId": "TRJ-QUEUE-XXX-PATIENT-YYY-20260408...",                                                 │
│   "patientId": "PATIENT-abc123",                                                                            │
│   "queueId": "QUEUE-xyz789",                                                                                │
│   "currentState": "TrayectoriaActiva|TrayectoriaFinalizada|TrayectoriaCancelada",                          │
│   "openedAt": "2026-04-08T10:00:00Z",                                                                      │
│   "closedAt": null,                                                                                         │
│   "correlationIds": ["corr-1", "corr-2", ...],                                                              │
│   "stages": [                                                                                               │
│     { "occurredAt": "...", "stage": "Recepcion", "sourceEvent": "PatientCheckedIn", "sourceState": "..." }, │
│     { "occurredAt": "...", "stage": "Caja", "sourceEvent": "PatientPaymentValidated", "sourceState":"..." },│
│     ...                                                                                                      │
│   ]                                                                                                         │
│ }                                                                                                           │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Consultation Saga State (`consultation_state`)

```
┌───────────────┬──────────────┬──────────────┬───────────┬─────────┬────────┬──────────┬───────────┬───────────────┐
│ CorrelationId │ CurrentState │ TrajectoryId │ PatientId │ QueueId │ RoomId │ CalledAt │ StartedAt │LastCorrelationId│
│  (Guid PK)    │  (string)    │  (string)    │ (string)  │(string) │(string)│(DateTime)│(DateTime?)│   (string)     │
└───────────────┴──────────────┴──────────────┴───────────┴─────────┴────────┴──────────┴───────────┴───────────────┘
```

### Relaciones lógicas

```
PACIENTE (implícito, no tabla propia)
    │
    │ 1:N (a lo largo del tiempo, solo 1 activa simultáneamente)
    ▼
TRAYECTORIA (event_store: stream de eventos por AggregateId TRJ-*)
    │                   ↕ (proyección persistida)
    │          patient_trajectory_views (read model JSON)
    │
    │ 1:N
    ▼
EVENTOS (event_store: registros individuales)
    │
    │ 1:1 (opcional, por outbox)
    ▼
OUTBOX_MESSAGE → procesado → DEAD_LETTER o CONSUMED

COLA_DE_ESPERA (event_store: stream por AggregateId de queue)
    │
    │ emite eventos que el Orchestrator consume para crear/actualizar trayectorias
    ▼
PATIENT_TRAJECTORY (vía PatientTrajectoryOrchestrator)

AUDIT_LOG (tabla dedicada)
    └── Registra todas las operaciones con actor, resultado, contexto
```

---

## 9. Manejo de Concurrencia y Consistencia

### Control de concurrencia optimista

La concurrencia se resuelve a nivel del Event Store:

1. El `EventStoreRepository.SaveBatchAsync()` acepta un `expectedVersion` opcional.
2. Cuando se persisten eventos de un aggregate, se calcula el siguiente `SequenceNumber` como `currentVersion + 1`.
3. El constraint `UNIQUE (AggregateId, SequenceNumber)` en PostgreSQL garantiza que si dos writers intentan escribir la misma versión, uno fallará con una excepción de violación de constraint.
4. `DomainException.ConcurrencyConflict()` se lanza cuando `currentVersion != expectedVersion`, proporcionando un error descriptivo.

```csharp
// EventStoreRepository.SaveBatchAsync (extracto)
var currentVersion = await GetCurrentVersionAsync(aggregateId, cancellationToken);
if (currentVersion != expectedVersion.Value)
{
    throw DomainException.ConcurrencyConflict(aggregateId, expectedVersion.Value, currentVersion);
}
```

### Idempotencia

Se implementa en tres niveles:

1. **IdempotencyGuard** (in-process): `ConcurrentDictionary` con clave compuesta `{correlationId}|{idempotencyKey}`. Previene ejecución duplicada durante la vida del proceso.
2. **PatientTrajectory.HasDuplicateStage()**: Verifica que no exista ya un stage con el mismo `stage`, `sourceEvent`, `sourceState`, `occurredAt` y `correlationId` — idempotencia a nivel de dominio.
3. **PatientTrajectory.CorrelationIds**: Mantiene una lista de todos los correlationIds procesados. Los métodos `Complete()`, `Cancel()`, `RecordRebuild()` verifican contra esta lista antes de operar.

### Consistencia eventual vs fuerte

| Operación | Tipo de consistencia |
|-----------|---------------------|
| Escritura (Event Store + Outbox) | **Fuerte** (transacción PostgreSQL única) |
| Lectura de proyecciones | **Eventual** (consumer asíncrono, latencia ~500ms+) |
| SignalR UI updates | **Eventual** (consumer → SignalR → SSE → browser) |
| Saga state | **Eventual** (MassTransit consumer con persistencia EF Core) |

### Manejo de conflictos

- **Concurrencia de aggregates**: El constraint UNIQUE en el event store actúa como lock optimista. En caso de conflicto, el handler retorna un error y el cliente puede reintentar.
- **Mensajes duplicados en el bus**: MassTransit maneja idempotencia a nivel de consumer. Los consumers de trayectoria hacen `RefreshAsync()` que es idempotente por naturaleza (upsert de proyección).
- **Outbox deduplication**: Mensajes con tipos desconocidos o payloads inválidos se mueven a dead-letter en lugar de reintentar indefinidamente.

---

## 10. Integración con el Sistema

### Integración con el módulo de Recepción

El `RegisterPatientArrivalHandler` invoca directamente `PatientTrajectoryOrchestrator.TrackCheckInAsync()` después de persistir el check-in en el queue. Esto vincula cada registro de paciente con una trayectoria desde el primer momento.

### Integración con el módulo de Caja

Los handlers `CallNextAtCashierHandler` y `ValidatePaymentHandler` en `ConsultingRoomAndCashierHandlers.cs` interactúan con el queue aggregate. Los eventos `PatientPaymentValidated` generados son interceptados por el Orchestrator para registrar la etapa de Caja.

### Integración con el módulo Médico

Los handlers `MedicalCallNextHandler`, `StartConsultationHandler`, `FinishConsultationHandler` invocan directamente al Orchestrator para registrar las etapas de consulta. El `PatientTrajectoryCorrelationResolver` se usa para resolver el `TrajectoryId` activo del paciente antes de cada operación médica.

### Integración con SignalR (tiempo real)

El `SignalRNotificationConsumer` consume 13 tipos de eventos y los mapea a métodos SignalR que el frontend recibe vía SSE bridge. Cada evento se publica a tres scopes: `dashboard` (todos los supervisores), `queue-{queueId}` (personal de esa cola), `trajectory-{trajectoryId}` (supervisores viendo esa trayectoria).

### Integración con el Frontend

- **Consola de Trayectorias** (`patient-trajectory-console.tsx`): Permite discovery por `patientId`/`queueId`, vista de detalle longitudinal, y rebuild. Se invalida automáticamente vía SSE cuando llegan eventos de trayectoria.
- **Dashboard** (`dashboard-home.tsx`): Muestra métricas agregadas que incluyen el estado de trayectorias.
- **Display Público** (`public-waiting-room-display.tsx`): Muestra el estado de la cola y los llamados activos con actualización en tiempo real vía SSE polling (cada 2 segundos).

### Dependencias externas

| Componente | Dependencia | Propósito |
|-----------|-----------|----------|
| PostgreSQL 17 | Base de datos | Event Store, Outbox, Projections, Saga State, Audit Log |
| RabbitMQ 3.13 | Message Broker | Distribución de eventos entre consumers |
| .NET 10 | Runtime | Backend API + Background Services |
| Next.js 16 | Framework | Frontend SSR + API Routes + SSE Bridge |

### Impacto en el sistema existente

La feature se integra de forma **aditiva**, no disruptiva:

- Los aggregates existentes (`WaitingQueue`, `ConsultingRoom`, `StaffUser`) no fueron modificados en su contrato público.
- Los controllers existentes no cambiaron sus endpoints ni sus contratos de request/response.
- Se agregan nuevas dependencias a los handlers existentes (`PatientTrajectoryOrchestrator`, `PatientTrajectoryCorrelationResolver`) vía inyección de dependencias.
- Se agregan 5 nuevos endpoints (`GET /api/patient-trajectories`, `GET /{id}`, `GET /active`, `GET /history`, `POST /rebuild`).
- Se agrega 1 nuevo consumer (`PatientTrajectoryConsumer`) y 4 tipos de eventos adicionales al `SignalRNotificationConsumer`.

---

## 11. Paso a Paso de la Implementación

Reconstruyendo el orden lógico de implementación a partir del código:

### Fase 1: Definición del dominio

1. **Base classes**: `DomainEntity` con `Version`, `_unraisedEvents`, y `DomainEvent` con `EventType`, `OccurredAt`, `CorrelationId`, `AggregateId`, `TrajectoryId`, `SchemaVersion`.
2. **Eventos de trayectoria**: 5 eventos — `PatientTrajectoryOpened`, `PatientTrajectoryStageRecorded`, `PatientTrajectoryCompleted`, `PatientTrajectoryCancelled`, `PatientTrajectoryRebuilt`.
3. **Aggregate PatientTrajectory**: Máquina de estados con `AllowedTransitions`, invariantes (`EnsureActiveForMutation`, `EnsureValidTransition`, `EnsureChronologicalOrder`), idempotencia (`HasDuplicateStage`), y `Replay()` estático.
4. **Factory**: `PatientTrajectoryIdFactory.Create()` para IDs deterministas.

### Fase 2: Puertos (interfaces)

1. **IPatientTrajectoryRepository**: `GetByIdAsync`, `FindActiveAsync`, `AddAsync`, `UpdateAsync`.
2. **IProjectionStore**: `FindPatientTrajectoriesAsync`, `QueryPatientTrajectoriesAsync`, `GetPatientTrajectoryAsync`, `UpsertAsync`.
3. **IEventStore**: `SaveBatchAsync` con `expectedVersion`, `GetEventsByAggregateIdAsync`, `GetAllAsync`.
4. **PatientTrajectoryProjection**: DTO de proyección con stages, correlationIds, state.

### Fase 3: Servicios de aplicación

1. **PatientTrajectoryOrchestrator**: 7 métodos de tracking (`TrackCheckIn`, `TrackPaymentValidated`, `TrackConsultationCalled`, `TrackConsultationStarted`, `TrackCompletion`, `TrackCashierAbsence`, `TrackConsultationAbsence`). Cada uno: busca trayectoria activa → crea o actualiza → persiste → publica eventos.
2. **PatientTrajectoryCorrelationResolver**: Resuelve el trajectoryId activo de un paciente para operaciones que requieren vinculación.
3. **PatientTrajectoryProjectionWriter**: Mapper entre aggregate y proyección, con `RefreshAsync` y `UpsertAsync`.
4. **IdempotencyGuard**: Guard in-process con `ConcurrentDictionary`.

### Fase 4: Handlers (Commands y Queries)

1. **DiscoverPatientTrajectoriesHandler**: Query contra `IProjectionStore.FindPatientTrajectoriesAsync()` + telemetría.
2. **GetPatientTrajectoryHandler**: Query contra `IProjectionStore.GetPatientTrajectoryAsync()`.
3. **QueryActivePatientTrajectoriesHandler**: Query con filtros `queueId`, `stage` opcional.
4. **QueryPatientTrajectoryHistoryHandler**: Query con filtros `queueId`, `from`, `to`.
5. **RebuildPatientTrajectoriesHandler**: Lee todos los eventos históricos, reconstruye trayectorias desde `PatientCheckedIn`, `PaymentValidated`, `PatientCalled`, `AttentionCompleted`, etc. Soporta dry-run.
6. **Integración en handlers existentes**: `RegisterPatientArrivalHandler`, `MedicalCallNextHandler`, `StartConsultationHandler`, `FinishConsultationHandler` ahora invocan al Orchestrator.

### Fase 5: Adaptadores de infraestructura

1. **PatientTrajectoryRepository**: Implementación híbrida (projection store + event store replay).
2. **EventStoreRepository**: Concurrencia optimista vía `UNIQUE (AggregateId, SequenceNumber)`, serialización JSON, type map estático.
3. **ProjectionStoreRepository**: CRUD sobre `PatientTrajectoryView` con queries por `patientId`, `queueId`, `state`, rango de fechas.
4. **PatientTrajectoryConsumer**: Consumer MassTransit que refresca la proyección ante cada evento de trayectoria.
5. **SignalRNotificationConsumer**: 4 nuevos consumers para `TrajectoryOpened`, `TrajectoryStageRecorded`, `TrajectoryCompleted`, `TrajectoryCancelled`.

### Fase 6: Exposición (API + Frontend)

1. **PatientTrajectoriesController**: 5 endpoints REST (`GET /`, `GET /{id}`, `GET /active`, `GET /history`, `POST /rebuild`) con RBAC.
2. **Frontend**: `patient-trajectory-console.tsx` con discovery, detalle, rebuild, y real-time invalidation vía SSE.

### Fase 7: Observabilidad

1. **PatientTrajectoryTelemetry**: Activity source + metrics (discovery counters, latency histograms).
2. **MessageFlowTelemetry**: Instrumentación de consumers y saga transitions.
3. **OutboxProcessorTelemetry**: Métricas de backlog, published, failed, dead-letter.
4. **Health checks**: `ProjectionLagHealthCheck`, `RealtimeChannelHealthCheck`.

---

## 12. Observaciones Técnicas

### Fortalezas del diseño actual

1. **Separación hexagonal estricta**: El dominio no tiene ninguna dependencia de infraestructura. Los puertos definen contratos claros. Los adaptadores son intercambiables (demostrado con RabbitMQ ↔ InMemory).

2. **Event Sourcing riguroso**: El replay sin reflexión (`PatientTrajectory.Replay()` usa pattern matching) es más robusto y performante que la alternativa basada en reflexión. La concurrencia optimista a nivel de event store es sólida.

3. **Outbox transaccional real**: El Outbox Pattern garantiza que la persistencia de eventos y la publicación de mensajes sean atómicas. No hay riesgo de dual-write.

4. **Orquestación explícita**: El `PatientTrajectoryOrchestrator` centraliza toda la lógica de tracking de trayectorias en un solo punto, evitando que la lógica de coordinación se disperse entre handlers.

5. **Idempotencia en profundidad**: Tres niveles de idempotencia (IdempotencyGuard, HasDuplicateStage, CorrelationIds) protegen contra duplicados en diferentes escenarios (reintentos HTTP, replay de eventos, redelivery de mensajes).

6. **Observabilidad integrada**: OpenTelemetry con tracing, métricas, y Prometheus export. Activity sources independientes por dominio (`PatientTrajectoryTelemetry`, `MessageFlowTelemetry`). Health checks granulares.

7. **Reconstrucción determinista**: El `RebuildPatientTrajectoriesHandler` puede regenerar trayectorias completas desde el histórico de eventos de otros aggregates, con deduplicación y soporte para dry-run.

8. **Frontend desacoplado**: El proxy pattern de Next.js con SSE bridge permite que el frontend no tenga dependencia directa del backend SignalR. El uso de React Query con invalidación por SSE es elegante y eficiente.

### Complejidad del diseño

1. **Orquestador síncrono embebido**: El `PatientTrajectoryOrchestrator` se invoca directamente desde los handlers de cola/consultorio como parte del mismo ciclo request-response. Esto añade latencia a las operaciones principales. Si la creación de la trayectoria falla, la operación original también falla. Una alternativa sería mover la orquestación a un consumer asíncrono que reaccione a los eventos del bus.

2. **Outbox polling**: Aunque configurable, el polling introduce latencia inherente. El `IOutboxProcessingSignal` existe como mecanismo para acortar el ciclo, pero la implementación primaria sigue siendo timer-based.

3. **Reconstruction handler complejo**: `RebuildPatientTrajectoriesHandler.RebuildFromHistoricalEvents()` implementa lógica de reconstrucción desde eventos de otros aggregates — esencialmente duplica la lógica del Orchestrator pero en modo batch. Esto introduce un punto dual de mantenimiento.

4. **Consumer count**: 5 consumers + 1 saga + 1 SignalR consumer procesan cada evento publicado. Cada consumer reconstruye el aggregate completo desde el event store (`GetByIdAsync` hace replay). Esto implica N replays por evento publicado, donde N es la cantidad de consumers interesados.

### Nivel de escalabilidad

**Escalabilidad vertical**: El sistema actual escala verticalmente sin dificultad. PostgreSQL maneja la carga de lecturas/escrituras. El monolito modular simplifica la operación.

**Escalabilidad horizontal**: El diseño soporta escalado horizontal con ajustes:

- El `IdempotencyGuard` es in-process (no distribuido). Para múltiples instancias, necesitaría migrar a Redis o un mecanismo distribuido.
- Los consumers de MassTransit pueden escalar horizontalmente nativamente (competing consumers pattern).
- Las proyecciones son idempotentes por diseño (upsert), lo que permite su regeneración desde cualquier instancia.
- El `ConcurrentDictionary` del `RebuildPatientTrajectoriesHandler` para prevenir rebuilds concurrentes es in-process — no protegería entre múltiples instancias.

---

*Documento generado a partir del análisis directo del código fuente del repositorio RLApp-V2, branch `feature/synchronized-clinical-trajectory-orchestrator`. Todas las observaciones están respaldadas por evidencia directa del código — no se asume ni se inventa ningún componente, patrón o comportamiento no implementado.*
