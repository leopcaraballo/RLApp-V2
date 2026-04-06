# Patient Trajectory Contracts

## Trajectory query

### Query purpose

Exponer la trayectoria longitudinal de un paciente desde una proyeccion persistente y auditable.

### Query method and path

- `GET /api/patient-trajectories/{trajectoryId}`

### Query authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Disponible para `Supervisor` y `Support`. |
| `X-Correlation-Id` | No | Recomendado para trazar la consulta misma. |

### Query path parameters

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `trajectoryId` | `string` | Yes | Identificador canonico de la trayectoria a consultar. |

### Query response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `trajectoryId` | `string` | Yes | Identificador longitudinal consultado. |
| `patientId` | `string` | Yes | Paciente asociado. |
| `queueId` | `string` | Yes | Queue canonica donde se abrio la trayectoria. |
| `currentState` | `string` | Yes | Estado actual de la trayectoria (`ST-010`, `ST-011` o `ST-012`). |
| `openedAt` | `string(date-time)` | Yes | Momento de apertura de la trayectoria. |
| `closedAt` | `string(date-time)` | No | Momento de cierre cuando aplique. |
| `correlationIds` | `array[string]` | Yes | Correlaciones operativas observadas en la trayectoria. |
| `stages` | `array[TrajectoryStageEntry]` | Yes | Hitos ordenados cronologicamente. |

### Query stage schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `occurredAt` | `string(date-time)` | Yes | Momento del hito longitudinal. |
| `stage` | `string` | Yes | Nombre de etapa de negocio observada. |
| `sourceEvent` | `string` | Yes | Evento canonico que origino el hito. |
| `sourceState` | `string` | No | Estado operativo asociado cuando aplique. |
| `correlationId` | `string` | Yes | Correlacion operativa del hito. |

### Query canonical errors

| Code | When |
| --- | --- |
| `TRAJECTORY_NOT_FOUND` | No existe trayectoria materializada para el `trajectoryId` solicitado. |
| `AUTH_ROLE_FORBIDDEN` | El actor autenticado no puede consultar trayectorias. |

### Query example response

```json
{
  "trajectoryId": "TRJ-00041",
  "patientId": "PAT-00041",
  "queueId": "QUEUE-01",
  "currentState": "TrayectoriaFinalizada",
  "openedAt": "2026-04-01T09:10:00Z",
  "closedAt": "2026-04-01T09:42:00Z",
  "correlationIds": [
    "CORR-a1",
    "CORR-a2",
    "CORR-a3"
  ],
  "stages": [
    {
      "occurredAt": "2026-04-01T09:10:00Z",
      "stage": "Recepcion",
      "sourceEvent": "PatientCheckedIn",
      "sourceState": "EnEsperaTaquilla",
      "correlationId": "CORR-a1"
    },
    {
      "occurredAt": "2026-04-01T09:22:00Z",
      "stage": "Caja",
      "sourceEvent": "PatientPaymentValidated",
      "sourceState": "EnEsperaConsulta",
      "correlationId": "CORR-a2"
    },
    {
      "occurredAt": "2026-04-01T09:42:00Z",
      "stage": "Consulta",
      "sourceEvent": "PatientAttentionCompleted",
      "sourceState": "Finalizado",
      "correlationId": "CORR-a3"
    }
  ]
}
```

## Trajectory discovery

### Discovery purpose

Resolver `trajectoryId` candidatos cuando el operador conoce `patientId` y, opcionalmente, `queueId`, pero aun no tiene el identificador longitudinal exacto.

### Discovery method and path

- `GET /api/patient-trajectories`

### Discovery authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Disponible para `Supervisor` y `Support`. |
| `X-Correlation-Id` | No | Recomendado para trazar el lookup operacional. |

### Discovery query parameters

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `patientId` | `string` | Yes | Paciente a buscar dentro de trayectorias materializadas. |
| `queueId` | `string` | No | Acota la busqueda a una queue especifica cuando el operador conoce el contexto operativo. |

### Discovery response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `total` | `integer` | Yes | Cantidad de trayectorias candidatas encontradas. |
| `items` | `array[TrajectoryDiscoveryEntry]` | Yes | Candidatas ordenadas con trayectorias activas primero y luego por `openedAt` descendente. |

### Discovery entry schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `trajectoryId` | `string` | Yes | Identificador longitudinal candidato. |
| `patientId` | `string` | Yes | Paciente asociado a la trayectoria. |
| `queueId` | `string` | Yes | Queue canonica donde se abrio la trayectoria. |
| `currentState` | `string` | Yes | Estado actual de la trayectoria (`ST-010`, `ST-011` o `ST-012`). |
| `openedAt` | `string(date-time)` | Yes | Momento de apertura de la trayectoria. |
| `closedAt` | `string(date-time)` | No | Momento de cierre cuando aplique. |
| `lastCorrelationId` | `string` | No | Ultima correlacion operativa conocida para continuar el diagnostico. |

### Discovery canonical errors

| Code | When |
| --- | --- |
| `TRAJECTORY_DISCOVERY_SCOPE_INVALID` | La busqueda no define un `patientId` valido. |
| `AUTH_ROLE_FORBIDDEN` | El actor autenticado no puede descubrir trayectorias. |

### Discovery example response

```json
{
  "total": 2,
  "items": [
    {
      "trajectoryId": "TRJ-00041",
      "patientId": "PAT-00041",
      "queueId": "QUEUE-01",
      "currentState": "TrayectoriaActiva",
      "openedAt": "2026-04-06T08:10:00Z",
      "closedAt": null,
      "lastCorrelationId": "CORR-b4"
    },
    {
      "trajectoryId": "TRJ-00012",
      "patientId": "PAT-00041",
      "queueId": "QUEUE-2026-03-31",
      "currentState": "TrayectoriaFinalizada",
      "openedAt": "2026-03-31T09:10:00Z",
      "closedAt": "2026-03-31T09:44:00Z",
      "lastCorrelationId": "CORR-a3"
    }
  ]
}
```

## Trajectory rebuild

### Rebuild purpose

Reprocesar eventos historicos para materializar o reconciliar trayectorias de paciente sin ejecutar side effects operativos.

### Rebuild method and path

- `POST /api/patient-trajectories/rebuild`

### Rebuild authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Disponible para `Support`. |
| `X-Correlation-Id` | Yes | Obligatorio para auditar la operacion. |
| `X-Idempotency-Key` | Yes | Evita disparar rebuilds duplicados del mismo alcance. |

### Rebuild request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `queueId` | `string` | No | Limita el rebuild a una queue especifica. |
| `patientId` | `string` | No | Limita el rebuild a un paciente especifico. |
| `dryRun` | `boolean` | Yes | Cuando es `true`, valida alcance y conteos sin materializar cambios. |

### Rebuild response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `jobId` | `string` | Yes | Identificador del rebuild aceptado. |
| `acceptedAt` | `string(date-time)` | Yes | Momento de aceptacion. |
| `scope` | `string` | Yes | Alcance efectivo del rebuild. |
| `dryRun` | `boolean` | Yes | Refleja el modo solicitado. |
| `status` | `string` | Yes | Estado inicial del trabajo (`Accepted` o `Completed` para dry-run`). |

### Rebuild canonical errors

| Code | When |
| --- | --- |
| `TRAJECTORY_REBUILD_SCOPE_INVALID` | La solicitud no define un alcance valido o mezcla filtros incompatibles. |
| `TRAJECTORY_REBUILD_ALREADY_RUNNING` | Ya existe un rebuild activo para el mismo alcance e `X-Idempotency-Key`. |
| `AUTH_ROLE_FORBIDDEN` | El actor autenticado no puede ejecutar rebuild de trayectoria. |

## Query rules

- La consulta de trayectoria lee siempre desde proyecciones persistentes, nunca desde replay en hot path.
- El discovery lee siempre desde proyecciones persistentes y no puede disparar rebuild ni replay implicito.
- Un discovery sin coincidencias responde `200` con `total=0` e `items=[]`.
- El rebuild debe ser controlado, auditable e idempotente.
- Ningun rebuild de trayectoria puede reemitir mensajes externos ni modificar eventos historicos ya persistidos.
