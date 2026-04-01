# Metrics Catalog

- waiting time
- cashier throughput
- consultation throughput
- absent count
- projection lag
- outbox backlog
- outbox publish duration
- outbox propagation delay

## Thresholds

- `waiting time p95`: seguir tendencia operativa y exponerla por dashboard.
- `projection lag <= 30 segundos` en operacion nominal.
- `projection lag > 120 segundos` genera alerta critica.
- `cashier throughput per hour` y `consultation throughput per hour` deben exponerse por dashboard.
- `outbox propagation delay p95 <= 3000 ms` en operacion nominal.
- `outbox backlog` debe tender a cero en operacion estable y escalar cuando supere la capacidad del batch.
