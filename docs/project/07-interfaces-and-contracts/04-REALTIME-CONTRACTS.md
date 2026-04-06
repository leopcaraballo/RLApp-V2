# Realtime Contracts

## Public display channel

- `GET|WS /ws/waiting-room`
- mensajes versionados
- display solo recibe payload sanitizado

## Staff synchronized stream

### Stream purpose

Propagar invalidaciones operativas para que la UI de staff resincronice monitor, dashboard y trayectoria sin exponer el token Bearer del backend al navegador.

### Stream method and path

- `GET /api/realtime/operations`

### Stream transport and authorization

- transporte: `text/event-stream` same-origin desde el frontend BFF
- autenticacion: cookie de sesion firmada `httpOnly`; el browser no porta `Authorization: Bearer`
- el BFF usa el token backend solo del lado servidor para conectarse al hub interno de notificaciones

### Stream event schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `version` | `string` | Yes | Version del contrato del mensaje. |
| `eventType` | `string` | Yes | Evento operacional observado. |
| `scope` | `string(enum: queue, dashboard, trajectory)` | Yes | Ambito que debe resincronizarse. |
| `queueId` | `string` | No | Cola afectada cuando aplique. |
| `trajectoryId` | `string` | No | Trayectoria afectada cuando aplique. |
| `correlationId` | `string` | No | Correlacion operativa de apoyo para diagnostico autorizado. |
| `occurredAt` | `string(date-time)` | Yes | Momento del cambio observado. |

### Stream rules

- el stream es de invalidacion; el cliente debe volver a consultar snapshots persistidos para obtener estado completo
- una desconexion del stream debe gatillar reconexion automatica y refetch del estado visible autorizado
- `401` indica sesion inexistente o expirada; `403` indica rol autentico pero no autorizado
- el stream no puede introducir PII adicional fuera de la ya aprobada para la vista de staff que lo consume

## Internal backend relay

- `WS /hubs/notifications`
- reservado para el BFF de staff o clientes confiables equivalentes
- no forma parte del contrato del browser publico ni del display anonimo
