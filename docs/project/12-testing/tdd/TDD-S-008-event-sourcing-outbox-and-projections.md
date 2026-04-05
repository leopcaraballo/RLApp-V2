# TDD-S-008 Event Sourcing Outbox And Projections

- event store append and load
- event store assigns monotonic `SequenceNumber` per aggregate stream
- event store rejects stale `expectedVersion` with `CONCURRENCY_CONFLICT`
- outbox exactly-once publishing behavior
- unknown or invalid outbox messages are quarantined to dead-letter storage
- local outbox fallback materializes projections when external messaging is disabled
- projection rebuild idempotency
- trajectory projection rebuild reuses the same idempotent replay contract
