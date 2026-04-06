# Runbook

Operacion diaria, arranque, validacion y contingencias principales.

## Health probes

- `GET /health/startup` valida dependencias minimas de arranque para contenedores: proceso API, PostgreSQL y RabbitMQ cuando el broker esta habilitado.
- `GET /health/ready` conserva la validacion operativa completa: base de datos, broker, projection lag y canal realtime.
- Si `ready` falla por `ProjectionLag` despues de un reinicio con volumen persistente, revisar los logs de `OperationalProjectionWarmupService`; el servicio debe recomponer `QueueState` y `WaitingRoomMonitor` desde el event store sin reemitir side effects.
