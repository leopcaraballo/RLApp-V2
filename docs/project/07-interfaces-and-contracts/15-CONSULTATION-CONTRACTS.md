# Consultation Contracts

## Shared enums and component schemas

### Consultation turn states

- `EnEsperaConsulta`
- `LlamadoConsulta`
- `EnConsulta`
- `Finalizado`
- `CanceladoPorAusencia`

## Claim next patient

### Claim next purpose

Reservar el siguiente turno elegible de consulta para un consultorio activo.

### Method and path

- `POST /api/waiting-room/claim-next`

### Authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Rol `Doctor`. |
| `X-Correlation-Id` | Yes | Requerido para auditoria y proyecciones. |
| `X-Idempotency-Key` | Yes | Requerido por ser comando mutante. |

### Request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola operativa del dia. |
| `consultingRoomId` | `string` | Yes | Consultorio activo que reclama el turno. |

### Response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno reservado. |
| `turnNumber` | `string` | Yes | Identificador visible del turno. |
| `consultingRoomId` | `string` | Yes | Consultorio que queda asociado. |
| `claimStatus` | `string` | Yes | Resultado funcional de la reserva. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

## Call patient to consultation

### Call patient purpose

Llamar al paciente reservado hacia el consultorio y reflejar el estado visible.

### Call patient method and path

- `POST /api/waiting-room/call-patient`

### Call patient request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno a llamar. |
| `consultingRoomId` | `string` | Yes | Consultorio que realiza el llamado. |

### Call patient response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `LlamadoConsulta`. |
| `consultingRoomId` | `string` | Yes | Consultorio asociado. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

## Medical call next shortcut

### Medical call next purpose

Shortcut operacional que combina reclamar y llamar el siguiente turno elegible.

### Medical call next method and path

- `POST /api/medical/call-next`

### Medical call next request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola operativa del dia. |
| `consultingRoomId` | `string` | Yes | Consultorio activo que llama al siguiente turno. |

### Medical call next response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno seleccionado. |
| `turnNumber` | `string` | Yes | Identificador visible del turno. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `LlamadoConsulta`. |
| `consultingRoomId` | `string` | Yes | Consultorio asociado. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

## Start consultation

### Start consultation purpose

Iniciar la atencion medica de un turno correctamente llamado.

### Start consultation method and path

- `POST /api/medical/start-consultation`

### Start consultation request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `consultingRoomId` | `string` | Yes | Consultorio que inicia la atencion. |

### Start consultation response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `previousState` | `string(enum: TurnState)` | Yes | Debe ser `LlamadoConsulta`. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `EnConsulta`. |
| `startedAt` | `string(date-time)` | Yes | Inicio efectivo de la consulta. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

## Finish consultation

### Finish consultation purpose

Cerrar la consulta y liberar el consultorio al finalizar la atencion.

### Finish consultation method and path

- `POST /api/medical/finish-consultation`

### Finish consultation request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno en atencion. |
| `consultingRoomId` | `string` | Yes | Consultorio que finaliza. |
| `outcome` | `string` | Yes | Resultado funcional de cierre documentado por el medico. |

### Finish consultation response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `previousState` | `string(enum: TurnState)` | Yes | Debe ser `EnConsulta`. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `Finalizado`. |
| `finishedAt` | `string(date-time)` | Yes | Momento efectivo del cierre. |
| `consultingRoomReleased` | `boolean` | Yes | Confirma liberacion del consultorio. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

### Example response

```json
{
  "turnId": "TURN-00052",
  "previousState": "EnConsulta",
  "currentState": "Finalizado",
  "finishedAt": "2026-03-17T11:05:00Z",
  "consultingRoomReleased": true,
  "correlationId": "CORR-a4d05f77"
}
```

## Mark absent in consultation

### Mark absent purpose

Cancelar por ausencia un turno llamado a consulta que no comparecio dentro de la politica vigente.

### Mark absent method and path

- `POST /api/medical/mark-absent`

### Mark absent request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `consultingRoomId` | `string` | Yes | Consultorio asociado. |
| `reason` | `string` | Yes | Justificacion operativa. |

### Mark absent response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `CanceladoPorAusencia`. |
| `policyOutcome` | `string` | Yes | Resultado funcional de la politica de ausencia. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

## Complete attention alias

### Complete attention purpose

Alias operacional para finalizar la atencion desde una ruta mas generica del dominio.

### Complete attention method and path

- `POST /api/waiting-room/complete-attention`

### Complete attention request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `consultingRoomId` | `string` | Yes | Consultorio que completa la atencion. |
| `outcome` | `string` | Yes | Resultado funcional de cierre. |

### Complete attention response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Turno afectado. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `Finalizado`. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |
