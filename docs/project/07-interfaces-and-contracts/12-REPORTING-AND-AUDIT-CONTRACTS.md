# Reporting And Audit Contracts

## Operations dashboard

### Dashboard purpose

Exponer el estado operativo agregado de sala de espera, caja y consulta desde proyecciones persistentes.

### Dashboard method and path

- `GET /api/v1/operations/dashboard`

### Dashboard authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Disponible para `Supervisor` y `Support`. |
| `X-Correlation-Id` | No | Recomendado para trazabilidad de lectura. |

### Dashboard response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `generatedAt` | `string(date-time)` | Yes | Momento de emision del snapshot. |
| `currentWaitingCount` | `integer` | Yes | Turnos visibles en espera. |
| `averageWaitTimeMinutes` | `number` | Yes | Promedio operativo persistido para las queues observadas. |
| `totalPatientsToday` | `integer` | Yes | Conteo agregado persistido por la proyeccion de dashboard. |
| `totalCompleted` | `integer` | Yes | Conteo total de atenciones cerradas persistidas. |
| `activeRooms` | `integer` | Yes | Consultorios activos al momento del snapshot. |
| `projectionLagSeconds` | `integer` | Yes | Lag actual de la proyeccion usada para el dashboard. |
| `queueSnapshots` | `array[DashboardQueueSnapshot]` | Yes | Snapshot por queue utilizado para componer la vista. |
| `statusBreakdown` | `array[DashboardStatusCount]` | Yes | Conteo de entradas visibles por estado en monitor. |

### Dashboard queue snapshot schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | Yes | Cola observada. |
| `totalPending` | `integer` | Yes | Pendientes materializados para la queue. |
| `averageWaitTimeMinutes` | `number` | Yes | Promedio de espera persistido para la queue. |
| `lastUpdatedAt` | `string(date-time)` | Yes | Ultima actualizacion observada de la queue. |

### Dashboard status count schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `status` | `string` | Yes | Estado visible del monitor. |
| `total` | `integer` | Yes | Cantidad de entradas materializadas en ese estado. |

### Dashboard canonical errors

| Code | When |
| --- | --- |
| `DASHBOARD_PROJECTION_UNAVAILABLE` | La proyeccion persistente no esta disponible o excede el lag tolerado. |
| `AUTH_ROLE_FORBIDDEN` | El actor autenticado no puede consultar el dashboard. |

### Dashboard example response

```json
{
  "generatedAt": "2026-03-17T10:30:00Z",
  "currentWaitingCount": 9,
  "averageWaitTimeMinutes": 19.5,
  "totalPatientsToday": 46,
  "totalCompleted": 31,
  "activeRooms": 4,
  "projectionLagSeconds": 3,
  "queueSnapshots": [
    {
      "queueId": "Q-2026-03-17-MAIN",
      "totalPending": 9,
      "averageWaitTimeMinutes": 19.5,
      "lastUpdatedAt": "2026-03-17T10:29:58Z"
    }
  ],
  "statusBreakdown": [
    {
      "status": "Waiting",
      "total": 9
    },
    {
      "status": "InConsultation",
      "total": 4
    }
  ]
}
```

## Audit timeline by correlationId

### Timeline purpose

Reconstruir la secuencia auditable de acciones y eventos asociados a una correlacion operacional.

### Timeline method and path

- `GET /api/v1/audit/timeline/{correlationId}`

### Timeline authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Disponible para supervisor y perfiles de auditoria. |
| `X-Correlation-Id` | No | Recomendado para trazar la consulta misma. |

### Timeline path parameters

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `correlationId` | `string` | Yes | Correlacion a consultar. |

### Timeline response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `correlationId` | `string` | Yes | Identificador consultado. |
| `generatedAt` | `string(date-time)` | Yes | Momento en que se genero la respuesta. |
| `entries` | `array[AuditTimelineEntry]` | Yes | Entradas ordenadas cronologicamente. |

### Timeline entry schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `occurredAt` | `string(date-time)` | Yes | Momento del comando, decision o evento. |
| `actor` | `string` | Yes | Actor tecnico u operativo asociado. |
| `action` | `string` | Yes | Nombre de la accion observada. |
| `entityType` | `string` | Yes | Tipo de agregado, proyeccion o recurso auditado. |
| `entityId` | `string` | Yes | Identificador de la entidad. |
| `outcome` | `string` | Yes | Resultado observable de la accion. |
| `eventName` | `string` | No | Evento de dominio asociado cuando aplique. |

### Timeline canonical errors

| Code | When |
| --- | --- |
| `AUDIT_TIMELINE_NOT_FOUND` | No existe timeline para la correlacion solicitada. |
| `AUTH_ROLE_FORBIDDEN` | El actor autenticado no puede consultar auditoria. |

### Timeline example response

```json
{
  "correlationId": "CORR-2a88b01c",
  "generatedAt": "2026-03-17T10:34:12Z",
  "entries": [
    {
      "occurredAt": "2026-03-17T10:31:02Z",
      "actor": "Cashier:CASH-01",
      "action": "call-next",
      "entityType": "Turn",
      "entityId": "TURN-00041",
      "outcome": "EnTaquilla",
      "eventName": "PatientCalledAtCashier"
    },
    {
      "occurredAt": "2026-03-17T10:33:18Z",
      "actor": "Cashier:CASH-01",
      "action": "validate-payment",
      "entityType": "Turn",
      "entityId": "TURN-00041",
      "outcome": "EnEsperaConsulta",
      "eventName": "PatientPaymentValidated"
    }
  ]
}
```

## Query rules

- Los dos contratos leen desde proyecciones persistentes o audit store, nunca desde replay en hot path.
- El timeline debe devolver los eventos en orden cronologico ascendente.
- El dashboard compone su snapshot desde `v_operations_dashboard`, `v_queue_state` y `v_waiting_room_monitor` o equivalentes persistidos del mismo bounded context.
- Toda respuesta debe incluir `correlationId` cuando aplique o permitir inferirlo desde la ruta.
