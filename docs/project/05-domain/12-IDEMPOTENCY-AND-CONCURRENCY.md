# Idempotency And Concurrency

## Idempotency

- check-in debe ser idempotente por llave
- comandos mutantes expuestos al staff deben reemitir la misma respuesta ante reintento valido
- replay de trayectoria debe deduplicar por `trajectoryId` y evento historico ya aplicado

## Concurrency

- el agregado usa versionado por eventos
- el event store debe persistir `SequenceNumber` monotono por `AggregateId`
- los comandos mutantes deben validar `expectedVersion` contra la ultima version persistida del agregado
- conflictos de version deben devolverse como conflicto de concurrencia
- una segunda trayectoria activa para el mismo paciente y `QueueId` debe rechazarse como conflicto de dominio
