# Metrics Catalog

- waiting time
- cashier throughput
- consultation throughput
- absent count
- projection lag

## Thresholds

- `waiting time p95`: seguir tendencia operativa y exponerla por dashboard.
- `projection lag <= 30 segundos` en operacion nominal.
- `projection lag > 120 segundos` genera alerta critica.
- `cashier throughput per hour` y `consultation throughput per hour` deben exponerse por dashboard.
