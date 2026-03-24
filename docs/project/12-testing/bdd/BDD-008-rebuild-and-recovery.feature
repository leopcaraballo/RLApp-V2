Feature: Rebuild and recovery

  Scenario: Support rebuilds projections
    Given the projection store is stale
    When support triggers rebuild
    Then the projections catch up from the event store
