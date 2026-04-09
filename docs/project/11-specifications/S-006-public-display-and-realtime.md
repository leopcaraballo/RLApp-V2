# S-006 Public Display And Realtime

## Purpose

Definir display publico, monitor de visibilidad, contrato sanitizado, latencia operativa y reconexion realtime sin exponer write-side ni PII.

## Traceability

- User stories: `US-006`, `US-007`, `US-010`, `US-021`
- Use cases: `UC-006`, `UC-012`, `UC-013`, `UC-021`
- Tests: `BDD-006`, `TDD-S-006`, `SEC-TEST-002`, `RES-TEST-004`

## Scope

- payload visible para pantalla publica
- destinos simultaneos visibles por caja y consultorio
- consistencia entre monitor y display
- mensajes realtime versionados
- reconexion y recuperacion de estado visible

## Required behavior

- El display publico es anonimo y de solo lectura.
- El payload visible solo puede contener campos aprobados en `10-PUBLIC-DISPLAY-CONTRACT.md`.
- El display nunca acepta comandos ni mutaciones.
- El monitor y el display deben leer desde proyecciones persistentes y payloads sanitizados.
- El display puede mostrar multiples destinos simultaneos siempre que provengan de entradas activas persistidas y sanitizadas.
- La ruta web `/public/waiting-room` debe aceptar `queueId` explicito por query string y, cuando no se indique, resolver la cola por defecto desde configuracion server-side `PUBLIC_WAITING_ROOM_DEFAULT_QUEUE_ID`.
- Tras una desconexion, el cliente debe poder resincronizarse con el ultimo estado visible consistente.

## Contracts

- Query channel: `GET /api/v1/waiting-room/{queueId}/public-display`
- Browser relay: `GET /api/public/waiting-room/{queueId}`
- Realtime relay: `GET /api/realtime/public-waiting-room?queueId={queueId}`
- Campos visibles obligatorios: `queueId`, `generatedAt`, `currentTurn`, `upcomingTurns`, `activeCalls`
- Campos prohibidos: `patientId`, datos de contacto, metadata interna de seguridad
- Contract references: `/docs/project/07-interfaces-and-contracts/10-PUBLIC-DISPLAY-CONTRACT.md`, `/docs/project/07-interfaces-and-contracts/13-RECEPTION-AND-MONITOR-CONTRACTS.md`

## State and event impact

- Consume estados y eventos generados por `S-003`, `S-004` y `S-005`.
- Refleja principalmente `ST-001`, `ST-002`, `ST-005`, `ST-006`, `ST-007`, `ST-008`, `ST-009` segun proyeccion visible.
- Eventos fuente: `EV-002`, `EV-003`, `EV-004`, `EV-010`, `EV-011`, `EV-012`, `EV-013`.

## Validation criteria

- Ninguna mutacion puede exponerse por el canal publico.
- Ningun payload publico puede incluir PII o identificadores internos prohibidos.
- Si existen varios destinos activos en caja o consulta, el snapshot publico debe exponerlos de forma simultanea y sanitizada.
- Reconexion realtime debe recuperar consistencia sin reconstruir write-side en linea.
