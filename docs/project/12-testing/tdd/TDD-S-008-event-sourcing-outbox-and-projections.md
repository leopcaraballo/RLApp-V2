# TDD-S-008 Event Sourcing Outbox And Projections

- event store append and load
- outbox exactly-once publishing behavior
- unknown or invalid outbox messages are quarantined to dead-letter storage
- local outbox fallback materializes projections when external messaging is disabled
- projection rebuild idempotency
- trajectory projection rebuild reuses the same idempotent replay contract
