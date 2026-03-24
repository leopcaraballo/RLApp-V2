# S-006 Public Display And Realtime

## Purpose

Definir display publico, monitor de visibilidad, contrato sanitizado, latencia operativa y reconexion realtime sin exponer write-side ni PII.

## Traceability

- User stories: `US-006`, `US-007`, `US-010`
- Use cases: `UC-006`, `UC-012`, `UC-013`
- Tests: `BDD-006`, `TDD-S-006`, `SEC-TEST-002`, `RES-TEST-004`

## Scope

- payload visible para pantalla publica
- consistencia entre monitor y display
- mensajes realtime versionados
- reconexion y recuperacion de estado visible

## Required behavior

- El display publico es anonimo y de solo lectura.
- El payload visible solo puede contener campos aprobados en `10-PUBLIC-DISPLAY-CONTRACT.md`.
- El display nunca acepta comandos ni mutaciones.
- El monitor y el display deben leer desde proyecciones persistentes y payloads sanitizados.
- Tras una desconexion, el cliente debe poder resincronizarse con el ultimo estado visible consistente.

## Contracts

- Query channel: `GET /api/v1/waiting-room/{queueId}/monitor`
- Realtime channel: `GET|WS /ws/waiting-room`
- Campos visibles obligatorios: `queueId`, `currentTurn`, `waitingSummary`, `roomStatus`, `recentHistory`
- Campos prohibidos: `patientId`, datos de contacto, metadata interna de seguridad
- Contract references: `/docs/project/07-interfaces-and-contracts/10-PUBLIC-DISPLAY-CONTRACT.md`, `/docs/project/07-interfaces-and-contracts/13-RECEPTION-AND-MONITOR-CONTRACTS.md`

## State and event impact

- Consume estados y eventos generados por `S-003`, `S-004` y `S-005`.
- Refleja principalmente `ST-001`, `ST-002`, `ST-005`, `ST-006`, `ST-007`, `ST-008`, `ST-009` segun proyeccion visible.
- Eventos fuente: `EV-002`, `EV-003`, `EV-004`, `EV-010`, `EV-011`, `EV-012`, `EV-013`, `EV-014`.

## Validation criteria

- Ninguna mutacion puede exponerse por el canal publico.
- Ningun payload publico puede incluir PII o identificadores internos prohibidos.
- Reconexion realtime debe recuperar consistencia sin reconstruir write-side en linea.
