# TDD-S-012 Saga Correlation And State Machines

- correlate longitudinal state machines by `trajectoryId` instead of `PatientId`
- preserve `correlationId` as operational trace in saga state, audit and emitted messages
- reuse one saga instance for the same trajectory across consultation call, completion and absence events
- bridge legacy messages without `trajectoryId` through deterministic resolution
- emit logs and traces with `trajectoryId` and `correlationId` during transitions, retries and timeouts
- persist consultation saga state in PostgreSQL-backed storage instead of the default in-memory repository
- publish a machine-readable async contract for EV-011, EV-012 and EV-013 aligned with the current runtime payload
