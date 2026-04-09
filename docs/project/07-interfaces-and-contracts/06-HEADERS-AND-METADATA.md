# Headers And Metadata

- X-Correlation-Id
- X-Idempotency-Key
- Authorization
- Set-Cookie: rlapp_session

`Authorization` se permite hacia el backend y entre componentes confiables. El browser del frontend staff no debe recibir ni persistir el Bearer token del backend.

`rlapp_session` es una cookie firmada `httpOnly`; debe ser `Secure` fuera de local y `SameSite=Lax` como baseline del workspace web.

Los endpoints `/api/session/*` y `/api/realtime/operations` del frontend deben operar sobre la sesion sellada y nunca devolver `accessToken` en payload.

X-User-Role solo se considera legacy en AS-IS y no debe existir en TO-BE final.
