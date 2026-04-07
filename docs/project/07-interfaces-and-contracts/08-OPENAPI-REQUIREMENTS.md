# OpenAPI Requirements

## Scope

- Todos los endpoints HTTP y realtime documentados en `/docs/project/07-interfaces-and-contracts` deben poder convertirse sin ambiguedad a OpenAPI o AsyncAPI.
- Cada contrato debe describir headers, schemas, errores, ejemplos y reglas de seguridad suficientes para que Copilot genere handlers, DTOs y tests sin inferir campos faltantes.
- Los contratos de display publico deben seguir la regla de sanitizacion definida en `/docs/project/07-interfaces-and-contracts/10-PUBLIC-DISPLAY-CONTRACT.md`.

## Minimum contract sections

- Purpose
- Method and path
- Authorization and headers
- Request schema
- Response schema
- Canonical errors
- Example request
- Example response
- Notes when the endpoint reads from projections, audit store or realtime feeds

## Canonical schema conventions

| Element | Canonical type | Notes |
| --- | --- | --- |
| `*Id` | `string` | Identificadores opacos del dominio o de infraestructura. |
| `*Reference` | `string` | Referencias funcionales visibles para staff o integraciones. |
| `occurredAt`, `generatedAt`, `registeredAt`, `startedAt`, `finishedAt` | `string(date-time)` | Siempre en UTC y formato ISO-8601. |
| `queuePosition`, `waitingCount`, `activeRooms`, `attemptNumber`, `projectionLagSeconds` | `integer` | Enteros no negativos. |
| `validatedAmount` | `number` | Valor monetario en la moneda operativa configurada. |
| `idempotencyReplay`, `consultingRoomReleased` | `boolean` | Indicadores binarios de replay o liberacion. |
| `details` | `object` | Datos adicionales no sensibles en respuestas de error. |

## Canonical headers

| Header | Required | Applies to | Notes |
| --- | --- | --- | --- |
| `Authorization` | Yes, salvo login o display publico | Staff commands and protected queries | Bearer token con rol interno valido. |
| `X-Correlation-Id` | Yes en comandos; recomendado en queries | Commands, audit and monitoring flows | Debe propagarse a eventos, logs y auditoria. |
| `X-Idempotency-Key` | Yes en comandos mutantes | POST que cambian estado | La misma llave debe retornar la misma respuesta semantica o `IDEMPOTENCY_REPLAY`. |

## Canonical enums already fixed in docs

### StaffRole

- `Receptionist`
- `Cashier`
- `Doctor`
- `Supervisor`
- `Support`

### TurnState

- `EnEsperaTaquilla`
- `EnTaquilla`
- `PagoPendiente`
- `EnEsperaConsulta`
- `LlamadoConsulta`
- `EnConsulta`
- `Finalizado`
- `CanceladoPorAusencia`

## Error response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `code` | `string` | Yes | Debe coincidir con `/docs/project/07-interfaces-and-contracts/05-ERROR-CONTRACTS.md`. |
| `message` | `string` | Yes | Mensaje tecnico estable para cliente y logs. |
| `correlationId` | `string` | Yes | Valor propagado desde request o generado por el sistema. |
| `details` | `object` | No | Solo datos no sensibles utiles para soporte o validacion. |

## Example rules

- Usar nombres de campos estables y consistentes con el dominio.
- Reflejar `correlationId` y `X-Idempotency-Key` cuando aplique.
- Marcar `required` y `optional` explicitamente; no dejarlo implicito.
- Indicar `enum` solo cuando el valor ya exista en documentacion canonica.
- No documentar payloads que expongan PII en display publico.
