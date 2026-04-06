# TDD-S-009 Platform NFR

- performance thresholds
- resilience and recovery
- observability requirements
- structured logs for trajectory discovery lookups with `correlationId`, `patientId`, `queueId` and match count
- validate `ProjectionLag` health check against `Healthy`, `Degraded` and `Unhealthy` thresholds derived from `30s` and `120s`
- validate `RealtimeChannel` health check against active connection and latest publish outcome semantics
- expose Prometheus metrics for trajectory discovery requests, duration and match count
- expose Prometheus metrics for realtime publications and publish duration
- propagate tracing for trajectory discovery with `correlationId`, `patientId` and optional `queueId`
- dead-letter alerts and counters for unrecoverable outbox messages
- readiness remains healthy without broker when external messaging is disabled
