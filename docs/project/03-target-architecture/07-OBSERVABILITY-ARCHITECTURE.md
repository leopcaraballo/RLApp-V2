# Observability Architecture

## Mandatory observability

- logs estructurados
- metricas de cola, caja, consulta y display
- tracing de request, evento y proyeccion
- health checks
- dashboards y alertas
- readiness debe cubrir base de datos, broker cuando aplique y `projection lag` de las proyecciones operativas
- `projection lag` usa umbrales de `S-009`: degradado cuando supera `30s` y critico cuando supera `120s`
- el baseline ejecutable debe exponer scraping Prometheus para outbox y operaciones criticas como discovery de trayectorias
- el baseline ejecutable ya cubre el canal realtime con health basado en conexiones activas y resultado de la ultima publicacion SignalR
- alertas dedicadas y evidencia end-to-end de reconnect del canal realtime siguen siendo un hueco posterior
