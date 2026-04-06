# Fase 6 - Kickoff

**Fecha:** 2026-04-06
**Rama de trabajo:** feature/code-only-audit-20260406
**Origen:** Fase 5 discovery operacional cerrada y baseline Docker saludable

## Objetivo

Abrir el slice que materializa vistas operativas sincronizadas para staff, cerrando el gap entre monitor, dashboard y trayectoria con lectura desde proyecciones persistidas y realtime mediado por el BFF del frontend.

## Decision de arranque

- Se abre un nuevo `UC-021` y una nueva `S-013` porque el alcance ya no es solo discovery de trayectoria: ahora cubre monitor, dashboard, sesion web y sincronizacion realtime para staff.
- `monitor`, `dashboard` y `trajectory` permanecen projection-first; ninguna consulta puede leer del write-side ni disparar replay en hot path.
- El navegador no recibe el token Bearer del backend. El frontend conserva ese token del lado servidor dentro de una sesion firmada `httpOnly` y expone solo resumen de sesion y un stream same-origin.
- El canal realtime de staff se trata como invalidacion y resync, no como fuente de verdad. Toda reconexion debe volver a consultar snapshots persistidos.

## Trazabilidad

- US: `US-021`
- UC: `UC-021`
- S: `S-013`, `S-009`
- BDD/TDD: `BDD-012`, `TDD-S-013`, `TDD-S-009`, `SEC-TEST-003`, `RES-TEST-004`

## Alcance de Fase 6

- canonizar en `/docs/project` la traduccion del PRD externo para visibilidad operacional sincronizada
- exponer `GET /api/v1/waiting-room/{queueId}/monitor` y `GET /api/v1/operations/dashboard` sobre proyecciones persistidas reales
- actualizar la UI de `/waiting-room` y `/` para consumir esos read models en lugar de mostrar hallazgos estaticos
- agregar un canal realtime de staff mediado por el BFF para invalidar y resincronizar monitor, dashboard y trayectoria sin exponer JWT al browser

## Entregables esperados

1. Cadena documental `US-021` / `UC-021` / `S-013` con BDD, TDD, contratos, seguridad y trazabilidad cerrada.
2. Implementacion backend de queries operativas sobre `v_queue_state`, `v_waiting_room_monitor`, `v_operations_dashboard` y proyecciones relacionadas.
3. Implementacion frontend de dashboard y monitor sincronizados, con stream realtime same-origin para staff.
4. Evidencia de validacion con build frontend, pruebas backend del slice y smoke Docker/Compose del flujo autenticado.

## Restricciones

- Mantener Git Flow: trabajo solo en `feature/*`.
- Mantener docs-first: no cerrar codigo sin cerrar primero `US/UC/S/BDD/TDD` del slice.
- No exponer `accessToken` del backend en respuestas del frontend ni en almacenamiento accesible al navegador.
- No usar el stream realtime para payloads PII adicionales ni para saltarse reglas de autorizacion ya aprobadas.
- No reconstruir read models desde replay en cada request; la sincronizacion debe salir de snapshots persistidos y refetch controlado.

## Riesgos remanentes

- El canal realtime de staff agrega riesgo de sesion y reconnect; el slice debe dejar 401/403 y resincronizacion explicitos.
- El contrato historico de `queue-state`, `next-turn` y `recent-history` sigue fuera del cierre funcional de este corte si no es requerido por la UI operativa aprobada.
- El baseline Docker debe seguir siendo obligatorio, porque el valor del slice depende de backend, worker, broker y frontend levantando juntos.
