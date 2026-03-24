# Health Checks

- API live and ready
- database
- broker
- projection lag
- realtime channel

## Readiness thresholds

- `ready` requiere base de datos y broker alcanzables.
- `ready` falla si `projection lag > 120 segundos`.
- `degraded` si el canal realtime pierde sincronizacion pero el sistema sigue aceptando operaciones internas.
