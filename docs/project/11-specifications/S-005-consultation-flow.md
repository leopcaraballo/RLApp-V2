# S-005 Consultation Flow

## Purpose

Definir claim del siguiente paciente, llamado a consulta, inicio efectivo, finalizacion y ausencia en consulta.

## Traceability

- User stories: `US-007`, `US-008`, `US-009`, `US-010`
- Use cases: `UC-011`, `UC-012`, `UC-013`, `UC-014`
- Tests: `BDD-005`, `TDD-S-005`

## Scope

- seleccion del siguiente paciente elegible para consulta
- llamado del paciente al consultorio
- inicio de consulta para turno correctamente llamado
- finalizacion de consulta y liberacion del consultorio
- ausencia en consulta con politica asociada

## Preconditions

- Doctor autenticado y autorizado.
- Consultorio activo y no ocupado.
- El turno debe estar en estado elegible para consulta.
- `X-Correlation-Id` obligatorio.

## Required behavior

- `claim-next` reserva el siguiente turno elegible para un consultorio activo.
- `call-patient` mueve el turno de `ST-005 EnEsperaConsulta` a `ST-006 LlamadoConsulta`.
- `start-consultation` solo es valido para un turno correctamente llamado y marca `ST-006 -> ST-007 EnConsulta`.
- `finish-consultation` mueve `ST-007 EnConsulta` a `ST-008 Finalizado` y libera el consultorio.
- `mark-absent` en consulta mueve `ST-006 LlamadoConsulta` a `ST-009 CanceladoPorAusencia`.
- Maximo una ausencia en consulta antes de cancelacion por ausencia.

## Contracts

- Commands: `POST /api/waiting-room/claim-next`, `POST /api/waiting-room/call-patient`, `POST /api/medical/call-next`, `POST /api/medical/start-consultation`, `POST /api/medical/finish-consultation`, `POST /api/medical/mark-absent`, `POST /api/waiting-room/complete-attention`
- Contract reference: `/docs/project/07-interfaces-and-contracts/15-CONSULTATION-CONTRACTS.md`

## State and event impact

- Transiciones: `ST-005 -> ST-006`, `ST-006 -> ST-007`, `ST-007 -> ST-008`, `ST-006 -> ST-009`
- Eventos canonicos: `EV-010 PatientClaimedForAttention`, `EV-011 PatientCalled`, `EV-012 PatientAttentionCompleted`, `EV-013 PatientAbsentAtConsultation`, `EV-014 PatientCancelledByAbsence`

## Validation criteria

- No se puede iniciar consulta sin turno llamado y elegible.
- Un consultorio no puede mantener dos atenciones concurrentes.
- Finalizar consulta debe liberar consultorio y actualizar proyecciones relacionadas.
