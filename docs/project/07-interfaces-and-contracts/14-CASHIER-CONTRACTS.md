# Cashier Contracts

## Shared enums and schema notes

### Cashier turn states

- `EnTaquilla`
- `PagoPendiente`
- `EnEsperaConsulta`
- `CanceladoPorPago`
- `CanceladoPorAusencia`

## Call next at cashier

### Call next purpose

Asignar el siguiente turno elegible de caja a una estacion especifica.

### Method and path

- `POST /api/cashier/call-next`

### Authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Rol `Cashier`. |
| `X-Correlation-Id` | Yes | Requerido para auditoria. |
| `X-Idempotency-Key` | Yes | Requerido por ser comando mutante. |

### Request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola operativa del dia. |
| `cashierStationId` | `string` | Yes | Caja o posicion fisica que toma el turno. |

### Response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno asignado. |
| `turnNumber` | `string` | Yes | Identificador visible del turno. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `EnTaquilla`. |
| `cashierStationId` | `string` | Yes | Estacion que quedo asociada. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

### Example response

```json
{
  "turnId": "TURN-00041",
  "turnNumber": "R-041",
  "currentState": "EnTaquilla",
  "cashierStationId": "CASH-01",
  "correlationId": "CORR-2a88b01c"
}
```

## Validate payment

### Validate payment purpose

Confirmar que el pago fue validado y mover el turno a espera de consulta.

### Validate payment method and path

- `POST /api/cashier/validate-payment`

### Validate payment request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno actualmente atendido en caja. |
| `paymentReference` | `string` | Yes | Referencia de pago confirmada por el operador. |
| `validatedAmount` | `number` | Yes | Monto validado para la operacion. |

### Validate payment response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `previousState` | `string(enum: TurnState)` | Yes | `EnTaquilla` o `PagoPendiente`. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `EnEsperaConsulta`. |
| `paymentStatus` | `string` | Yes | Estado funcional de la validacion de pago. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

### Validate payment canonical errors

| Code | When |
| --- | --- |
| `INVALID_STATE_TRANSITION` | El turno no se encuentra en un estado pagable. |
| `AUTH_ROLE_FORBIDDEN` | El actor no es de caja. |
| `IDEMPOTENCY_REPLAY` | Se repite la misma validacion. |

## Mark payment pending

### Mark payment pending purpose

Marcar el turno como pago pendiente manteniendolo bajo la politica de reintentos.

### Mark payment pending method and path

- `POST /api/cashier/mark-payment-pending`

### Mark payment pending request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `reason` | `string` | Yes | Motivo operativo del pendiente. |
| `attemptNumber` | `integer` | Yes | Numero de intento acumulado. |

### Mark payment pending response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `PagoPendiente`. |
| `attemptNumber` | `integer` | Yes | Intento persistido tras la operacion. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

## Mark absent at cashier

### Mark absent purpose

Cancelar por ausencia un turno que fue llamado en caja y no comparecio segun politica vigente.

### Mark absent method and path

- `POST /api/cashier/mark-absent`

### Mark absent request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno marcado como ausente. |
| `reason` | `string` | Yes | Justificacion operativa. |

### Mark absent response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `CanceladoPorAusencia`. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

## Cancel by payment policy

### Cancel by payment purpose

Cancelar el turno cuando la politica de pago determina que no puede continuar.

### Cancel by payment method and path

- `POST /api/cashier/cancel-payment`

### Cancel by payment request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `reason` | `string` | Yes | Motivo operativo de la cancelacion. |
| `attemptNumber` | `integer` | Yes | Numero de intento que dispara la politica. |

### Cancel by payment response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `previousState` | `string(enum: TurnState)` | Yes | `EnTaquilla` o `PagoPendiente`. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `CanceladoPorPago`. |
| `policyOutcome` | `string` | Yes | Resultado funcional de la politica aplicada. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

### Cancel by payment example response

```json
{
  "turnId": "TURN-00041",
  "previousState": "PagoPendiente",
  "currentState": "CanceladoPorPago",
  "policyOutcome": "max-payment-attempts-exceeded",
  "correlationId": "CORR-12ab09ef"
}
```
