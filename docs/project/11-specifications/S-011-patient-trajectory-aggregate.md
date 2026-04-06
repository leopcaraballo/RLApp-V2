# S-011 Patient Trajectory Aggregate

## Purpose

Definir el agregado `TrayectoriaPaciente`, su identificador canonico `TrajectoryId`, la consulta protegida de trayectoria longitudinal y el replay controlado para reconstruir historial de paciente sin fragmentar recepcion, caja y consulta.

## Traceability

- User stories: `US-012`, `US-018`
- Use cases: `UC-018`
- Tests: `BDD-010`, `TDD-S-011`, `SEC-TEST-001`, `SEC-TEST-003`, `RES-TEST-002`

## Scope

- apertura y cierre de una trayectoria unica por paciente dentro de `Waiting Room`
- registro de hitos longitudinales desde recepcion, caja y consulta
- consulta protegida de trayectoria por `trajectoryId`
- rebuild y replay controlados para poblar o reconciliar trayectorias desde eventos historicos

## Preconditions

- el actor debe estar autenticado y autorizado para consultar o reconstruir trayectorias cuando use contratos operativos
- los eventos historicos disponibles deben conservar `PatientId`, `QueueId`, `occurredAt` y `correlationId`
- `X-Correlation-Id` es obligatorio en rebuild y recomendado en consultas de trayectoria

## Required behavior

- el primer evento operativo elegible debe abrir una unica `TrayectoriaPaciente` con `TrajectoryId`, `PatientId`, `QueueId` y `correlationId` inicial
- ningun paciente puede mantener mas de una trayectoria activa en la misma `QueueId`
- los hitos de recepcion, caja y consulta deben anexarse en orden cronologico monotono y con `trajectoryId` estable
- una trayectoria cerrada por finalizacion o cancelacion no admite nuevos hitos salvo rehidratacion idempotente del historial
- el rebuild debe reconstruir la misma trayectoria desde eventos historicos sin mutar eventos legacy ni reemitir side effects operativos
- `trajectoryId` complementa `correlationId` para vistas longitudinales; la migracion completa de correlacion de sagas queda fuera de esta fase

## Contract dependencies

- contrato de consulta y rebuild: `/docs/project/07-interfaces-and-contracts/16-PATIENT-TRAJECTORY-CONTRACTS.md`
- metadata de eventos internos: `/docs/project/07-interfaces-and-contracts/09-INTERNAL-EVENT-CONTRACTS.md`
- correlacion y auditoria: `/docs/project/06-application/08-AUDIT-AND-CORRELATION.md`
- replay controlado: `/docs/project/09-data-and-messaging/09-REBUILD-AND-REPLAY-STRATEGY.md`

## State and event impact

- introduce `ST-010 TrayectoriaActiva`, `ST-011 TrayectoriaFinalizada` y `ST-012 TrayectoriaCancelada`
- introduce `EV-015 PatientTrajectoryOpened`, `EV-016 PatientTrajectoryStageRecorded`, `EV-017 PatientTrajectoryCompleted`, `EV-018 PatientTrajectoryCancelled` y `EV-019 PatientTrajectoryRebuilt`
- observa `ST-001` a `ST-009` y `EV-001` a `EV-014` para reconstruir la trayectoria longitudinal desde el historial existente

## Validation criteria

- un paciente con eventos historicos de recepcion, caja y consulta debe producir una unica trayectoria materializada
- el replay debe ser idempotente y no puede duplicar hitos ya aplicados
- una trayectoria cerrada debe rechazar un nuevo hito mutante
- la consulta debe devolver una vista cronologica desde proyeccion persistente, nunca desde replay en hot path
