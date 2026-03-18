# S-004 Cashier Flow

## Purpose

Definir llamada de caja, validacion de pago, pago pendiente, ausencia operativa en caja y cancelacion por politica de pago.

## Traceability

- User stories: `US-007`, `US-008`, `US-013`, `US-014`, `US-015`
- Use cases: `UC-007`, `UC-008`, `UC-009`, `UC-010`
- Tests: `BDD-004`, `TDD-S-004`

## Scope

- llamada del siguiente turno elegible a caja
- validacion de pago
- paso a pago pendiente
- registro de ausencia de caja
- cancelacion por politica de pago

## Preconditions

- Staff autenticado y autorizado como cashier.
- El turno debe ser el actual o elegible en flujo de caja.
- `X-Correlation-Id` obligatorio.
- `X-Idempotency-Key` obligatorio para comandos mutantes.

## Required behavior

- `call-next` mueve el siguiente turno elegible de `ST-001 EnEsperaTaquilla` a `ST-002 EnTaquilla`.
- `validate-payment` solo es valido para el turno actual en caja y mueve `ST-002` o `ST-003` a `ST-005 EnEsperaConsulta`.
- `mark-payment-pending` mueve `ST-002 EnTaquilla` a `ST-003 PagoPendiente`.
- La politica de pago admite maximo tres intentos antes de cancelacion funcional.
- La ausencia en caja debe registrarse como evento operativo y contribuir a la politica de cancelacion.
- `cancel-payment` puede cancelar desde `ST-002` o `ST-003` hacia `ST-004 CanceladoPorPago` cuando la politica lo requiera.

## Contracts

- Commands: `POST /api/cashier/call-next`, `POST /api/cashier/validate-payment`, `POST /api/cashier/mark-payment-pending`, `POST /api/cashier/mark-absent`, `POST /api/cashier/cancel-payment`
- Contract reference: `/docs/project/07-interfaces-and-contracts/14-CASHIER-CONTRACTS.md`

## State and event impact

- Transiciones: `ST-001 -> ST-002`, `ST-002 -> ST-003`, `ST-002 -> ST-005`, `ST-003 -> ST-005`, `ST-002 -> ST-004`, `ST-003 -> ST-004`
- Eventos canonicos: `EV-003 PatientCalledAtCashier`, `EV-004 PatientPaymentValidated`, `EV-005 PatientPaymentPending`, `EV-006 PatientAbsentAtCashier`, `EV-007 PatientCancelledByPayment`

## Validation criteria

- No se puede validar pago para un turno distinto al actual en caja.
- El turno cancelado por pago no puede reingresar al flujo operativo.
- Los intentos y ausencias deben ser auditables y consistentes con la politica de negocio.
