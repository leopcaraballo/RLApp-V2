# S-012 Saga Correlation And State Machines

## Purpose

Definir la correlacion longitudinal de sagas y state machines mediante `trajectoryId` estable y `correlationId` operativo para coordinar recepcion, caja y consulta sin abrir instancias paralelas ni perder auditabilidad.

## Traceability

- User stories: `US-020`
- Use cases: `UC-020`
- Tests: `BDD-011`, `TDD-S-012`, `TDD-S-009`

## Scope

- correlacion de state machines del bounded context `Waiting Room`
- adaptacion de `ConsultationSaga` y de futuros flujos longitudinales asincronos
- propagacion de `trajectoryId` y `correlationId` en saga state, mensajes internos, auditoria y observabilidad
- puente determinista para mensajes legacy que aun no contienen `trajectoryId`
- formalizacion de artefactos async machine-readable para los eventos longitudinales ejecutables

## Preconditions

- `trajectoryId` debe existir o poder resolverse deterministicamente antes de aplicar una transicion de saga longitudinal
- `correlationId` sigue siendo obligatorio en acciones mutantes y mensajes internos
- el estado persistido de cada saga debe poder almacenar ambos identificadores

## Required behavior

- toda saga longitudinal de `Waiting Room` debe correlacionarse primariamente por `trajectoryId`, nunca por `PatientId` como clave final
- un mismo `trajectoryId` puede agrupar multiples `correlationId` operativos sin crear instancias paralelas de saga
- el estado de la saga debe persistir `trajectoryId` estable y el `correlationId` mas reciente o causal para diagnostico
- el repositorio por defecto de la saga en perfiles ejecutables debe ser durable sobre PostgreSQL; `InMemoryRepository` solo es valido para pruebas o probes aislados
- `PatientCalled`, `PatientAttentionCompleted` y `PatientAbsentAtConsultation` deben resolver la misma saga cuando pertenecen a la misma trayectoria
- los mensajes legacy sin `trajectoryId` deben resolverse por puente determinista desde la proyeccion de trayectoria o el contexto de replay, sin mutar el payload historico
- los retries, timeouts y dead-letter de la saga deben conservar ambos identificadores en logs, auditoria y diagnostico operativo
- la adaptacion de correlacion no puede alterar las transiciones de negocio documentadas en consulta ni introducir estados o eventos publicos nuevos
- los eventos longitudinales ejecutables deben tener un artefacto async machine-readable alineado con el payload publicado realmente por el sistema

## Contract dependencies

- correlacion y auditoria: `/docs/project/06-application/08-AUDIT-AND-CORRELATION.md`
- eventos internos: `/docs/project/07-interfaces-and-contracts/09-INTERNAL-EVENT-CONTRACTS.md`
- contratos de consulta: `/docs/project/07-interfaces-and-contracts/15-CONSULTATION-CONTRACTS.md`
- trayectoria paciente: `/docs/project/07-interfaces-and-contracts/16-PATIENT-TRAJECTORY-CONTRACTS.md`
- replay y rebuild: `/docs/project/09-data-and-messaging/09-REBUILD-AND-REPLAY-STRATEGY.md`
- artefacto ejecutable derivado: `apps/backend/docs/api/asyncapi.yaml`

## State and event impact

- no introduce nuevos estados o eventos de negocio
- observa `ST-005` a `ST-012` y `EV-010` a `EV-019`
- los estados internos de saga son solo de orquestacion y no reemplazan el catalogo funcional

## Validation criteria

- una trayectoria unica no puede producir dos instancias de saga activas para el mismo flujo longitudinal
- finalizacion y ausencia en consulta deben resolver la misma saga abierta por el llamado previo
- el diagnostico operativo debe poder reconstruir request, evento, saga y proyeccion con `trajectoryId` y `correlationId`
- el puente legacy debe ser determinista e idempotente
- la persistencia de la saga debe sobrevivir reinicios del proceso mientras la base PostgreSQL permanezca integra
