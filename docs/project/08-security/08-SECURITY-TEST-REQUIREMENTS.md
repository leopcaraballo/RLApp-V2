# Security Test Requirements

- 401 para acceso sin token
- 403 para rol invalido
- proteccion del display frente a mutaciones
- pruebas de secretos y configuracion insegura
- el frontend web no expone `accessToken` en `/api/session/login`, `/api/session/me` ni `/api/realtime/operations`
- no deben existir bypasses de autorizacion entre query HTTP y stream realtime para staff
