# Reporting And Audit Contracts

## Operations dashboard

### Dashboard purpose

Exponer el estado operativo agregado de sala de espera, caja y consulta desde proyecciones persistentes.

### Dashboard method and path

- `GET /api/v1/operations/dashboard`

### Dashboard authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Disponible para supervisor y perfiles autorizados de monitoreo. |
| `X-Correlation-Id` | No | Recomendado para trazabilidad de lectura. |

### Dashboard response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `generatedAt` | `string(date-time)` | Yes | Momento de emision del snapshot. |
| `currentWaitingCount` | `integer` | Yes | Turnos visibles en espera. |
| `waitingTimeP95Seconds` | `integer` | Yes | Percentil 95 del tiempo de espera. |
| `cashierThroughputPerHour` | `integer` | Yes | Atenciones por hora consolidadas para caja. |
| `consultationThroughputPerHour` | `integer` | Yes | Atenciones por hora consolidadas para consulta. |
| `absentCount` | `integer` | Yes | Conteo agregado de ausencias en la ventana operativa. |
| `activeRooms` | `integer` | Yes | Consultorios activos al momento del snapshot. |
| `projectionLagSeconds` | `integer` | Yes | Lag actual de la proyeccion usada para el dashboard. |

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
  "waitingTimeP95Seconds": 1140,
  "cashierThroughputPerHour": 22,
  "consultationThroughputPerHour": 18,
  "absentCount": 1,
  "activeRooms": 4,
  "projectionLagSeconds": 3
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
- Toda respuesta debe incluir `correlationId` cuando aplique o permitir inferirlo desde la ruta.
