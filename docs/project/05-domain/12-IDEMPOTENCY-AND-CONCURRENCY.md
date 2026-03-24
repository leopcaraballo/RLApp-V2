# Idempotency And Concurrency

## Idempotency

- check-in debe ser idempotente por llave
- comandos mutantes expuestos al staff deben reemitir la misma respuesta ante reintento valido

## Concurrency

- el agregado usa versionado por eventos
- conflictos de version deben devolverse como conflicto de concurrencia
