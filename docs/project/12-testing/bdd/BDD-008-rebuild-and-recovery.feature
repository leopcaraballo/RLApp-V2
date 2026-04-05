Feature: Rebuild and recovery

  Scenario: Support rebuilds projections
    Given the projection store is stale
    When support triggers rebuild
    Then the projections catch up from the event store

  Scenario: Write-side append rejects a stale aggregate version
    Given two write operations load the same aggregate version from the event store
    When the second operation persists with a stale expectedVersion
    Then the event store rejects the append with CONCURRENCY_CONFLICT and no partial outbox or audit success is committed
