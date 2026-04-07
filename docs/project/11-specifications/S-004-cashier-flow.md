# S-004 Cashier Flow

## Purpose

Definir llamada de caja, validacion de pago, pago pendiente y ausencia operativa en caja.

## Traceability

- User stories: `US-007`, `US-008`, `US-013`, `US-014`
- Use cases: `UC-007`, `UC-008`, `UC-009`, `UC-010`
- Tests: `BDD-004`, `TDD-S-004`

## Scope

- llamada del siguiente turno elegible a caja
- validacion de pago
- paso a pago pendiente
- registro de ausencia de caja

## Preconditions

- Staff autenticado y autorizado como cashier.
- El turno debe ser el actual o elegible en flujo de caja.
- `X-Correlation-Id` obligatorio para `call-next`; `validate-payment`, `mark-payment-pending` y `mark-absent` aceptan generacion server-side cuando el header no se envia.
- `X-Idempotency-Key` obligatorio para `call-next`; el resto de comandos de caja no lo exige en la implementacion actual.

## Required behavior

- `call-next` mueve el siguiente turno elegible de `ST-001 EnEsperaTaquilla` a `ST-002 EnTaquilla`.
- `validate-payment` solo es valido para el turno actual en caja y mueve `ST-002` o `ST-003` a `ST-005 EnEsperaConsulta`.
- `mark-payment-pending` mueve `ST-002 EnTaquilla` a `ST-003 PagoPendiente` y conserva el turno en flujo de caja hasta `validate-payment` o `mark-absent`.
- `mark-absent` mueve un turno activo de caja de `ST-002 EnTaquilla` o `ST-003 PagoPendiente` a `ST-009 CanceladoPorAusencia`, lo retira del flujo operativo y no acumula nuevos intentos de pago.

## Contracts

- Commands: `POST /api/cashier/call-next`, `POST /api/cashier/validate-payment`, `POST /api/cashier/mark-payment-pending`, `POST /api/cashier/mark-absent`
- Contract reference: `/docs/project/07-interfaces-and-contracts/14-CASHIER-CONTRACTS.md`

## State and event impact

- Transiciones: `ST-001 -> ST-002`, `ST-002 -> ST-003`, `ST-002 -> ST-005`, `ST-003 -> ST-005`, `ST-002 -> ST-009`, `ST-003 -> ST-009`
- Eventos canonicos: `EV-003 PatientCalledAtCashier`, `EV-004 PatientPaymentValidated`, `EV-005 PatientPaymentPending`, `EV-006 PatientAbsentAtCashier`

## Validation criteria

- No se puede validar pago para un turno distinto al actual en caja.
- La ausencia en caja debe quedar auditada como cierre terminal en `CanceladoPorAusencia`.
- Un turno marcado ausente en caja no puede reingresar al flujo operativo.
- Pago pendiente y ausencia deben ser auditables y consistentes con el contrato vigente.
