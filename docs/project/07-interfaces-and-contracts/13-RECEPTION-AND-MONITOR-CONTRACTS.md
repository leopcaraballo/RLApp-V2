# Reception And Monitor Contracts

## Shared enums and component schemas

### Turn state enum used in this document

- `EnEsperaTaquilla`
- `EnEsperaConsulta`
- `EnTaquilla`
- `PagoPendiente`

### RecentHistoryEntry schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnNumber` | `string` | Yes | Identificador visible del turno. |
| `visibleOutcome` | `string` | Yes | Estado publico o interno permitido para monitor. |

### MonitorTurnEntry schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `turnId` | `string` | Yes | Identificador canonico del turno materializado. |
| `patientName` | `string` | Yes | Nombre visible autorizado para monitor interno. |
| `ticketNumber` | `string` | Yes | Turno visible del paciente. |
| `status` | `string` | Yes | Estado visible actual de la entrada. |
| `roomAssigned` | `string` | No | Consultorio visible cuando aplique. |
| `updatedAt` | `string(date-time)` | Yes | Ultima actualizacion observada para la entrada. |

### Monitor visible status taxonomy

Los snapshots de monitor y sus agregados usan un vocabulario visible estable para no exponer todo el detalle del write-side en la UI:

- `Waiting`: turno en espera operativa antes de caja.
- `AtCashier`: turno atendido actualmente en caja.
- `PaymentPending`: turno retenido por pago pendiente.
- `WaitingForConsultation`: turno listo y visible en espera de consulta.
- `Called`: turno llamado hacia consulta.
- `InConsultation`: turno actualmente materializado como atencion activa en consulta.
- `Completed`: turno finalizado y retenido en monitor segun la politica visible vigente.
- `Absent`: turno cancelado por ausencia visible.
- `Cancelled`: turno cancelado por politica de pago visible.

### Visible status mapping rules

- `ST-001 EnEsperaTaquilla` se expone como `Waiting`.
- `ST-002 EnTaquilla` se expone como `AtCashier`.
- `ST-003 PagoPendiente` se expone como `PaymentPending`.
- `ST-005 EnEsperaConsulta` se expone como `WaitingForConsultation`.
- `ST-006 LlamadoConsulta` se expone como `Called`.
- `ST-007 EnConsulta` o su materializacion operacional equivalente se expone como `InConsultation`.
- `ST-008 Finalizado` se expone como `Completed`.
- `ST-009 CanceladoPorAusencia` se expone como `Absent`.
- `ST-004 CanceladoPorPago` se expone como `Cancelled`.

### MonitorStatusCount schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `status` | `string` | Yes | Estado visible agregado. |
| `total` | `integer` | Yes | Cantidad de entradas en ese estado. |

## Check-in command

### Check-in purpose

Registrar la llegada de un paciente con cita valida y ubicarlo en la cola operativa.

### Method and path

- `POST /api/waiting-room/check-in`

### Authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Staff autorizado para recepcion. |
| `X-Correlation-Id` | Yes | Se propaga a logs, eventos y auditoria. |
| `X-Idempotency-Key` | Yes | Requerido para evitar duplicados de admision. |

### Request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `appointmentReference` | `string` | Yes | Referencia canonica de cita del dia. |
| `patientId` | `string` | Yes | Identificador del paciente usado internamente. |
| `consultationType` | `string` | Yes | Tipo de consulta ya definido por agenda o integracion. |
| `priority` | `string` | Yes | Prioridad operativa; su ordenamiento sigue `BR-002 priority ordering`. |

### Response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola operativa del dia. |
| `turnId` | `string` | Yes | Identificador canonico del turno. |
| `turnNumber` | `string` | Yes | Identificador visible del turno. |
| `currentState` | `string(enum: TurnState)` | Yes | Debe quedar en `EnEsperaTaquilla`. |
| `queuePosition` | `integer` | Yes | Posicion visible dentro de la cola elegible. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |
| `idempotencyReplay` | `boolean` | Yes | Indica si la respuesta provino de replay idempotente. |

### Canonical errors

| Code | When |
| --- | --- |
| `AUTH_ROLE_FORBIDDEN` | El actor no tiene permisos de recepcion. |
| `AUTH_INVALID_CREDENTIALS` | El token es invalido o esta expirado. |
| `IDEMPOTENCY_REPLAY` | Se reenvia el mismo comando mutante. |
| `INVALID_STATE_TRANSITION` | La admision no es valida para el estado actual del turno. |

### Example request

```json
{
  "appointmentReference": "APT-20260317-00045",
  "patientId": "PAT-0045",
  "consultationType": "GeneralMedicine",
  "priority": "Standard"
}
```

### Example response

```json
{
  "queueId": "Q-2026-03-17-MAIN",
  "turnId": "TURN-00045",
  "turnNumber": "R-045",
  "currentState": "EnEsperaTaquilla",
  "queuePosition": 8,
  "correlationId": "CORR-9d8818f8",
  "idempotencyReplay": false
}
```

## Reception register command

### Register purpose

Alias operativo para recepcion cuando el front expresa la accion como registro de llegada.

### Register method and path

- `POST /api/reception/register`

### Register authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Staff autorizado para recepcion. |
| `X-Correlation-Id` | Yes | Requerido para trazabilidad. |
| `X-Idempotency-Key` | Yes | Requerido por ser comando mutante. |

### Register request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `patientId` | `string` | Yes | Identificador interno del paciente. |
| `appointmentReference` | `string` | Yes | Referencia de la cita a registrar. |
| `priority` | `string` | Yes | Prioridad operativa definida para la cola. |
| `notes` | `string` | No | Observacion operativa interna cuando aplique. |

### Register response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola donde quedo el turno. |
| `turnId` | `string` | Yes | Turno creado o reafirmado. |
| `registeredAt` | `string(date-time)` | Yes | Momento del registro. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

### Register canonical errors

| Code | When |
| --- | --- |
| `AUTH_ROLE_FORBIDDEN` | El actor no es de recepcion. |
| `IDEMPOTENCY_REPLAY` | El registro ya fue procesado con la misma llave. |
| `INVALID_STATE_TRANSITION` | El turno no admite nueva admision. |

## Monitor query

### Monitor purpose

Exponer un snapshot operativo para recepcion y supervision sin leer del write-side.

### Monitor method and path

- `GET /api/v1/waiting-room/{queueId}/monitor`

### Monitor authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Disponible para `Receptionist` y `Supervisor`. |
| `X-Correlation-Id` | No | Recomendado para tracing de lectura. |

### Monitor path parameters

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola a consultar. |

### Monitor response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola consultada. |
| `generatedAt` | `string(date-time)` | Yes | Momento del snapshot. |
| `waitingCount` | `integer` | Yes | Turnos aun esperando atencion. |
| `averageWaitTimeMinutes` | `number` | Yes | Promedio de espera persistido para la queue. |
| `activeConsultationRooms` | `integer` | Yes | Cantidad de consultorios visibles con atencion activa. |
| `statusBreakdown` | `array[MonitorStatusCount]` | Yes | Conteo agregado por estado visible. |
| `entries` | `array[MonitorTurnEntry]` | Yes | Entradas vivas del monitor ordenadas por `updatedAt` descendente. |

### Monitor example response

```json
{
  "queueId": "Q-2026-03-17-MAIN",
  "generatedAt": "2026-03-17T10:15:30Z",
  "waitingCount": 9,
  "averageWaitTimeMinutes": 18.25,
  "activeConsultationRooms": 4,
  "statusBreakdown": [
    {
      "status": "Waiting",
      "total": 9
    },
    {
      "status": "InConsultation",
      "total": 4
    }
  ],
  "entries": [
    {
      "turnId": "Q-2026-03-17-MAIN-PAT-0040",
      "patientName": "Ana Perez",
      "ticketNumber": "R-040",
      "status": "Waiting",
      "roomAssigned": null,
      "updatedAt": "2026-03-17T10:15:22Z"
    }
  ]
}
```

## Queue state query

### Queue state purpose

Devolver la cola completa observable para monitoreo interno.

### Queue state method and path

- `GET /api/v1/waiting-room/{queueId}/queue-state`

### Queue state response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola consultada. |
| `generatedAt` | `string(date-time)` | Yes | Momento del snapshot. |
| `turns` | `array[object]` | Yes | Cada entrada debe incluir al menos `turnId`, `turnNumber`, `currentState` y `queuePosition`. |

## Next turn query

### Next turn purpose

Resolver los siguientes turnos elegibles para caja y consulta.

### Next turn method and path

- `GET /api/v1/waiting-room/{queueId}/next-turn`

### Next turn response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola consultada. |
| `nextCashierTurn` | `string` | No | Siguiente turno elegible para caja. |
| `nextConsultationTurn` | `string` | No | Siguiente turno elegible para consulta. |
| `generatedAt` | `string(date-time)` | Yes | Momento del calculo. |

## Recent history query

### Recent history purpose

Exponer el historial reciente visible para recepcion, monitor y display sanitizado.

### Recent history method and path

- `GET /api/v1/waiting-room/{queueId}/recent-history`

### Recent history response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola consultada. |
| `entries` | `array[RecentHistoryEntry]` | Yes | Historial ordenado desde el mas reciente segun la proyeccion visible. |
| `generatedAt` | `string(date-time)` | Yes | Momento del snapshot. |

## Query rules

- el monitor lee desde `v_queue_state` y `v_waiting_room_monitor` o equivalentes persistidos; no consulta el write-side
- las `entries` del monitor se ordenan por `updatedAt` descendente
- `waitingCount` y `averageWaitTimeMinutes` deben reflejar el snapshot de queue persistido mas reciente
- `waitingCount` contabiliza solo entradas con estado visible `Waiting` o `WaitingForConsultation`
- `activeConsultationRooms` contabiliza solo entradas visibles en `InConsultation`
- `statusBreakdown` y `entries[*].status` deben seguir exactamente la taxonomia visible definida en este contrato
