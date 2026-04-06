# Staff Identity Contracts

## Canonical enums

### Staff role enum

- `Receptionist`
- `Cashier`
- `Doctor`
- `Supervisor`
- `Support`

## Login

### Login purpose

Autenticar a un cliente confiable de staff y devolver sus capacidades operativas iniciales.

### Login method and path

- `POST /api/staff/auth/login`

### Login authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | No | El login inicia la sesion. |
| `X-Correlation-Id` | No | Si no llega, el sistema puede generarlo y devolverlo en la respuesta. |

### Login request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `identifier` | `string` | Yes | Usuario, email o identificador interno segun proveedor de identidad. |
| `password` | `string` | Yes | Credencial secreta del usuario. |

### Login response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `accessToken` | `string` | Yes | Token Bearer para operaciones autenticadas entre backend y componentes confiables. |
| `tokenType` | `string` | Yes | Valor esperado: `Bearer`. |
| `expiresInSeconds` | `integer` | Yes | TTL del token en segundos. |
| `role` | `string(enum: StaffRole)` | Yes | Rol operativo resuelto tras autenticacion. |
| `capabilities` | `array[string]` | Yes | Capacidades derivadas del rol. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad de la operacion. |

### Login canonical errors

| Code | When |
| --- | --- |
| `AUTH_INVALID_CREDENTIALS` | Credenciales invalidas o usuario inexistente. |
| `AUTH_ROLE_FORBIDDEN` | El usuario autentica pero no tiene rol permitido para la plataforma. |

### Login example request

```json
{
  "identifier": "supervisor.main",
  "password": "***redacted***"
}
```

### Login example response

```json
{
  "accessToken": "eyJhbGciOi...",
  "tokenType": "Bearer",
  "expiresInSeconds": 3600,
  "role": "Supervisor",
  "capabilities": [
    "staff.users.manage",
    "operations.dashboard.read",
    "audit.timeline.read"
  ],
  "correlationId": "CORR-login-001"
}
```

## Staff web session login

### Session login purpose

Autenticar al staff desde el frontend web y sellar una sesion BFF segura sin exponer `accessToken` al navegador.

### Session login method and path

- `POST /api/session/login`

### Session login authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | No | La autenticacion se resuelve contra el backend desde el BFF. |
| `X-Correlation-Id` | No | Si no llega, el frontend puede generarlo. |

### Session login request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `identifier` | `string` | Yes | Igual al contrato backend de login. |
| `password` | `string` | Yes | Credencial secreta del usuario. |

### Session login response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `session.staffId` | `string` | Yes | Identificador del usuario autenticado. |
| `session.username` | `string` | Yes | Nombre visible del actor autenticado. |
| `session.email` | `string` | Yes | Email operativo disponible para UI. |
| `session.role` | `string(enum: StaffRole)` | Yes | Rol operativo aprobado. |
| `session.authenticatedAt` | `string(date-time)` | Yes | Momento de autenticacion web. |
| `session.expiresAt` | `string(date-time)` | Yes | Expiracion efectiva de la sesion web. |
| `warnings` | `array[string]` | Yes | Advertencias no bloqueantes para UI o soporte. |

### Session login rules

- la respuesta debe incluir `Set-Cookie: rlapp_session` con cookie firmada `httpOnly`
- `accessToken` no puede aparecer en el payload de respuesta ni en APIs de sesion consumidas por el browser

### Session login example response

```json
{
  "session": {
    "staffId": "SUP-001",
    "username": "supervisor.main",
    "email": "supervisor.main@rlapp.local",
    "role": "Supervisor",
    "authenticatedAt": "2026-04-06T09:10:00Z",
    "expiresAt": "2026-04-06T10:10:00Z"
  },
  "warnings": []
}
```

## Staff web session summary

### Session summary purpose

Devolver al browser el contexto minimo de sesion ya autenticada sin exponer credenciales reutilizables.

### Session summary method and path

- `GET /api/session/me`

### Session summary authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Cookie` | Yes | Debe incluir `rlapp_session` valida. |
| `Authorization` | No | El browser no debe enviar Bearer token. |

### Session summary response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `staffId` | `string` | No | Presente cuando existe sesion valida. |
| `username` | `string` | No | Presente cuando existe sesion valida. |
| `email` | `string` | No | Presente cuando existe sesion valida. |
| `role` | `string(enum: StaffRole)` | No | Presente cuando existe sesion valida. |
| `authenticatedAt` | `string(date-time)` | No | Presente cuando existe sesion valida. |
| `expiresAt` | `string(date-time)` | No | Presente cuando existe sesion valida. |

### Session summary rules

- cuando no existe sesion valida responde `200` con `null`
- el payload nunca incluye `accessToken`, `refreshToken` ni secretos equivalentes

## Register staff user

### Register purpose

Dar de alta un usuario interno con rol inicial y estado operativo inicial.

### Register method and path

- `POST /api/staff/users/register`

### Register authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Solo supervisor. |
| `X-Correlation-Id` | Yes | Requerido para auditoria. |
| `X-Idempotency-Key` | Yes | Requerido por ser comando mutante. |

### Register request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `staffUserId` | `string` | Yes | Identificador canonico del usuario interno. |
| `role` | `string(enum: StaffRole)` | Yes | Rol inicial autorizado. |
| `initialStatus` | `string` | Yes | Estado de habilitacion definido por identidad interna. |

### Register response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `staffUserId` | `string` | Yes | Identificador creado o reafirmado. |
| `role` | `string(enum: StaffRole)` | Yes | Rol vigente tras el alta. |
| `status` | `string` | Yes | Estado operativo persistido. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

### Register canonical errors

| Code | When |
| --- | --- |
| `AUTH_ROLE_FORBIDDEN` | El actor no es supervisor. |
| `CONCURRENCY_CONFLICT` | El alta compite con otra mutacion del mismo usuario. |
| `IDEMPOTENCY_REPLAY` | Se repite la misma llave de idempotencia. |

## Change role

### Change role purpose

Cambiar el rol operativo de un usuario interno con trazabilidad explicita.

### Change role method and path

- `POST /api/staff/users/change-role`

### Change role authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Solo supervisor. |
| `X-Correlation-Id` | Yes | Requerido para timeline de auditoria. |
| `X-Idempotency-Key` | Yes | Requerido por ser comando mutante. |

### Change role request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `staffUserId` | `string` | Yes | Usuario a modificar. |
| `newRole` | `string(enum: StaffRole)` | Yes | Nuevo rol solicitado. |
| `reason` | `string` | Yes | Justificacion auditable del cambio. |

### Change role response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `staffUserId` | `string` | Yes | Usuario afectado. |
| `previousRole` | `string(enum: StaffRole)` | Yes | Rol anterior. |
| `newRole` | `string(enum: StaffRole)` | Yes | Rol persistido. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

### Change role canonical errors

| Code | When |
| --- | --- |
| `STAFF_USER_NOT_FOUND` | El usuario no existe. |
| `ROLE_CHANGE_NOT_ALLOWED` | La politica de RBAC o segregacion de funciones impide el cambio. |
| `AUTH_ROLE_FORBIDDEN` | El actor no tiene privilegios de supervisor. |
| `IDEMPOTENCY_REPLAY` | Se repite la misma mutacion. |

## Change status

### Change status purpose

Cambiar el estado operativo de un usuario interno con razon auditable.

### Change status method and path

- `POST /api/staff/users/change-status`

### Change status authorization and headers

| Header | Required | Notes |
| --- | --- | --- |
| `Authorization` | Yes | Solo supervisor. |
| `X-Correlation-Id` | Yes | Requerido para auditoria. |
| `X-Idempotency-Key` | Yes | Requerido por ser comando mutante. |

### Change status request schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `staffUserId` | `string` | Yes | Usuario a modificar. |
| `newStatus` | `string` | Yes | Estado operativo requerido por identidad interna. |
| `reason` | `string` | Yes | Justificacion del cambio. |

### Change status response schema

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `staffUserId` | `string` | Yes | Usuario afectado. |
| `status` | `string` | Yes | Estado vigente tras la mutacion. |
| `correlationId` | `string` | Yes | Identificador de trazabilidad. |

### Change status canonical errors

| Code | When |
| --- | --- |
| `STAFF_USER_NOT_FOUND` | El usuario no existe. |
| `AUTH_ROLE_FORBIDDEN` | El actor no es supervisor. |
| `CONCURRENCY_CONFLICT` | Existe una mutacion competitiva. |
| `IDEMPOTENCY_REPLAY` | Se repite la misma mutacion. |

## Security requirements

- `Authorization` obligatorio en todas las operaciones salvo login.
- `X-Correlation-Id` obligatorio en alta, cambio de rol y cambio de estado.
- Operaciones de gestion de usuarios limitadas a supervisor.
- el frontend web de staff usa `rlapp_session` firmada `httpOnly`; el browser no recibe Bearer token del backend
- los endpoints de sesion del frontend deben devolver solo resumen de sesion y metadatos no sensibles
