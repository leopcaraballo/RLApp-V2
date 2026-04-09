# RLApp Backend API Audit And Guide

## Alcance

Esta documentación cubre la superficie HTTP implementada hoy en `apps/backend/src/RLApp.Adapters.Http/Controllers` y los endpoints de salud definidos en `apps/backend/src/RLApp.Api/Program.cs`.

Los artefactos machine-readable vigentes de esta carpeta son:

- [openapi.yaml](./openapi.yaml)
- [asyncapi.yaml](./asyncapi.yaml)

`openapi.yaml` cubre la superficie HTTP y `asyncapi.yaml` formaliza los eventos de correlacion longitudinal usados por `ConsultationSaga` en Fase 4.

Esta guía **no** asume que la documentación objetivo del proyecto esté implementada; cuando hay divergencias, se documenta el comportamiento real y se deja el hallazgo explícito.

## Inventario actual de endpoints

| Área | Método | Ruta | Auth | Notas |
| --- | --- | --- | --- | --- |
| Staff | `POST` | `/api/staff/auth/login` | Pública | Login JWT. |
| Staff | `POST` | `/api/staff/users/change-role` | `Supervisor` | Mutación administrativa. |
| Reception | `POST` | `/api/reception/register` | `Receptionist`, `Supervisor` | Alias operativo para check-in. |
| WaitingRoom | `POST` | `/api/waiting-room/check-in` | `Receptionist`, `Supervisor` | Check-in formal. |
| WaitingRoom | `POST` | `/api/waiting-room/call-patient` | `Doctor`, `Supervisor` | `queueId` via query string. |
| WaitingRoom | `POST` | `/api/waiting-room/claim-next` | `Doctor`, `Supervisor` | `queueId` via query string. |
| Cashier | `POST` | `/api/cashier/call-next` | `Cashier`, `Supervisor` | Devuelve `patientId`, no `turnId`. |
| Cashier | `POST` | `/api/cashier/validate-payment` | `Cashier`, `Supervisor` | Usa `queueId` + `patientId`. |
| Cashier | `POST` | `/api/cashier/mark-payment-pending` | `Cashier`, `Supervisor` | Varios campos del request están ignorados. |
| Cashier | `POST` | `/api/cashier/mark-absent` | `Cashier`, `Supervisor` | Varios campos del request están ignorados. |
| Medical | `POST` | `/api/medical/consulting-room/activate` | `Supervisor` | Activa consultorio. |
| Medical | `POST` | `/api/medical/consulting-room/deactivate` | `Supervisor` | Responde 400 cuando la sala no existe. |
| Medical | `POST` | `/api/medical/finish-consultation` | `Doctor`, `Supervisor` | Respuesta simple, sin outcome persistido. |
| Medical | `POST` | `/api/medical/mark-absent` | `Doctor`, `Supervisor` | Respuesta simple, sin `policyOutcome`. |
| Health | `GET` | `/health` | Pública | Estado agregado. |
| Health | `GET` | `/health/ready` | Pública | Readiness. |
| Health | `GET` | `/health/live` | Pública | Liveness. |

## Problemas detectados

### 1. Contratos con campos obligatorios que hoy no se usan

| Endpoint | Campos ignorados por la lógica actual |
| --- | --- |
| `/api/staff/users/change-role` | `reason` |
| `/api/reception/register` | `appointmentReference`, `priority`, `notes` |
| `/api/waiting-room/check-in` | `appointmentReference`, `consultationType`, `priority` |
| `/api/cashier/call-next` | `cashierStationId` |
| `/api/cashier/validate-payment` | `turnId`, `paymentReference` |
| `/api/cashier/mark-payment-pending` | `turnId`, `reason`, `attemptNumber` |
| `/api/cashier/mark-absent` | `turnId`, `reason` |
| `/api/medical/finish-consultation` | `turnId`, `outcome` |
| `/api/medical/mark-absent` | `turnId`, `reason` |

Impacto:

- El frontend puede creer que está enviando información operativa importante cuando en realidad el backend la descarta.
- QA puede diseñar casos de prueba falsamente positivos si valida solo HTTP 200 sin verificar persistencia/efecto de negocio.

### 2. Respuestas implementadas no coinciden con los DTOs de `Adapters.Http/Responses`

Los archivos:

- `apps/backend/src/RLApp.Adapters.Http/Responses/StaffIdentityResponses.cs`
- `apps/backend/src/RLApp.Adapters.Http/Responses/ReceptionAndWaitingResponses.cs`
- `apps/backend/src/RLApp.Adapters.Http/Responses/CashierAndMedicalResponses.cs`

no son la fuente de verdad del runtime. Los controllers actuales responden:

- `AuthenticationResultDto` en login.
- `CommandResult` simple en la mayoría de operaciones mutantes.
- `PatientCallResultDto` en `cashier/call-next`.
- `ClaimedPatientResultDto` en `waiting-room/claim-next`.

Impacto:

- Alto riesgo de generar SDKs o tests con contratos incorrectos.
- Swagger generado solo por reflexión podría inducir a error si en el futuro se anotan esos DTOs sin alinear controllers.

### 3. Formato de errores inconsistente

Hoy conviven al menos tres formas de error:

- `ValidationProblemDetails` de ASP.NET Core cuando falla el model binding.
- Objeto inline `{ error, correlationId }` cuando el controller recibe `CommandResult.Failure`.
- `ProblemDetails` desde `GlobalExceptionMiddleware` para excepciones no controladas.

Además:

- `401` y `403` por JWT/Authorization suelen depender del comportamiento por defecto de ASP.NET Core y no garantizan un body uniforme.
- Muchos casos de “not found” son degradados a `400` por los handlers, no a `404`.

Impacto:

- Frontend y QA necesitan parsers distintos según el tipo de fallo.
- El contrato de errores no es estable ni amigable para automatización.

### 4. Inconsistencia de diseño entre rutas

- `queueId` llega en el body para casi todo, pero en `waiting-room/call-patient` y `waiting-room/claim-next` llega por query string.
- No hay versionado de API por URI ni por header.
- Existen contratos de consulta en la documentación TO-BE, pero hoy no hay endpoints GET funcionales de negocio.

Impacto:

- Mayor complejidad de integración en frontend.
- El diseño REST no es uniforme.

### 5. Login documentado como `identifier`, implementado como `username`

El DTO acepta `identifier`, pero el handler usa `GetByUsernameAsync`.

Impacto:

- Si frontend envía email o ID interno esperando soporte, el login fallará con `AUTH_INVALID_CREDENTIALS`.

### 6. Seguridad parcialmente documentada, parcialmente implementada

Implementado:

- JWT Bearer.
- Policies por rol en controllers.
- Hub SignalR protegido.

Pendiente o inconsistente:

- No hay documentación OpenAPI publicada en producción.
- `X-Idempotency-Key` se recibe, pero no se persiste ni se reejecuta idempotentemente.
- No hay formato uniforme para errores `401/403`.

### 7. Health checks todavía parciales frente a la operación objetivo

La documentación operativa del proyecto exige revisar también:

- `projection lag`
- `realtime channel`

La implementación actual expone:

- `database`
- `projection lag`
- `realtime channel`
- `broker` opcional según configuración
- `self`

Impacto:

- `ready` ya cubre la salud de proyecciones persistidas y del broadcaster realtime en función de conexiones activas y del resultado de la última publicación SignalR.
- Siguen faltando alertas operativas dedicadas y evidencia end-to-end de reconnect para el canal realtime.

## Recomendaciones técnicas

### Prioridad alta

- Unificar el contrato de errores en un envelope único, por ejemplo `{ code, message, correlationId, details }`.
- Hacer que los handlers realmente usen o eliminen los campos hoy ignorados.
- Retirar o alinear los DTOs de `Adapters.Http/Responses` con la salida real.
- Normalizar `queueId` para que llegue siempre por body o siempre por path/query, no mezclado.
- Resolver el login para aceptar de verdad `username | email | internalId`, o renombrar `identifier` a `username`.

### Prioridad media

- Agregar versionado explícito de API.
- Publicar OpenAPI también fuera de `Development`, al menos como artefacto estático en CI/CD.
- Añadir anotaciones de responses/produces/problem details en los controllers para endurecer el contrato.
- Añadir endpoints GET de consulta si frontend depende de snapshots o monitor.

### Prioridad media-alta para robustez operativa

- Implementar idempotencia real sobre `X-Idempotency-Key`.
- Hacer consistente el uso de `404` para recursos faltantes.
- Endurecer evidencia operativa end-to-end del canal realtime, incluyendo reconnect y alertas sobre thresholds de propagación.

## Guía para frontend developers

### 1. Autenticación

1. Llamar `POST /api/staff/auth/login`.
2. Guardar `accessToken`.
3. Enviar `Authorization: Bearer <token>` en todos los endpoints protegidos.

Notas importantes:

- No asumas que `identifier` acepta email; hoy usa `username`.
- No asumas que `capabilities` trae permisos derivados; hoy normalmente llega vacío.

### 2. Headers recomendados

Enviar siempre:

- `Authorization` en endpoints protegidos.
- `X-Correlation-Id` en todas las operaciones, incluso donde sea opcional.
- `X-Idempotency-Key` en operaciones mutantes, aunque hoy no haya replay real.

### 3. Flujo típico end-to-end

#### Login

```http
POST /api/staff/auth/login
Content-Type: application/json

{
  "identifier": "admin",
  "password": "local-admin-pass"
}
```

#### Check-in desde recepción

```http
POST /api/waiting-room/check-in
Authorization: Bearer <token>
X-Correlation-Id: CORR-checkin-001
X-Idempotency-Key: IDEMP-checkin-001
Content-Type: application/json

{
  "queueId": "Q-2026-03-19-MAIN",
  "appointmentReference": "APT-20260319-0045",
  "patientId": "PAT-0045",
  "patientName": "Ana Perez",
  "consultationType": "GeneralMedicine",
  "priority": "Standard"
}
```

#### Caja: llamar siguiente

```http
POST /api/cashier/call-next
Authorization: Bearer <token>
X-Correlation-Id: CORR-cash-001
X-Idempotency-Key: IDEMP-cash-001
Content-Type: application/json

{
  "queueId": "Q-2026-03-19-MAIN",
  "cashierStationId": "CASH-01"
}
```

#### Consulta: reclamar siguiente

```http
POST /api/waiting-room/claim-next?queueId=Q-2026-03-19-MAIN
Authorization: Bearer <token>
X-Correlation-Id: CORR-claim-001
Content-Type: application/json

{
  "roomId": "ROOM-01"
}
```

#### Consulta: finalizar

```http
POST /api/medical/finish-consultation
Authorization: Bearer <token>
X-Correlation-Id: CORR-finish-001
Content-Type: application/json

{
  "turnId": "TURN-00052",
  "queueId": "Q-2026-03-19-MAIN",
  "patientId": "PAT-0052",
  "consultingRoomId": "ROOM-01",
  "outcome": "completed"
}
```

### 4. Qué no debe asumir el frontend

- Que `turnId` se usa en todos los flujos donde aparece.
- Que `paymentReference`, `reason`, `attemptNumber` u `outcome` cambian el resultado hoy.
- Que todos los errores tendrán la misma forma JSON.
- Que las respuestas de caja/consulta devolverán siempre identificadores de turno; algunas devuelven solo `patientId`.

## Guía para QA automation

### Casos mínimos por endpoint

| Endpoint | Caso exitoso | Caso de error | Edge case clave |
| --- | --- | --- | --- |
| `/api/staff/auth/login` | Credenciales válidas | Password inválido | Enviar email en `identifier` y verificar fallo actual |
| `/api/staff/users/change-role` | Cambio válido con supervisor | Rol inválido | Verificar que `reason` no altera la lógica |
| `/api/reception/register` | Registro con cola válida | `patientId` faltante | Cambiar `appointmentReference` y confirmar que hoy no afecta |
| `/api/waiting-room/check-in` | Check-in correcto | Duplicado en la cola | Variar `consultationType` o `priority` sin cambio observable |
| `/api/waiting-room/call-patient` | Llamado válido | `queueId` faltante | `queueId` vacío por query debe devolver 400 inline |
| `/api/waiting-room/claim-next` | Claim válido | Cola vacía | Validar que devuelve `patientId`, no `turnId` |
| `/api/cashier/call-next` | Cola con pacientes | Cola vacía | Cambiar `cashierStationId` y validar que no cambia el resultado |
| `/api/cashier/validate-payment` | Pago válido | `validatedAmount <= 0` | Cambiar `paymentReference` sin efecto observable |
| `/api/cashier/mark-payment-pending` | Flujo pendiente | Cola inexistente | Cambiar `attemptNumber` sin efecto observable |
| `/api/cashier/mark-absent` | Ausencia válida | Cola inexistente | Cambiar `reason` sin efecto observable |
| `/api/medical/consulting-room/activate` | Activación válida | `roomName` faltante | Reactivar una sala ya activa |
| `/api/medical/consulting-room/deactivate` | Desactivación válida | Sala inexistente | Desactivar sala ocupada |
| `/api/medical/finish-consultation` | Finalización válida | Cola inexistente | Cambiar `outcome` sin efecto observable |
| `/api/medical/mark-absent` | Ausencia válida | Paciente no asignado | Cambiar `reason` sin efecto observable |

## Nota sobre realtime

El backend también expone un hub SignalR en:

- `/hubs/notifications`

No está cubierto por OpenAPI porque no es un contrato HTTP REST, pero sí requiere autenticación y permite:

- `JoinQueueGroup(queueId)`
- `LeaveQueueGroup(queueId)`

Falta documentación formal del payload de mensajes emitidos por consumidores hacia el hub.
