# S-013 Synchronized Operational Visibility

## Purpose

Definir la visibilidad operacional sincronizada para staff a traves de monitor, dashboard y trayectoria con lectura desde proyecciones persistidas y realtime mediado por el BFF del frontend, sin exponer `accessToken` del backend al navegador.

## Traceability

- User stories: `US-021`
- Use cases: `UC-021`
- Tests: `BDD-012`, `TDD-S-013`, `TDD-S-009`, `SEC-TEST-003`, `RES-TEST-004`

## Scope

- snapshot operativo de sala de espera basado en proyecciones persistidas
- dashboard agregado para supervision operativa
- sincronizacion same-origin para staff con invalidacion y refetch de read models
- mediacion de sesion BFF para frontend autenticado

## Preconditions

- el actor debe estar autenticado y autorizado para la vista operativa solicitada
- las proyecciones `v_queue_state`, `v_waiting_room_monitor`, `v_operations_dashboard` y `v_patient_trajectory` deben existir como fuente de lectura persistida
- la sesion web de staff debe estar sellada con cookie firmada `httpOnly`

## Required behavior

- la UI de staff debe leer monitor, dashboard y trayectoria solo desde read models persistidos; nunca desde write-side ni replay en hot path
- el navegador no debe recibir ni persistir el `accessToken` del backend; el BFF lo conserva del lado servidor y expone solo resumen de sesion
- el canal realtime de staff debe ser same-origin y autenticado por la sesion web; el backend hub permanece detras del BFF o de clientes confiables equivalentes
- todo mensaje realtime de staff debe ser versionado y minimizar payload a metadatos de invalidacion como `eventType`, `queueId`, `trajectoryId`, `correlationId` y `occurredAt`
- el mensaje realtime nunca reemplaza el snapshot persistido; cada evento debe disparar refetch del recurso afectado
- tras una desconexion, la UI debe reconectar y resincronizarse consultando otra vez los snapshots autorizados
- monitor y dashboard deben reflejar el estado operacional mas reciente disponible en proyecciones, incluyendo conteos, estados visibles y lag operativo
- la autorizacion del stream realtime no puede ampliar permisos: cada rol solo puede suscribirse a vistas que ya puede consultar por contrato

## Contract dependencies

- monitor y waiting room: `/docs/project/07-interfaces-and-contracts/13-RECEPTION-AND-MONITOR-CONTRACTS.md`
- dashboard y auditoria: `/docs/project/07-interfaces-and-contracts/12-REPORTING-AND-AUDIT-CONTRACTS.md`
- trayectoria: `/docs/project/07-interfaces-and-contracts/16-PATIENT-TRAJECTORY-CONTRACTS.md`
- realtime: `/docs/project/07-interfaces-and-contracts/04-REALTIME-CONTRACTS.md`
- identidad y sesion de staff: `/docs/project/07-interfaces-and-contracts/11-STAFF-IDENTITY-CONTRACTS.md`
- metadata de sesion: `/docs/project/07-interfaces-and-contracts/06-HEADERS-AND-METADATA.md`

## State and event impact

- no introduce estados nuevos; observa `ST-001` a `ST-012`
- no introduce eventos nuevos; observa `EV-001` a `EV-019`
- sincroniza lecturas ya materializadas por `S-007`, `S-008`, `S-011` y `S-012`

## Validation criteria

- monitor y dashboard deben responder desde snapshots persistidos coherentes con la operacion actual
- el browser no puede leer `accessToken` del backend desde los endpoints de sesion del frontend
- el stream realtime debe reconectar y disparar resincronizacion sin romper autorizacion ni exponer PII innecesaria
- cuando no exista sesion valida, monitor/dashboard protegidos y stream realtime deben fallar con `401`; cuando el rol no aplique, con `403`
