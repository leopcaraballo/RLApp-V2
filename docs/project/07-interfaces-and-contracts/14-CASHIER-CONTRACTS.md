# Cashier Contracts

## Shared enums and schema notes

### Cashier turn states

- `EnTaquilla`
- `PagoPendiente`
- `EnEsperaConsulta`
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

Marcar el turno como pago pendiente manteniendolo en el flujo operativo de caja hasta su siguiente accion valida.

### Mark payment pending method and path

- `POST /api/cashier/mark-payment-pending`

### Mark payment pending request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `reason` | `string` | Yes | Motivo operativo del pendiente. |
| `attemptNumber` | `integer` | Yes | Dato operativo provisto por el cliente; no activa una cancelacion automatica en el runtime vigente. |

### Mark payment pending response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `PagoPendiente`. |
| `attemptNumber` | `integer` | Yes | Valor reflejado por el contrato actual para trazabilidad operativa del cliente. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

## Mark absent at cashier

### Mark absent purpose

Registrar una ausencia operativa en caja y retirar el turno del flujo sin reutilizar la politica de reintentos de pago.

### Mark absent method and path

- `POST /api/cashier/mark-absent`

### Mark absent authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Rol `Cashier`. |
| `X-Correlation-Id` | No | Si se omite, el backend genera uno. |

### Mark absent request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola operativa del dia. |
| `patientId` | `string` | Yes | Paciente actualmente en flujo de caja. |
| `turnId` | `string` | Yes | Turno marcado como ausente. |
| `reason` | `string` | Yes | Justificacion operativa. |

### Mark absent response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `success` | `boolean` | Yes | `true` cuando la operacion se registra correctamente. |
| `message` | `string` | Yes | Mensaje operativo de exito. |
| `correlationId` | `string` | Yes | Header recibido o generado por servidor. |
| `executedAt` | `string(date-time)` | Yes | Timestamp UTC de ejecucion. |

### Mark absent operational outcome

- Resultado canonico: `EnTaquilla` o `PagoPendiente` terminan en `CanceladoPorAusencia`.
- La verificacion del estado terminal se hace por auditoria y proyecciones visibles; el endpoint actual no devuelve `currentState`.

### Mark absent example response

```json
{
  "success": true,
  "message": "Patient PAT-001 marked as absent",
  "correlationId": "CORR-19ac0dfe",
  "executedAt": "2026-04-07T16:15:00Z"
}
```
