# Configuration Matrix

Variables por entorno para API, worker, PostgreSQL, RabbitMQ, auth y observabilidad.

## Outbox Processor

- `OutboxProcessor:PollingIntervalMs`: intervalo de polling del worker de outbox. Valor inicial de Fase 1: `500`.
- `OutboxProcessor:BatchSize`: cantidad maxima de mensajes procesados por ciclo. Valor inicial de Fase 1: `50`.
- `Messaging:Enabled=false` en Docker local implica despacho local del outbox en vez de broker externo.
