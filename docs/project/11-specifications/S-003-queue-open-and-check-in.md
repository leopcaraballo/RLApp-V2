# S-003 Queue Open And Check-In

## Purpose

Definir apertura operativa de cola, admision de pacientes por check-in, orden de prioridad y visibilidad de monitor para recepcion.

## Traceability

- User stories: `US-005`, `US-006`
- Use cases: `UC-005`, `UC-006`
- Tests: `BDD-003`, `TDD-S-003`

## Scope

- resolucion o creacion de contexto de queue del dia
- check-in sin duplicados
- orden de turnos por prioridad y hora de llegada
- consultas de monitor, queue state, next turn e historial reciente

## Preconditions

- Staff autenticado y autorizado para recepcion.
- Referencia de cita valida para el dia operativo.
- `X-Correlation-Id` obligatorio.
- `X-Idempotency-Key` obligatorio para check-in y registro mutante.

## Required behavior

- Un check-in valido debe admitir al paciente exactamente una vez en la queue.
- Un reintento con la misma llave idempotente debe reemitir resultado sin crear un segundo turno.
- La prioridad mas alta se atiende primero; a igualdad de prioridad gana el menor `check-in time`.
- El resultado de admision debe dejar al turno en `ST-001 EnEsperaTaquilla`.
- Monitor, queue state, next turn e historial reciente se leen desde proyecciones, no desde el write-side en linea.

## Contracts

- Commands: `POST /api/waiting-room/check-in`, `POST /api/reception/register`
- Queries: `GET /api/v1/waiting-room/{queueId}/monitor`, `GET /api/v1/waiting-room/{queueId}/queue-state`, `GET /api/v1/waiting-room/{queueId}/next-turn`, `GET /api/v1/waiting-room/{queueId}/recent-history`
- Contract reference: `/docs/project/07-interfaces-and-contracts/13-RECEPTION-AND-MONITOR-CONTRACTS.md`

## State and event impact

- Estado operativo creado o reafirmado: `ST-001 EnEsperaTaquilla`
- Eventos canonicos: `EV-001 WaitingQueueCreated` cuando se inicializa contexto, `EV-002 PatientCheckedIn` cuando se admite el turno

## Validation criteria

- Check-in duplicado debe rechazarse o reemitirse segun llave idempotente, pero nunca duplicar turno.
- El monitor debe reflejar orden consistente con prioridad y llegada.
- El read model de recepcion no puede reconstruirse desde el write-side en hot path.
