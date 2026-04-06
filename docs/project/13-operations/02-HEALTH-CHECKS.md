# Health Checks

- API live and ready
- database
- broker
- projection lag
- realtime channel

## Readiness thresholds

- `ready` requiere base de datos y broker alcanzables cuando la mensajeria externa esta habilitada.
- `ready` falla si `projection lag > 120 segundos`.
- `ready` puede quedar `degraded` mientras las proyecciones iniciales se materializan despues del arranque.
- `degraded` si el canal realtime pierde sincronizacion pero el sistema sigue aceptando operaciones internas.
